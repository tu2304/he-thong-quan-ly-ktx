using System.ComponentModel.DataAnnotations;
using KtxAI.Api.Models;

namespace KtxAI.Api.Dtos
{
    public class ViPhamCreateDto
    {
        [Required]
        public int SinhVienId { get; set; }

        public int? DieuKhoanId { get; set; }

        [Required]
        public string MoTa { get; set; }

        [Required]
        public MucDoViPham MucDo { get; set; }
    }

    public class ViPhamResponseDto
    {
        public int Id { get; set; }
        public int SinhVienId { get; set; }
        public string MaSv { get; set; }
        public string TenSinhVien { get; set; }
        public string DieuKhoan { get; set; }
        public string MoTa { get; set; }
        public string MucDo { get; set; }
        public DateTime NgayLap { get; set; }
    }
}
