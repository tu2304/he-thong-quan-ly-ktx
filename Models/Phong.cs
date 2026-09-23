using System.ComponentModel.DataAnnotations;

namespace KtxAI.Api.Models
{
    public class Phong
    {
        public int Id { get; set; }

        public int TangId { get; set; }
        public Tang Tang { get; set; }

        [Required, MaxLength(10)]
        public string SoPhong { get; set; }

        [MaxLength(30)]
        public string LoaiPhong { get; set; }

        [Range(1, 20)]
        public int SucChua { get; set; }

        public TrangThaiPhong TrangThai { get; set; } = TrangThaiPhong.ConTrong;

        public ICollection<Giuong> Giuongs { get; set; } = new List<Giuong>();
        public ICollection<PhanAnh> PhanAnhs { get; set; } = new List<PhanAnh>();
    }
}
