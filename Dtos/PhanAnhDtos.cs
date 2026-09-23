using System.ComponentModel.DataAnnotations;
using KtxAI.Api.Models;

namespace KtxAI.Api.Dtos
{
    public class PhanAnhCreateDto
    {
        [Required]
        public int SinhVienId { get; set; }

        public int? PhongId { get; set; }

        [Required]
        public string NoiDung { get; set; }
    }

    public class PhanAnhUpdateTrangThaiDto
    {
        [Required]
        public TrangThaiPhanAnh TrangThai { get; set; }

        [MaxLength(50)]
        public string NhomVanDe { get; set; }
    }

    public class PhanAnhResponseDto
    {
        public int Id { get; set; }
        public int SinhVienId { get; set; }
        public string TenSinhVien { get; set; }
        public int? PhongId { get; set; }
        public string SoPhong { get; set; }
        public string NoiDung { get; set; }
        public string NhomVanDe { get; set; }
        public string TrangThai { get; set; }
        public DateTime NgayTao { get; set; }
    }
}
