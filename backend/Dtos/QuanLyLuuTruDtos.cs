using System.ComponentModel.DataAnnotations;
using KtxAI.Api.Models;

namespace KtxAI.Api.Dtos
{
    // ----- Tòa nhà -----
    public class ToaNhaCreateDto
    {
        [Required, MaxLength(50)]
        public string TenToa { get; set; }
    }

    // ----- Tầng -----
    public class TangCreateDto
    {
        [Required]
        public int ToaId { get; set; }

        [Range(1, 100)]
        public int SoTang { get; set; }
    }

    // ----- Phòng -----
    public class PhongCreateDto
    {
        [Required]
        public int TangId { get; set; }

        [Required, MaxLength(10)]
        public string SoPhong { get; set; }

        [MaxLength(30)]
        public string LoaiPhong { get; set; }

        [Range(1, 20)]
        public int SucChua { get; set; }
    }

    public class PhongUpdateDto
    {
        [Required, MaxLength(10)]
        public string SoPhong { get; set; }

        [MaxLength(30)]
        public string LoaiPhong { get; set; }

        [Range(1, 20)]
        public int SucChua { get; set; }

        [Required]
        public TrangThaiPhong TrangThai { get; set; }
    }

    public class PhongResponseDto
    {
        public int Id { get; set; }
        public string SoPhong { get; set; }
        public string LoaiPhong { get; set; }
        public int SucChua { get; set; }
        public string TrangThai { get; set; }
        public string TenToa { get; set; }
        public int SoTang { get; set; }
        public int SoGiuongTrong { get; set; }
        public int TongSoGiuong { get; set; }
    }

    // ----- Giường -----
    public class GiuongCreateDto
    {
        [Required]
        public int PhongId { get; set; }

        [Required, MaxLength(10)]
        public string SoGiuong { get; set; }
    }

    public class GiuongResponseDto
    {
        public int Id { get; set; }
        public string SoGiuong { get; set; }
        public string TrangThai { get; set; }
        public int PhongId { get; set; }
        public string SoPhong { get; set; }
    }
}
