using System.ComponentModel.DataAnnotations;

namespace KtxAI.Api.Dtos
{
    public class LoginRequestDto
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string MatKhau { get; set; }
    }

    public class LoginResponseDto
    {
        public string Token { get; set; }
        public string HoTen { get; set; }
        public string Email { get; set; }
        public string VaiTro { get; set; }
        public DateTime HetHan { get; set; }
    }
}
