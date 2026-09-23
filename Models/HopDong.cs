namespace KtxAI.Api.Models
{
    public class HopDong
    {
        public int Id { get; set; }

        public int SinhVienId { get; set; }
        public SinhVien SinhVien { get; set; }

        public int GiuongId { get; set; }
        public Giuong Giuong { get; set; }

        public DateTime NgayBatDau { get; set; } = DateTime.UtcNow;
        public DateTime? NgayKetThuc { get; set; }

        public TrangThaiHopDong TrangThai { get; set; } = TrangThaiHopDong.DangHieuLuc;

        public ICollection<Phi> Phis { get; set; } = new List<Phi>();
    }
}
