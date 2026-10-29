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
    [Route("api/giuong")]
    [Authorize(Roles = "QuanLy,KeToan,SinhVien")]
    public class GiuongController : ControllerBase
    {
        private readonly AppDbContext _db;
        public GiuongController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? phongId, [FromQuery] TrangThaiGiuong? trangThai)
        {
            var query = _db.Giuongs.Include(g => g.Phong).AsQueryable();
            if (phongId.HasValue) query = query.Where(g => g.PhongId == phongId);
            if (trangThai.HasValue) query = query.Where(g => g.TrangThai == trangThai);

            var list = await query
                .Select(g => new GiuongResponseDto
                {
                    Id = g.Id,
                    SoGiuong = g.SoGiuong,
                    TrangThai = g.TrangThai.ToString(),
                    PhongId = g.PhongId,
                    SoPhong = g.Phong.SoPhong
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(list));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var g = await _db.Giuongs.Include(x => x.Phong).FirstOrDefaultAsync(x => x.Id == id);
            if (g == null) throw AppException.NotFound($"Không tìm thấy giường có id = {id}.");

            return Ok(ApiResponse<object>.Ok(new GiuongResponseDto
            {
                Id = g.Id,
                SoGiuong = g.SoGiuong,
                TrangThai = g.TrangThai.ToString(),
                PhongId = g.PhongId,
                SoPhong = g.Phong.SoPhong
            }));
        }

        [HttpPost]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> Create([FromBody] GiuongCreateDto dto)
        {
            var phong = await _db.Phongs.Include(p => p.Giuongs).FirstOrDefaultAsync(p => p.Id == dto.PhongId);
            if (phong == null) throw AppException.NotFound($"Không tìm thấy phòng có id = {dto.PhongId}.");

            if (phong.Giuongs.Count >= phong.SucChua)
                throw AppException.Conflict("Số giường đã đạt sức chứa tối đa của phòng.");

            var trungSo = phong.Giuongs.Any(g => g.SoGiuong == dto.SoGiuong);
            if (trungSo) throw AppException.Conflict($"Giường '{dto.SoGiuong}' đã tồn tại trong phòng này.");

            var giuong = new Giuong { PhongId = dto.PhongId, SoGiuong = dto.SoGiuong, TrangThai = TrangThaiGiuong.Trong };
            _db.Giuongs.Add(giuong);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = giuong.Id }, ApiResponse<Giuong>.Ok(giuong, "Đã tạo giường."));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> Delete(int id)
        {
            var giuong = await _db.Giuongs.FindAsync(id);
            if (giuong == null) throw AppException.NotFound($"Không tìm thấy giường có id = {id}.");

            if (giuong.TrangThai == TrangThaiGiuong.DaO)
                throw AppException.Conflict("Không thể xóa giường đang có sinh viên ở.");

            _db.Giuongs.Remove(giuong);
            await _db.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(null, "Đã xóa giường."));
        }
    }
}
