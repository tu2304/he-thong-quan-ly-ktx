namespace KtxAI.Api.Models
{
    public class Tang
    {
        public int Id { get; set; }

        public int ToaId { get; set; }
        public ToaNha ToaNha { get; set; }

        public int SoTang { get; set; }

        public ICollection<Phong> Phongs { get; set; } = new List<Phong>();
    }
}
