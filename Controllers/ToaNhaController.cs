using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KtxAI.Api.Common;
using KtxAI.Api.Data;
using KtxAI.Api.Dtos;
using KtxAI.Api.Models;

namespace KtxAI.Api.Controllers
{
    [ApiController]
    [Route("api/toanha")]
    [Authorize] // Bắt buộc đăng nhập; chỉ Quản lý mới được thêm/sửa/xóa (kiểm tra ở từng action)
    public class ToaNhaController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ToaNhaController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _db.ToaNhas
                .Select(t => new { t.Id, t.TenToa, SoTang = t.Tangs.Count })
                .ToListAsync();
            return Ok(ApiResponse<object>.Ok(list));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var toa = await _db.ToaNhas.FindAsync(id);
            if (toa == null) throw AppException.NotFound($"Không tìm thấy tòa nhà có id = {id}.");
            return Ok(ApiResponse<ToaNha>.Ok(toa));
        }

        [HttpPost]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> Create([FromBody] ToaNhaCreateDto dto)
        {
            var toa = new ToaNha { TenToa = dto.TenToa };
            _db.ToaNhas.Add(toa);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = toa.Id }, ApiResponse<ToaNha>.Ok(toa, "Đã tạo tòa nhà."));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> Update(int id, [FromBody] ToaNhaCreateDto dto)
        {
            var toa = await _db.ToaNhas.FindAsync(id);
            if (toa == null) throw AppException.NotFound($"Không tìm thấy tòa nhà có id = {id}.");

            toa.TenToa = dto.TenToa;
            await _db.SaveChangesAsync();
            return Ok(ApiResponse<ToaNha>.Ok(toa, "Đã cập nhật tòa nhà."));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> Delete(int id)
        {
            var toa = await _db.ToaNhas.Include(t => t.Tangs).FirstOrDefaultAsync(t => t.Id == id);
            if (toa == null) throw AppException.NotFound($"Không tìm thấy tòa nhà có id = {id}.");

            if (toa.Tangs.Any())
                throw AppException.Conflict("Không thể xóa tòa nhà đang còn tầng/phòng bên trong.");

            _db.ToaNhas.Remove(toa);
            await _db.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(null, "Đã xóa tòa nhà."));
        }
    }
}
