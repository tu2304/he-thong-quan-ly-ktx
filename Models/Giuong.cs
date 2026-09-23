using System.ComponentModel.DataAnnotations;

namespace KtxAI.Api.Models
{
    public class Giuong
    {
        public int Id { get; set; }

        public int PhongId { get; set; }
        public Phong Phong { get; set; }

        [Required, MaxLength(10)]
        public string SoGiuong { get; set; }

        public TrangThaiGiuong TrangThai { get; set; } = TrangThaiGiuong.Trong;

        public ICollection<HopDong> HopDongs { get; set; } = new List<HopDong>();
    }
}
