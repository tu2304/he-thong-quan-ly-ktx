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
    [Route("api/phong")]
    [Authorize]
    public class PhongController : ControllerBase
    {
        private readonly AppDbContext _db;
        public PhongController(AppDbContext db) => _db = db;

        // GET /api/phong?keyword=A1&toaId=1&trangThai=ConTrong&sortBy=soPhong&sortDir=asc&page=1&pageSize=20
        // Tiêu chí KT2 #4: tìm kiếm, lọc, sắp xếp dữ liệu.
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string keyword,
            [FromQuery] int? toaId,
            [FromQuery] int? tangId,
            [FromQuery] TrangThaiPhong? trangThai,
            [FromQuery] string sortBy = "soPhong",
            [FromQuery] string sortDir = "asc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _db.Phongs
                .Include(p => p.Tang).ThenInclude(t => t.ToaNha)
                .Include(p => p.Giuongs)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(p => p.SoPhong.Contains(keyword));

            if (toaId.HasValue)
                query = query.Where(p => p.Tang.ToaId == toaId);

            if (tangId.HasValue)
                query = query.Where(p => p.TangId == tangId);

            if (trangThai.HasValue)
                query = query.Where(p => p.TrangThai == trangThai);

            query = (sortBy?.ToLower(), sortDir?.ToLower()) switch
            {
                ("succhua", "desc") => query.OrderByDescending(p => p.SucChua),
                ("succhua", _) => query.OrderBy(p => p.SucChua),
                ("trangthai", "desc") => query.OrderByDescending(p => p.TrangThai),
                ("trangthai", _) => query.OrderBy(p => p.TrangThai),
                (_, "desc") => query.OrderByDescending(p => p.SoPhong),
                _ => query.OrderBy(p => p.SoPhong)
            };

            var totalCount = await query.CountAsync();
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PhongResponseDto
                {
                    Id = p.Id,
                    SoPhong = p.SoPhong,
                    LoaiPhong = p.LoaiPhong,
                    SucChua = p.SucChua,
                    TrangThai = p.TrangThai.ToString(),
                    TenToa = p.Tang.ToaNha.TenToa,
                    SoTang = p.Tang.SoTang,
                    SoGiuongTrong = p.Giuongs.Count(g => g.TrangThai == TrangThaiGiuong.Trong),
                    TongSoGiuong = p.Giuongs.Count
                })
                .ToListAsync();

            var result = new PagedResult<PhongResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<PhongResponseDto>>.Ok(result));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var phong = await _db.Phongs
                .Include(p => p.Tang).ThenInclude(t => t.ToaNha)
                .Include(p => p.Giuongs)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (phong == null) throw AppException.NotFound($"Không tìm thấy phòng có id = {id}.");

            var dto = new PhongResponseDto
            {
                Id = phong.Id,
                SoPhong = phong.SoPhong,
                LoaiPhong = phong.LoaiPhong,
                SucChua = phong.SucChua,
                TrangThai = phong.TrangThai.ToString(),
                TenToa = phong.Tang.ToaNha.TenToa,
                SoTang = phong.Tang.SoTang,
                SoGiuongTrong = phong.Giuongs.Count(g => g.TrangThai == TrangThaiGiuong.Trong),
                TongSoGiuong = phong.Giuongs.Count
            };

            return Ok(ApiResponse<PhongResponseDto>.Ok(dto));
        }

        [HttpPost]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> Create([FromBody] PhongCreateDto dto)
        {
            var tangTonTai = await _db.Tangs.AnyAsync(t => t.Id == dto.TangId);
            if (!tangTonTai) throw AppException.NotFound($"Không tìm thấy tầng có id = {dto.TangId}.");

            var trungSo = await _db.Phongs.AnyAsync(p => p.TangId == dto.TangId && p.SoPhong == dto.SoPhong);
            if (trungSo) throw AppException.Conflict($"Phòng '{dto.SoPhong}' đã tồn tại trong tầng này.");

            var phong = new Phong
            {
                TangId = dto.TangId,
                SoPhong = dto.SoPhong,
                LoaiPhong = dto.LoaiPhong,
                SucChua = dto.SucChua,
                TrangThai = TrangThaiPhong.ConTrong
            };
            _db.Phongs.Add(phong);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = phong.Id }, ApiResponse<Phong>.Ok(phong, "Đã tạo phòng."));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> Update(int id, [FromBody] PhongUpdateDto dto)
        {
            var phong = await _db.Phongs.FindAsync(id);
            if (phong == null) throw AppException.NotFound($"Không tìm thấy phòng có id = {id}.");

            phong.SoPhong = dto.SoPhong;
            phong.LoaiPhong = dto.LoaiPhong;
            phong.SucChua = dto.SucChua;
            phong.TrangThai = dto.TrangThai;

            await _db.SaveChangesAsync();
            return Ok(ApiResponse<Phong>.Ok(phong, "Đã cập nhật phòng."));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> Delete(int id)
        {
            var phong = await _db.Phongs.Include(p => p.Giuongs).FirstOrDefaultAsync(p => p.Id == id);
            if (phong == null) throw AppException.NotFound($"Không tìm thấy phòng có id = {id}.");

            var coGiuongDaO = phong.Giuongs.Any(g => g.TrangThai == TrangThaiGiuong.DaO);
            if (coGiuongDaO)
                throw AppException.Conflict("Không thể xóa phòng đang có sinh viên ở.");

            _db.Phongs.Remove(phong);
            await _db.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(null, "Đã xóa phòng."));
        }

        // GET /api/phong/cho-trong — danh sách chỗ trống, phục vụ UC-01/UC-08 (tra cứu & gợi ý xếp phòng)
        [HttpGet("cho-trong")]
        public async Task<IActionResult> ChoTrong([FromQuery] int? toaId)
        {
            var query = _db.Giuongs
                .Include(g => g.Phong).ThenInclude(p => p.Tang).ThenInclude(t => t.ToaNha)
                .Where(g => g.TrangThai == TrangThaiGiuong.Trong);

            if (toaId.HasValue)
                query = query.Where(g => g.Phong.Tang.ToaId == toaId);

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
    }
}
