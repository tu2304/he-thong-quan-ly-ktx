using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using KtxAI.Api.Models;

namespace KtxAI.Api.Services
{
    public interface IJwtService
    {
        (string token, DateTime hetHan) GenerateToken(NguoiDung nguoiDung);
    }

    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        public (string token, DateTime hetHan) GenerateToken(NguoiDung nguoiDung)
        {
            var jwtKey = _config["Jwt:Key"]
                ?? throw new InvalidOperationException("Thiếu cấu hình Jwt:Key trong appsettings/.env");
            var jwtIssuer = _config["Jwt:Issuer"] ?? "KtxAI.Api";
            var expireMinutes = int.TryParse(_config["Jwt:ExpireMinutes"], out var m) ? m : 120;

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, nguoiDung.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, nguoiDung.Id.ToString()),
                new Claim(ClaimTypes.Name, nguoiDung.HoTen),
                new Claim(ClaimTypes.Email, nguoiDung.Email),
                new Claim(ClaimTypes.Role, nguoiDung.VaiTro.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var hetHan = DateTime.UtcNow.AddMinutes(expireMinutes);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtIssuer,
                claims: claims,
                expires: hetHan,
                signingCredentials: creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), hetHan);
        }
    }
}
