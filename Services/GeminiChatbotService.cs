using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using KtxAI.Api.Common;
using KtxAI.Api.Data;
using KtxAI.Api.Dtos;
using KtxAI.Api.Models;

namespace KtxAI.Api.Services
{
    // Lỗi cụ thể để phân biệt các tình huống gọi Gemini API thất bại (tiêu chí KT3 #7).
    public class GeminiApiException : Exception
    {
        public GeminiApiException(string message, Exception inner = null) : base(message, inner) { }
    }

    public class GeminiRateLimitException : Exception { }

    // Chatbot hỏi đáp nội quy KTX bằng AI (Google Gemini) — chức năng AI chính của KT3.
    // Luồng xử lý: tìm điều khoản nội quy liên quan trong CSDL (tiêu chí #5) -> ghép vào
    // prompt cùng câu hỏi -> gọi Gemini API (tiêu chí #2) -> trả lời kèm nguồn trích dẫn (#6).
    public class GeminiChatbotService : IChatbotService
    {
        private readonly AppDbContext _db;
        private readonly HttpClient _http;
        private readonly GeminiOptions _options;
        private readonly ILogger<GeminiChatbotService> _logger;
        private readonly string _systemPrompt;

        private const string KHONG_TIM_THAY_NOI_QUY =
            "Xin lỗi, mình chưa tìm thấy điều khoản nội quy nào liên quan đến câu hỏi này. " +
            "Bạn vui lòng liên hệ trực tiếp Ban quản lý KTX để được hỗ trợ chính xác.";

        public GeminiChatbotService(
            AppDbContext db,
            HttpClient http,
            IOptions<GeminiOptions> options,
            ILogger<GeminiChatbotService> logger,
            IWebHostEnvironment env)
        {
            _db = db;
            _http = http;
            _options = options.Value;
            _logger = logger;

            // Prompt tách riêng khỏi code (tiêu chí #3), đọc từ file text tại Prompts/.
            var promptPath = Path.Combine(env.ContentRootPath, "Prompts", "chatbot_system_prompt.txt");
            _systemPrompt = File.Exists(promptPath)
                ? File.ReadAllText(promptPath)
                : "Bạn là trợ lý hỏi đáp nội quy ký túc xá. Chỉ trả lời dựa trên dữ liệu được cung cấp, không suy diễn thêm.";
        }

        public async Task<ChatbotTraLoiDto> HoiAsync(string cauHoi)
        {
            if (string.IsNullOrWhiteSpace(cauHoi))
                throw new AppException("Câu hỏi không được để trống.", 400);

            cauHoi = cauHoi.Trim();

            // Giới hạn độ dài đầu vào — tránh gửi dữ liệu quá lớn / lạm dụng API (tiêu chí #7).
            if (cauHoi.Length > _options.MaxQuestionChars)
                throw new AppException(
                    $"Câu hỏi quá dài (tối đa {_options.MaxQuestionChars} ký tự). Vui lòng hỏi ngắn gọn hơn.", 400);

            var lienQuan = await TimNoiQuyLienQuanAsync(cauHoi);

            // Không tìm thấy dữ liệu phù hợp -> trả lời cố định, KHÔNG gọi AI.
            // Đây là lựa chọn thiết kế có chủ đích: tránh để AI tự suy diễn/bịa nội quy
            // không tồn tại, đồng thời tiết kiệm lượt gọi API (tiêu chí #5, #7).
            if (lienQuan.Count == 0)
            {
                return new ChatbotTraLoiDto
                {
                    TraLoi = KHONG_TIM_THAY_NOI_QUY,
                    NguonNoiQuy = new List<string>(),
                    CoCanhBao = true,
                    GhiChu = "Không gọi AI vì không có dữ liệu nội quy phù hợp trong CSDL."
                };
            }

            var userPrompt = BuildUserPrompt(cauHoi, lienQuan);
            string aiText;

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _logger.LogWarning("Chưa cấu hình Gemini API key (Gemini__ApiKey trong .env). Dùng phản hồi dựa trên nội quy nội bộ.");
                aiText = BuildLocalReply(cauHoi, lienQuan);
            }
            else
            {
                try
                {
                    aiText = await GoiGeminiAsync(userPrompt);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogWarning("Gemini API timeout sau {Timeout}s.", _options.TimeoutSeconds);
                    aiText = BuildLocalReply(cauHoi, lienQuan, "AI đang phản hồi chậm, câu trả lời dưới đây dựa trên nội quy hiện có.");
                }
                catch (GeminiRateLimitException)
                {
                    _logger.LogWarning("Gemini API trả về rate limit (429).");
                    aiText = BuildLocalReply(cauHoi, lienQuan, "Hệ thống AI đang quá tải, câu trả lời dưới đây dựa trên dữ liệu nội quy có sẵn.");
                }
                catch (GeminiApiException ex)
                {
                    _logger.LogError(ex, "Lỗi khi gọi Gemini API. Sử dụng fallback nội bộ.");
                    aiText = BuildLocalReply(cauHoi, lienQuan, "Dịch vụ AI đang gặp sự cố tạm thời, câu trả lời dưới đây dựa trên nội quy hiện có.");
                }
            }

