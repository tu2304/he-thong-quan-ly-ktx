using System.ComponentModel.DataAnnotations;
using KtxAI.Api.Models;

namespace KtxAI.Api.Dtos
{
    public class SinhVienCreateDto
    {
        [Required, MaxLength(100)]
        public string HoTen { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, MinLength(6)]
        public string MatKhau { get; set; }

        [Required, MaxLength(20)]
        public string MaSv { get; set; }

        [Required]
        public GioiTinh GioiTinh { get; set; }

        [Required]
        public DateTime NgaySinh { get; set; }

        [MaxLength(50)]
        public string Lop { get; set; }

        [MaxLength(15)]
        public string Sdt { get; set; }
    }

    public class SinhVienUpdateDto
    {
        [Required, MaxLength(100)]
        public string HoTen { get; set; }

        [MaxLength(50)]
        public string Lop { get; set; }

        [MaxLength(15)]
        public string Sdt { get; set; }

        [Required]
        public GioiTinh GioiTinh { get; set; }
    }

    public class SinhVienResponseDto
    {
        public int Id { get; set; }
        public string MaSv { get; set; }
        public string HoTen { get; set; }
        public string Email { get; set; }
        public string GioiTinh { get; set; }
        public DateTime NgaySinh { get; set; }
        public string Lop { get; set; }
        public string Sdt { get; set; }
        public string PhongHienTai { get; set; } // null nếu chưa xếp phòng
    }
}
