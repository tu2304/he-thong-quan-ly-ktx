using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KtxAI.Api.Common;
using KtxAI.Api.Data;
using KtxAI.Api.Models;

namespace KtxAI.Api.Controllers
{
    [ApiController]
    [Route("api/noiquy")]
    [Authorize(Roles = "QuanLy,KeToan,SinhVien")]
    public class NoiQuyController : ControllerBase
    {
        private readonly AppDbContext _db;
        public NoiQuyController(AppDbContext db) => _db = db;

        // GET /api/noiquy?keyword=giờ giới nghiêm — tìm kiếm nội quy theo từ khóa (tiêu chí #4)
        // Đây cũng là dữ liệu nguồn cho chatbot AI sẽ tích hợp ở KT3.
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string keyword)
        {
            var query = _db.NoiQuys.AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(n => n.TieuDe.Contains(keyword) || n.NoiDung.Contains(keyword));

            var list = await query.OrderBy(n => n.Id).ToListAsync();
            return Ok(ApiResponse<object>.Ok(list));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var nq = await _db.NoiQuys.FindAsync(id);
            if (nq == null) throw AppException.NotFound($"Không tìm thấy điều khoản nội quy có id = {id}.");
            return Ok(ApiResponse<NoiQuy>.Ok(nq));
        }

        [HttpPost]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> Create([FromBody] NoiQuy dto)
        {
            var nq = new NoiQuy { TieuDe = dto.TieuDe, NoiDung = dto.NoiDung, PhienBan = string.IsNullOrWhiteSpace(dto.PhienBan) ? "1.0" : dto.PhienBan };
            _db.NoiQuys.Add(nq);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = nq.Id }, ApiResponse<NoiQuy>.Ok(nq, "Đã thêm điều khoản nội quy."));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> Update(int id, [FromBody] NoiQuy dto)
        {
            var nq = await _db.NoiQuys.FindAsync(id);
            if (nq == null) throw AppException.NotFound($"Không tìm thấy điều khoản nội quy có id = {id}.");

            nq.TieuDe = dto.TieuDe;
            nq.NoiDung = dto.NoiDung;
            nq.PhienBan = dto.PhienBan;

            await _db.SaveChangesAsync();
            return Ok(ApiResponse<NoiQuy>.Ok(nq, "Đã cập nhật điều khoản nội quy."));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> Delete(int id)
        {
            var nq = await _db.NoiQuys.FindAsync(id);
            if (nq == null) throw AppException.NotFound($"Không tìm thấy điều khoản nội quy có id = {id}.");

            _db.NoiQuys.Remove(nq);
            await _db.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(null, "Đã xóa điều khoản nội quy."));
        }
    }
}
