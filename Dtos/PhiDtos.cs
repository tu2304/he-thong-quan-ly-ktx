using System.ComponentModel.DataAnnotations;
using KtxAI.Api.Models;

namespace KtxAI.Api.Dtos
{
    public class PhiCreateDto
    {
        [Required]
        public int HopDongId { get; set; }

        [Required]
        public LoaiPhi LoaiPhi { get; set; }

        [Range(0, double.MaxValue)]
        public decimal SoTien { get; set; }

        [Required]
        public DateTime HanThanhToan { get; set; }
    }

    public class PhiResponseDto
    {
        public int Id { get; set; }
        public int HopDongId { get; set; }
        public string MaSv { get; set; }
        public string TenSinhVien { get; set; }
        public string SoPhong { get; set; }
        public string LoaiPhi { get; set; }
        public decimal SoTien { get; set; }
        public DateTime HanThanhToan { get; set; }
        public string TrangThaiThanhToan { get; set; }
    }
}
