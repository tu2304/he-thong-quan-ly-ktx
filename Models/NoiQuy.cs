using System.ComponentModel.DataAnnotations;

namespace KtxAI.Api.Models
{
    public class NoiQuy
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string TieuDe { get; set; }

        [Required]
        public string NoiDung { get; set; }

        [MaxLength(20)]
        public string PhienBan { get; set; } = "1.0";

        public ICollection<ViPham> ViPhams { get; set; } = new List<ViPham>();
    }
}
