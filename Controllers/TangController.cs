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
    [Route("api/tang")]
    [Authorize]
    public class TangController : ControllerBase
    {
        private readonly AppDbContext _db;
        public TangController(AppDbContext db) => _db = db;

        // GET /api/tang?toaId=1
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? toaId)
        {
            var query = _db.Tangs.Include(t => t.ToaNha).AsQueryable();
            if (toaId.HasValue) query = query.Where(t => t.ToaId == toaId);

            var list = await query
                .Select(t => new { t.Id, t.SoTang, t.ToaId, TenToa = t.ToaNha.TenToa, SoPhong = t.Phongs.Count })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(list));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var tang = await _db.Tangs.FindAsync(id);
            if (tang == null) throw AppException.NotFound($"Không tìm thấy tầng có id = {id}.");
            return Ok(ApiResponse<Tang>.Ok(tang));
        }

        [HttpPost]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> Create([FromBody] TangCreateDto dto)
        {
            var toaTonTai = await _db.ToaNhas.AnyAsync(t => t.Id == dto.ToaId);
            if (!toaTonTai) throw AppException.NotFound($"Không tìm thấy tòa nhà có id = {dto.ToaId}.");

            var tang = new Tang { ToaId = dto.ToaId, SoTang = dto.SoTang };
            _db.Tangs.Add(tang);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = tang.Id }, ApiResponse<Tang>.Ok(tang, "Đã tạo tầng."));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> Update(int id, [FromBody] TangCreateDto dto)
        {
            var tang = await _db.Tangs.FindAsync(id);
            if (tang == null) throw AppException.NotFound($"Không tìm thấy tầng có id = {id}.");

            tang.SoTang = dto.SoTang;
            tang.ToaId = dto.ToaId;
            await _db.SaveChangesAsync();
            return Ok(ApiResponse<Tang>.Ok(tang, "Đã cập nhật tầng."));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> Delete(int id)
        {
            var tang = await _db.Tangs.Include(t => t.Phongs).FirstOrDefaultAsync(t => t.Id == id);
            if (tang == null) throw AppException.NotFound($"Không tìm thấy tầng có id = {id}.");

            if (tang.Phongs.Any())
                throw AppException.Conflict("Không thể xóa tầng đang còn phòng bên trong.");

            _db.Tangs.Remove(tang);
            await _db.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(null, "Đã xóa tầng."));
        }
    }
}
