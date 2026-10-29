using System.ComponentModel.DataAnnotations;

namespace KtxAI.Api.Models
{
    public class SinhVien
    {
        public int Id { get; set; }

        [Required]
        public int NguoiDungId { get; set; }
        public NguoiDung NguoiDung { get; set; }

        [Required, MaxLength(20)]
        public string MaSv { get; set; }

        [Required]
        public GioiTinh GioiTinh { get; set; }

        public DateTime NgaySinh { get; set; }

        [MaxLength(50)]
        public string Lop { get; set; }

        [MaxLength(15)]
        public string Sdt { get; set; }

        public ICollection<HopDong> HopDongs { get; set; } = new List<HopDong>();
        public ICollection<PhanAnh> PhanAnhs { get; set; } = new List<PhanAnh>();
        public ICollection<ViPham> ViPhams { get; set; } = new List<ViPham>();
    }
}
