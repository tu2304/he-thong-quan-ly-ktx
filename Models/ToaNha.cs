using System.ComponentModel.DataAnnotations;

namespace KtxAI.Api.Models
{
    public class ToaNha
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string TenToa { get; set; }

        public ICollection<Tang> Tangs { get; set; } = new List<Tang>();
    }
}
