namespace KtxAI.Api.Dtos
{
    public class ThongKeTongQuanDto
    {
        public int TongSoPhong { get; set; }
        public int TongSoGiuong { get; set; }
        public int SoGiuongDangO { get; set; }
        public int SoGiuongTrong { get; set; }
        public double TiLeLapDay { get; set; } // %

        public decimal TongPhiChuaThu { get; set; }
        public decimal TongPhiQuaHan { get; set; }
        public decimal TongPhiDaThu { get; set; }

        public int SoPhanAnhMoi { get; set; }
        public int SoPhanAnhDangXuLy { get; set; }
        public int SoPhanAnhDaXuLy { get; set; }

        public int TongSoSinhVien { get; set; }
        public int TongSoViPham { get; set; }
    }

    public class ThongKePhanAnhTheoNhomDto
    {
        public string NhomVanDe { get; set; }
        public int SoLuong { get; set; }
    }
}
