using System.ComponentModel.DataAnnotations;

namespace KtxAI.Api.Dtos
{
    // Xếp phòng cho sinh viên (tạo hợp đồng mới)
    public class XepPhongDto
    {
        [Required]
        public int SinhVienId { get; set; }

        [Required]
        public int GiuongId { get; set; }
    }

    // Chuyển sang giường khác
    public class ChuyenPhongDto
    {
        [Required]
        public int GiuongMoiId { get; set; }
    }

    public class HopDongResponseDto
    {
        public int Id { get; set; }
        public int SinhVienId { get; set; }
        public string TenSinhVien { get; set; }
        public string MaSv { get; set; }
        public string SoPhong { get; set; }
        public string SoGiuong { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public string TrangThai { get; set; }
    }
}
