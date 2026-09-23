using System.ComponentModel.DataAnnotations;

namespace KtxAI.Api.Models
{
    public class PhanAnh
    {
        public int Id { get; set; }

        public int SinhVienId { get; set; }
        public SinhVien SinhVien { get; set; }

        public int? PhongId { get; set; }
        public Phong Phong { get; set; }

        [Required]
        public string NoiDung { get; set; }

        [MaxLength(50)]
        public string NhomVanDe { get; set; }

        public TrangThaiPhanAnh TrangThai { get; set; } = TrangThaiPhanAnh.Moi;

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    }
}
