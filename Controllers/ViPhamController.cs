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
    [Route("api/vipham")]
    [Authorize(Roles = "QuanLy")]
    public class ViPhamController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ViPhamController(AppDbContext db) => _db = db;

        private static ViPhamResponseDto ToDto(ViPham v) => new ViPhamResponseDto
        {
            Id = v.Id,
            SinhVienId = v.SinhVienId,
            MaSv = v.SinhVien?.MaSv,
            TenSinhVien = v.SinhVien?.NguoiDung?.HoTen,
            DieuKhoan = v.DieuKhoan?.TieuDe,
            MoTa = v.MoTa,
            MucDo = v.MucDo.ToString(),
            NgayLap = v.NgayLap
        };

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? sinhVienId, [FromQuery] MucDoViPham? mucDo)
        {
            var query = _db.ViPhams
                .Include(v => v.SinhVien).ThenInclude(sv => sv.NguoiDung)
                .Include(v => v.DieuKhoan)
                .AsQueryable();

            if (sinhVienId.HasValue) query = query.Where(v => v.SinhVienId == sinhVienId);
            if (mucDo.HasValue) query = query.Where(v => v.MucDo == mucDo);

            var list = await query.OrderByDescending(v => v.NgayLap).ToListAsync();
            return Ok(ApiResponse<object>.Ok(list.Select(ToDto).ToList()));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var v = await _db.ViPhams
                .Include(x => x.SinhVien).ThenInclude(sv => sv.NguoiDung)
                .Include(x => x.DieuKhoan)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (v == null) throw AppException.NotFound($"Không tìm thấy vi phạm có id = {id}.");
            return Ok(ApiResponse<ViPhamResponseDto>.Ok(ToDto(v)));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ViPhamCreateDto dto)
        {
            var sv = await _db.SinhViens.AnyAsync(x => x.Id == dto.SinhVienId);
            if (!sv) throw AppException.NotFound($"Không tìm thấy sinh viên có id = {dto.SinhVienId}.");

            var viPham = new ViPham
            {
                SinhVienId = dto.SinhVienId,
                DieuKhoanId = dto.DieuKhoanId,
                MoTa = dto.MoTa,
                MucDo = dto.MucDo,
                NgayLap = DateTime.UtcNow
            };
            _db.ViPhams.Add(viPham);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = viPham.Id }, ApiResponse<ViPham>.Ok(viPham, "Đã lập biên bản vi phạm."));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var v = await _db.ViPhams.FindAsync(id);
            if (v == null) throw AppException.NotFound($"Không tìm thấy vi phạm có id = {id}.");

            _db.ViPhams.Remove(v);
            await _db.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(null, "Đã xóa biên bản vi phạm."));
        }
    }
}
