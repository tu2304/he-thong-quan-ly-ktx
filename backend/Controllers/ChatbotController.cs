using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KtxAI.Api.Common;
using KtxAI.Api.Dtos;
using KtxAI.Api.Services;

namespace KtxAI.Api.Controllers
{
    [ApiController]
    [Route("api/chatbot")]
    [Authorize(Roles = "QuanLy,KeToan,SinhVien")] // Yêu cầu đăng nhập (mọi vai trò) — tránh lạm dụng API AI trả phí/giới hạn tốc độ.
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _chatbot;
        private readonly StudentChatbotService _studentChatbot;
        private readonly GeminiOptions _options;
        public ChatbotController(IChatbotService chatbot, StudentChatbotService studentChatbot,
            Microsoft.Extensions.Options.IOptions<GeminiOptions> options)
        {
            _chatbot = chatbot;
            _studentChatbot = studentChatbot;
            _options = options.Value;
        }

        // POST /api/chatbot/hoi — hỏi đáp nội quy KTX bằng AI (KT3, chức năng AI chính).
        [HttpPost("hoi")]
        public async Task<IActionResult> Hoi([FromBody] JsonElement? payload)
        {
            var cauHoi = "";

            if (payload.HasValue)
            {
                if (payload.Value.ValueKind == JsonValueKind.Object &&
                    payload.Value.TryGetProperty("cauHoi", out var cauHoiEl))
                {
                    if (cauHoiEl.ValueKind != JsonValueKind.String)
                        throw new AppException("Câu hỏi phải là chuỗi văn bản.", 400);
                    cauHoi = cauHoiEl.GetString() ?? "";
                }
                else if (payload.Value.ValueKind == JsonValueKind.String)
                {
                    cauHoi = payload.Value.GetString() ?? "";
                }
            }

            if (string.IsNullOrWhiteSpace(cauHoi))
                throw new AppException("Câu hỏi không được để trống.", 400);

            cauHoi = cauHoi.Trim();
            if (cauHoi.Length > _options.MaxQuestionChars)
                throw new AppException($"Câu hỏi quá dài (tối đa {_options.MaxQuestionChars} ký tự).", 400);
            var ketQua = await _studentChatbot.AnswerAsync(cauHoi, User)
                ?? await _chatbot.HoiAsync(cauHoi);
            return Ok(ApiResponse<ChatbotTraLoiDto>.Ok(ketQua));
        }
    }
}