            // Phản hồi rỗng hoặc sai định dạng từ AI -> vẫn trả lời có ích cho người dùng
            // thay vì để lỗi 500 (tiêu chí #7).
            if (string.IsNullOrWhiteSpace(aiText))
            {
                return new ChatbotTraLoiDto
                {
                    TraLoi = "AI không tạo được câu trả lời phù hợp cho câu hỏi này. " +
                             "Bạn vui lòng thử diễn đạt lại câu hỏi hoặc liên hệ Ban quản lý KTX.",
                    NguonNoiQuy = lienQuan.Select(n => n.TieuDe).ToList(),
                    CoCanhBao = true,
                    GhiChu = "Gemini trả về nội dung rỗng."
                };
            }

            var isAiFallback = string.IsNullOrWhiteSpace(_options.ApiKey)
                || aiText.StartsWith("[Fallback]");

            return new ChatbotTraLoiDto
            {
                TraLoi = aiText.Trim(),
                NguonNoiQuy = lienQuan.Select(n => n.TieuDe).ToList(),
                CoCanhBao = isAiFallback,
                GhiChu = isAiFallback
                    ? "Câu trả lời dựa trên nội quy nội bộ của hệ thống, vì Gemini chưa được cấu hình hoặc đang gặp sự cố."
                    : "Câu trả lời do AI (Google Gemini) tạo dựa trên nội quy hiện có, chỉ mang tính tham khảo."
            };
        }

        // Các từ quá phổ biến trong tiếng Việt (hư từ / từ xuất hiện ở gần như mọi điều khoản
        // vì đều nói về "ký túc xá") — loại khỏi từ khóa so khớp để tránh so khớp nhầm, dẫn tới
        // đưa nhầm ngữ cảnh không liên quan vào prompt gửi AI (phát hiện qua kiểm thử thật:
        // câu "ký túc xá còn phòng không" bị khớp nhầm 3 điều khoản không liên quan).
        private static readonly HashSet<string> HuTu = new(new[]
        {
            "không", "còn", "được", "của", "và", "có", "cho", "về", "các", "những",
            "này", "đó", "khi", "để", "là", "ra", "vào", "lên", "xuống", "trong",
            "ngoài", "với", "hay", "thì", "mà", "sẽ", "đã", "rất", "cũng", "nên",
            "mấy", "ký", "túc", "xá", "làm", "gì", "phải", "như", "thế", "nào",
            "bạn", "mình", "tôi", "chúng"
        });

        // Tìm kiếm "lite RAG": chấm điểm các điều khoản nội quy theo số từ khóa trùng khớp
        // với câu hỏi, lấy tối đa 3 điều khoản liên quan nhất làm ngữ cảnh cho AI.
        // Dữ liệu chỉ giới hạn trong bảng NoiQuy (không đưa dữ liệu sinh viên/phí/... vào AI) —
        // đây là cơ chế kiểm soát quyền truy cập dữ liệu của chức năng AI (tiêu chí #5).
        private async Task<List<NoiQuy>> TimNoiQuyLienQuanAsync(string cauHoi)
        {
            var tuKhoa = cauHoi.ToLower()
                .Split(new[] { ' ', ',', '.', '?', '!', ';', ':', '"' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= 3 && !HuTu.Contains(w))
                .Distinct()
                .ToList();

            if (tuKhoa.Count == 0) return new List<NoiQuy>();

            var all = await _db.NoiQuys.ToListAsync();

            return all
                .Select(nq => new
                {
                    NoiQuy = nq,
                    Score = tuKhoa.Count(tk => (nq.TieuDe + " " + nq.NoiDung).ToLower().Contains(tk))
                })
                // Yêu cầu khớp ít nhất 2 từ khóa thật sự liên quan (không chỉ 1 từ chung chung)
                // mới coi là có dữ liệu phù hợp — giảm nguy cơ đưa nhầm ngữ cảnh không liên quan.
                .Where(x => x.Score >= 2)
                .OrderByDescending(x => x.Score)
                .Take(3)
                .Select(x => x.NoiQuy)
                .ToList();
        }

        private string BuildUserPrompt(string cauHoi, List<NoiQuy> lienQuan)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Dữ liệu nội quy liên quan:");
            for (int i = 0; i < lienQuan.Count; i++)
            {
                var noiDung = lienQuan[i].NoiDung;
                if (noiDung.Length > _options.MaxContextChars)
                    noiDung = noiDung.Substring(0, _options.MaxContextChars) + "...";

                sb.AppendLine($"[{i + 1}] {lienQuan[i].TieuDe}: {noiDung}");
            }
            sb.AppendLine();
            sb.AppendLine($"Câu hỏi của người dùng: \"{cauHoi}\"");
            return sb.ToString();
        }

