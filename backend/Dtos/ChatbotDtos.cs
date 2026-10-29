namespace KtxAI.Api.Dtos
{
    public class ChatbotHoiDto
    {
        public string CauHoi { get; set; }
    }

    public class ChatbotTraLoiDto
    {
        // Câu trả lời AI tạo ra (hoặc câu trả lời mặc định khi không có dữ liệu phù hợp).
        public string TraLoi { get; set; }

        // Nguồn nội quy, dữ liệu cá nhân hoặc hướng dẫn nghiệp vụ dùng cho câu trả lời.
        // (tiêu chí #6 - hiển thị rõ ràng, giúp người dùng biết thông tin lấy từ đâu).
        public List<string> NguonNoiQuy { get; set; } = new();

        // true nếu đây là câu trả lời cảnh báo/giới hạn (không phải do AI tạo),
        // để frontend hiển thị khác màu/icon (tiêu chí #6).
        public bool CoCanhBao { get; set; }

        public string GhiChu { get; set; }
    }
}
