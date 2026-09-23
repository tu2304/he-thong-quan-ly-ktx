namespace KtxAI.Api.Models
{
    public class Phi
    {
        public int Id { get; set; }

        public int HopDongId { get; set; }
        public HopDong HopDong { get; set; }

        public LoaiPhi LoaiPhi { get; set; }

        public decimal SoTien { get; set; }

        public DateTime HanThanhToan { get; set; }

        public TrangThaiThanhToan TrangThaiThanhToan { get; set; } = TrangThaiThanhToan.ChuaThu;
    }
}
