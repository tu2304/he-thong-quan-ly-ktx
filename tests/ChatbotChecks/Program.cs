using System.Net;
using System.Reflection;
using System.Text.Json;
using KtxAI.Api.Common;
using KtxAI.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

// Offline transport checks: no real API key, network request or database required.
var cases = new (string Body, HttpStatusCode Status, Type Error)[]
{
    ("""{"candidates":[{"finishReason":"STOP","content":{"parts":[{"text":"hidden","thought":true},{"text":"First"},{"text":"Second"}]}}]}""", HttpStatusCode.OK, null),
    ("{}", HttpStatusCode.TooManyRequests, typeof(GeminiRateLimitException)),
    ("secret upstream response", HttpStatusCode.Unauthorized, typeof(GeminiApiException)),
    ("not json", HttpStatusCode.OK, typeof(GeminiApiException)),
    ("""{"candidates":[{"finishReason":"MAX_TOKENS"}]}""", HttpStatusCode.OK, typeof(GeminiApiException)),
    ("""{"candidates":[]}""", HttpStatusCode.OK, typeof(GeminiApiException)),
    ("""{"candidates":[{"finishReason":"SAFETY"}]}""", HttpStatusCode.OK, typeof(GeminiApiException)),
    ("""{"candidates":[{"finishReason":"STOP","content":{"parts":[]}}]}""", HttpStatusCode.OK, typeof(GeminiApiException))
};
var method = typeof(GeminiChatbotService).GetMethod("GoiGeminiAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
foreach (var test in cases)
{
    using var client = new HttpClient(new FakeHandler(test.Body, test.Status));
    var service = new GeminiChatbotService(null, client,
        Options.Create(new GeminiOptions { ApiKey = "test-key" }),
        NullLogger<GeminiChatbotService>.Instance, new TestEnvironment());
    try
    {
        var result = await (Task<string>)method.Invoke(service, new object[] { "Question" })!;
        if (test.Error != null || result != "FirstSecond")
            throw new Exception("Unexpected response text.");
    }
    catch (Exception ex) when (test.Error != null && ex.GetType() == test.Error)
    {
        if (ex.Message.Contains("secret upstream")) throw new Exception("Upstream body leaked.");
    }
}
Console.WriteLine($"Passed {cases.Length} offline Gemini response checks.");

sealed class FakeHandler(string body, HttpStatusCode status) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        if (request.RequestUri!.ToString() != "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash:generateContent" ||
            request.Headers.GetValues("x-goog-api-key").Single() != "test-key")
            throw new Exception("Wrong API endpoint or authorization.");
        using var payload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
        var root = payload.RootElement;
        if (root.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString() != "Question" ||
            string.IsNullOrWhiteSpace(root.GetProperty("systemInstruction").GetProperty("parts")[0].GetProperty("text").GetString()) ||
            root.GetProperty("generationConfig").GetProperty("maxOutputTokens").GetInt32() <= 0)
            throw new Exception("Wrong Gemini API payload.");
        return new HttpResponseMessage(status) { Content = new StringContent(body) };
    }
}

sealed class TestEnvironment : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "Checks";
    public string EnvironmentName { get; set; } = "Testing";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = Directory.GetCurrentDirectory();
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}
