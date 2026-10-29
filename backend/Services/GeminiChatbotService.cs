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

    // Chatbot hỏi đáp nội quy KTX bằng AI (Gemini) — chức năng AI chính của KT3.
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
            "Mình chưa có đủ dữ liệu để trả lời câu hỏi này. Bạn có thể hỏi: Tôi đang ở phòng nào? " +
            "Tôi còn nợ bao nhiêu? Phản ánh của tôi xử lý chưa? Làm sao chuyển phòng? Hoặc hỏi cụ thể về nội quy KTX. " +
            "Với việc cần xét duyệt, vui lòng liên hệ Ban quản lý.";

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

            var thongTinPhong = await TraCuuPhongTrongAsync(cauHoi);
            if (thongTinPhong != null) return thongTinPhong;

            var lienQuan = await TimNoiQuyLienQuanAsync(cauHoi);

            // A phrasing that misses keyword retrieval still reaches Gemini with bounded,
            // read-only public context. The model never receives a database query tool.
            if (lienQuan.Count == 0)
                lienQuan = await _db.NoiQuys.AsNoTracking().OrderBy(n => n.Id).Take(20).ToListAsync();
            var publicContext = await BuildPublicContextAsync();
            var userPrompt = BuildUserPrompt(cauHoi, lienQuan) + "\n" + publicContext;
            string aiText;
            var isAiFallback = false;

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _logger.LogWarning("Chưa cấu hình Gemini API key (Gemini__ApiKey trong .env). Dùng phản hồi dựa trên nội quy nội bộ.");
                isAiFallback = true;
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
                    isAiFallback = true;
                    aiText = BuildLocalReply(cauHoi, lienQuan, "AI đang phản hồi chậm, câu trả lời dưới đây dựa trên nội quy hiện có.");
                }
                catch (GeminiRateLimitException)
                {
                    _logger.LogWarning("Gemini API trả về rate limit (429).");
                    isAiFallback = true;
                    aiText = BuildLocalReply(cauHoi, lienQuan, "Hệ thống AI đang quá tải, câu trả lời dưới đây dựa trên dữ liệu nội quy có sẵn.");
                }
                catch (GeminiApiException ex)
                {
                    _logger.LogError(ex, "Lỗi khi gọi Gemini API. Sử dụng fallback nội bộ.");
                    isAiFallback = true;
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



            return new ChatbotTraLoiDto
            {
                TraLoi = aiText.Trim(),
                NguonNoiQuy = lienQuan.Select(n => n.TieuDe)
                    .Concat(new[] { "Số liệu công khai trong SQL Server", "Hướng dẫn chức năng hệ thống" }).ToList(),
                CoCanhBao = isAiFallback,
                GhiChu = isAiFallback
                    ? "Câu trả lời dựa trên nội quy nội bộ của hệ thống, vì Gemini chưa được cấu hình hoặc đang gặp sự cố."
                    : "Câu trả lời do Gemini diễn giải từ nội quy, số liệu công khai và hướng dẫn chức năng của hệ thống."
            };
        }

        private async Task<ChatbotTraLoiDto> TraCuuPhongTrongAsync(string cauHoi)
        {
            var normalized = string.Concat(cauHoi.ToLowerInvariant().Normalize(NormalizationForm.FormD)
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) !=
                    System.Globalization.UnicodeCategory.NonSpacingMark));
            if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, @"\b(phong|giuong|toa|tang)\b") ||
                !System.Text.RegularExpressions.Regex.IsMatch(normalized, @"bao nhieu|may (phong|giuong|toa|tang)|so luong|tong so|con .*khong|con khong"))
                return null;

            // Only aggregate public availability; never expose occupant or contract data.
            if (System.Text.RegularExpressions.Regex.IsMatch(normalized, @"\b(toa|tang)\s+[a-z0-9]\b|\d|\b[a-z]+\d+\b"))
                return new ChatbotTraLoiDto
                {
                    TraLoi = "Bạn có thể hỏi tổng số phòng hoặc giường trống của toàn KTX. Để xem theo tòa, tầng hoặc phòng cụ thể, vui lòng mở mục Phòng và dùng bộ lọc.",
                    NguonNoiQuy = new List<string>(),
                    CoCanhBao = true,
                    GhiChu = "Chưa hỗ trợ lọc phòng qua câu hỏi."
                };

            var rooms = await _db.Phongs.AsNoTracking()
                .Select(p => new
                {
                    p.TrangThai,
                    Beds = p.Giuongs.Count(),
                    FreeBeds = p.Giuongs.Count(g => g.TrangThai == TrangThaiGiuong.Trong)
                }).ToListAsync();
            var totals = $"Toàn KTX có tổng cộng {rooms.Count} phòng, {rooms.Sum(p => p.Beds)} giường, {await _db.ToaNhas.CountAsync()} tòa nhà và {await _db.Tangs.CountAsync()} tầng.";
            var available = rooms.Where(p => p.TrangThai == TrangThaiPhong.ConTrong && p.FreeBeds > 0).ToList();
            var completelyEmpty = available.Count(p => p.Beds == p.FreeBeds);
            return new ChatbotTraLoiDto
            {
                TraLoi = totals + "\n" + $"Hiện toàn KTX có {available.Count} phòng còn chỗ, với tổng cộng {available.Sum(p => p.FreeBeds)} giường trống. " +
                    $"Trong đó có {completelyEmpty} phòng chưa có người ở. Không tính phòng đầy hoặc đang khóa. " +
                    "Bạn có thể mở mục Phòng để xem chi tiết.",
                NguonNoiQuy = new List<string> { "Dữ liệu phòng và giường hiện tại trong hệ thống" },
                CoCanhBao = false,
                GhiChu = "Số liệu được truy vấn trực tiếp từ SQL Server tại thời điểm hỏi."
            };
        }

        private async Task<string> BuildPublicContextAsync()
        {
            var rooms = await _db.Phongs.AsNoTracking().Select(p => new
            {
                p.TrangThai,
                Beds = p.Giuongs.Count(),
                FreeBeds = p.Giuongs.Count(g => g.TrangThai == TrangThaiGiuong.Trong)
            }).ToListAsync();
            var available = rooms.Where(p => p.TrangThai == TrangThaiPhong.ConTrong && p.FreeBeds > 0).ToList();
            return $"Dữ liệu công khai tại thời điểm hỏi: tổng {rooms.Count} phòng, {rooms.Sum(p => p.Beds)} giường, " +
                $"{await _db.ToaNhas.CountAsync()} tòa nhà, {await _db.Tangs.CountAsync()} tầng; " +
                $"{available.Count} phòng còn chỗ, {available.Sum(p => p.FreeBeds)} giường còn chỗ có thể xếp; " +
                $"{available.Count(p => p.Beds == p.FreeBeds)} phòng hoàn toàn chưa có người ở. Không tính phòng đầy/khóa vào chỗ còn có thể xếp.\n" +
                "Hướng dẫn nghiệp vụ đã xác nhận: sinh viên xem hóa đơn tại Hóa đơn của tôi; gửi phản ánh tại Phản ánh; " +
                "xem chỗ trống tại Phòng. Chỉ Quản lý sửa hồ sơ và xếp/chuyển/trả phòng; Quản lý hoặc Kế toán xác nhận đã thu. " +
                "Sinh viên liên hệ Ban quản lý để đăng ký/chuyển/trả phòng hoặc sửa hồ sơ; liên hệ Kế toán về cách nộp tiền. " +
                "Chưa có thanh toán trực tuyến, gia hạn hoặc gửi yêu cầu chuyển phòng qua chatbot. " +
                "Không có dữ liệu về giá phòng mới, lịch làm việc, số điện thoại liên hệ hoặc chính sách ngoài các nội quy đã cung cấp. " +
                "Không có dữ liệu cá nhân trong ngữ cảnh này. Nếu người dùng hỏi dữ liệu của mình mà chưa được tra cứu, " +
                "hướng dẫn hỏi Hồ sơ của tôi / Hợp đồng của tôi / Hóa đơn của tôi / Phản ánh của tôi; không suy đoán.";
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
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));
            using var request = new HttpRequestMessage(HttpMethod.Post, $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(_options.Model)}:generateContent");
            request.Headers.Add("x-goog-api-key", _options.ApiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(new
            {
                systemInstruction = new { parts = new[] { new { text = _systemPrompt } } },
                contents = new[] { new { role = "user", parts = new[] { new { text = userPrompt } } } },
                generationConfig = new { maxOutputTokens = _options.MaxOutputTokens }
            }), Encoding.UTF8, "application/json");

            try
            {
                using var response = await _http.SendAsync(request, cts.Token);
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    throw new GeminiRateLimitException();
                if (!response.IsSuccessStatusCode)
                    throw new GeminiApiException($"Gemini trả về mã lỗi {(int)response.StatusCode}.");

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cts.Token));
                var root = doc.RootElement;
                if (!root.TryGetProperty("candidates", out var candidates) ||
                    candidates.ValueKind != JsonValueKind.Array || candidates.GetArrayLength() == 0)
                    throw new GeminiApiException("Gemini không trả về câu trả lời.");
                var candidate = candidates[0];
                if (!candidate.TryGetProperty("finishReason", out var finish) || finish.GetString() != "STOP")
                    throw new GeminiApiException("Gemini chưa hoàn thành câu trả lời.");
                if (!candidate.TryGetProperty("content", out var content) ||
                    !content.TryGetProperty("parts", out var parts) || parts.ValueKind != JsonValueKind.Array)
                    throw new GeminiApiException("Gemini trả về dữ liệu không hợp lệ.");

                var text = new StringBuilder();
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("thought", out var thought) && thought.ValueKind == JsonValueKind.True)
                        continue;
                    if (part.TryGetProperty("text", out var partText))
                        text.Append(partText.GetString());
                }
                if (text.Length == 0)
                    throw new GeminiApiException("Gemini không trả về nội dung văn bản.");
                return text.ToString().Trim();
            }
            catch (HttpRequestException)
            {
                throw new GeminiApiException("Không thể kết nối tới dịch vụ Gemini.");
            }
            catch (JsonException)
            {
                throw new GeminiApiException("Không đọc được phản hồi JSON từ Gemini.");
            }
            catch (InvalidOperationException)
            {
                throw new GeminiApiException("Gemini trả về định dạng dữ liệu không hợp lệ.");
            }
        }
    }
}