        private string BuildLocalReply(string cauHoi, List<NoiQuy> lienQuan, string prefix = "")
        {
            if (lienQuan.Count == 0)
                return KHONG_TIM_THAY_NOI_QUY;

            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(prefix))
                sb.AppendLine(prefix);

            sb.AppendLine("Theo nội quy hiện hành:");
            foreach (var noiQuy in lienQuan)
            {
                sb.AppendLine($"- {noiQuy.TieuDe}: {noiQuy.NoiDung}");
            }

            sb.AppendLine();
            sb.AppendLine("Nếu bạn muốn, tôi có thể giải thích rõ hơn hoặc hướng dẫn cách xử lý phù hợp với trường hợp cụ thể.");
            return sb.ToString();
        }

        private async Task<string> GoiGeminiAsync(string userPrompt)
        {
            // Lưu ý (phát hiện khi test thật với key mới tạo từ AI Studio): key kiểu mới của
            // Google không được API chấp nhận khi gửi qua query string "?key=..." nữa (Google
            // trả 401 "ACCESS_TOKEN_TYPE_UNSUPPORTED" dù key đúng) — phải gửi qua HTTP header
            // "x-goog-api-key" theo hướng dẫn hiện hành của Google. Cách này còn tốt hơn cho
            // bảo vệ key (tiêu chí #2): key không còn nằm trong URL nên không bị lộ qua log
            // truy cập/URL của client HTTP.
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:generateContent";

            var body = new
            {
                systemInstruction = new { parts = new[] { new { text = _systemPrompt } } },
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = userPrompt } } }
                },
                generationConfig = new { temperature = 0.2, maxOutputTokens = 300 }
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };
            req.Headers.Add("x-goog-api-key", _options.ApiKey);

            HttpResponseMessage res;
            try
            {
                res = await _http.SendAsync(req, cts.Token);
            }
            catch (HttpRequestException ex)
            {
                // Lỗi kết nối (DNS, mất mạng, bị chặn...) — không để lộ URL/API key ra ngoài,
                // chỉ log nội bộ và trả lỗi chung cho client (tiêu chí #2 bảo vệ key, #7 xử lý lỗi).
                _logger.LogError(ex, "Không thể kết nối tới Gemini API.");
                throw new GeminiApiException("Không thể kết nối tới dịch vụ AI. Vui lòng kiểm tra kết nối mạng và thử lại.", ex);
            }

            var raw = await res.Content.ReadAsStringAsync();

            if (res.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                throw new GeminiRateLimitException();

            if (!res.IsSuccessStatusCode)
                throw new GeminiApiException($"Gemini trả về mã lỗi {(int)res.StatusCode}: {raw}");

            try
            {
                using var doc = JsonDocument.Parse(raw);

                if (!doc.RootElement.TryGetProperty("candidates", out var candidates) ||
                    candidates.ValueKind != JsonValueKind.Array || candidates.GetArrayLength() == 0)
                    return "";

                var firstCandidate = candidates.EnumerateArray().First();
                if (!firstCandidate.TryGetProperty("content", out var contentEl) ||
                    !contentEl.TryGetProperty("parts", out var parts) ||
                    parts.ValueKind != JsonValueKind.Array || parts.GetArrayLength() == 0)
                    return "";

                // Gemini có thể trả câu trả lời chia thành NHIỀU đoạn (parts) — trước đây code
                // chỉ lấy đoạn đầu tiên nên câu trả lời bị cắt cụt giữa câu/giữa từ (phát hiện
                // qua kiểm thử thật). Ghép toàn bộ các đoạn lại để có câu trả lời đầy đủ.
                var sbText = new StringBuilder();
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var partTextEl))
                        sbText.Append(partTextEl.GetString() ?? "");
                }
                return sbText.ToString();
            }
            catch (JsonException ex)
            {
                throw new GeminiApiException("Không đọc được phản hồi (JSON không hợp lệ) từ Gemini API.", ex);
            }
        }
    }
}
