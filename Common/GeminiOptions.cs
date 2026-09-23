namespace KtxAI.Api.Common
{
    // Cấu hình kết nối tới Google Gemini API cho chatbot hỏi đáp nội quy (KT3 tiêu chí #2).
    // Nạp từ appsettings.json / biến môi trường (.env) qua section "Gemini",
    // KHÔNG hardcode API key trong code (tiêu chí #2 - bảo vệ API key).
    public class GeminiOptions
    {
        public string ApiKey { get; set; } = "";
        public string Model { get; set; } = "gemini-2.0-flash";
        public int TimeoutSeconds { get; set; } = 30;

        // Giới hạn độ dài để tránh gửi dữ liệu quá lớn cho AI (tiêu chí #7).
        public int MaxQuestionChars { get; set; } = 500;
        public int MaxContextChars { get; set; } = 4000;
    }
}
