namespace KtxAI.Api.Models
{
    public class ViPham
    {
        public int Id { get; set; }

        public int SinhVienId { get; set; }
        public SinhVien SinhVien { get; set; }

        public int? DieuKhoanId { get; set; }
        public NoiQuy DieuKhoan { get; set; }

        public string MoTa { get; set; }

        public MucDoViPham MucDo { get; set; } = MucDoViPham.Nhe;

        public DateTime NgayLap { get; set; } = DateTime.UtcNow;
    }
}
