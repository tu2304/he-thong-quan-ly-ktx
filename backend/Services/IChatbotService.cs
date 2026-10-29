using KtxAI.Api.Dtos;

namespace KtxAI.Api.Services
{
    public interface IChatbotService
    {
        Task<ChatbotTraLoiDto> HoiAsync(string cauHoi);
    }
}
