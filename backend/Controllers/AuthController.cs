using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KtxAI.Api.Common;
using KtxAI.Api.Data;
using KtxAI.Api.Dtos;
using KtxAI.Api.Services;

namespace KtxAI.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IJwtService _jwtService;

        public AuthController(AppDbContext db, IJwtService jwtService)
        {
            _db = db;
            _jwtService = jwtService;
        }

        // POST /api/auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var nguoiDung = await _db.NguoiDungs
                .FirstOrDefaultAsync(x => x.Email == request.Email);

            if (nguoiDung == null || !nguoiDung.DangHoatDong)
            {
                // Không tiết lộ email có tồn tại hay không, tránh dò tài khoản
                return Unauthorized(ApiResponse<object>.Fail("Email hoặc mật khẩu không đúng."));
            }

            var hasher = new PasswordHasher<Models.NguoiDung>();
            var result = hasher.VerifyHashedPassword(nguoiDung, nguoiDung.MatKhauHash, request.MatKhau);

            if (result == PasswordVerificationResult.Failed)
            {
                return Unauthorized(ApiResponse<object>.Fail("Email hoặc mật khẩu không đúng."));
            }

            var (token, hetHan) = _jwtService.GenerateToken(nguoiDung);

            var response = new LoginResponseDto
            {
                Token = token,
                HoTen = nguoiDung.HoTen,
                Email = nguoiDung.Email,
                VaiTro = nguoiDung.VaiTro.ToString(),
                HetHan = hetHan
            };

            return Ok(ApiResponse<LoginResponseDto>.Ok(response, "Đăng nhập thành công"));
        }

        // GET /api/auth/me — kiểm tra token còn hợp lệ và lấy thông tin người dùng hiện tại
        [HttpGet("me")]
        [Authorize(Roles = "QuanLy,KeToan,SinhVien")]
        public IActionResult Me()
        {
            var hoTen = User.Identity?.Name;
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var vaiTro = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            return Ok(ApiResponse<object>.Ok(new { hoTen, email, vaiTro }));
        }
    }
}
