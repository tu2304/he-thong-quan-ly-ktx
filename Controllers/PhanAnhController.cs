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
    [Route("api/phananh")]
    [Authorize]
    public class PhanAnhController : ControllerBase
    {
        private readonly AppDbContext _db;
        public PhanAnhController(AppDbContext db) => _db = db;

        private static PhanAnhResponseDto ToDto(PhanAnh pa) => new PhanAnhResponseDto
        {
            Id = pa.Id,
            SinhVienId = pa.SinhVienId,
            TenSinhVien = pa.SinhVien?.NguoiDung?.HoTen,
            PhongId = pa.PhongId,
            SoPhong = pa.Phong?.SoPhong,
            NoiDung = pa.NoiDung,
            NhomVanDe = pa.NhomVanDe,
            TrangThai = pa.TrangThai.ToString(),
            NgayTao = pa.NgayTao
        };

        // GET /api/phananh?keyword=&nhomVanDe=&trangThai=&sinhVienId=  — tiêu chí #4
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string keyword,
            [FromQuery] string nhomVanDe,
            [FromQuery] TrangThaiPhanAnh? trangThai,
            [FromQuery] int? sinhVienId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _db.PhanAnhs
                .Include(pa => pa.SinhVien).ThenInclude(sv => sv.NguoiDung)
                .Include(pa => pa.Phong)
                .AsQueryable();

            // Sinh viên chỉ được xem phản ánh của chính mình; Quản lý xem tất cả.
            var vaiTro = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (vaiTro == VaiTro.SinhVien.ToString())
            {
                var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                var svHienTai = await _db.SinhViens.Include(sv => sv.NguoiDung).FirstOrDefaultAsync(sv => sv.NguoiDung.Email == email);
                if (svHienTai == null) throw AppException.Forbidden("Không xác định được hồ sơ sinh viên của tài khoản này.");
                query = query.Where(pa => pa.SinhVienId == svHienTai.Id);
            }
            else if (sinhVienId.HasValue)
            {
                query = query.Where(pa => pa.SinhVienId == sinhVienId);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(pa => pa.NoiDung.Contains(keyword));

            if (!string.IsNullOrWhiteSpace(nhomVanDe))
                query = query.Where(pa => pa.NhomVanDe == nhomVanDe);

            if (trangThai.HasValue)
                query = query.Where(pa => pa.TrangThai == trangThai);

            query = query.OrderByDescending(pa => pa.NgayTao);

            var totalCount = await query.CountAsync();
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return Ok(ApiResponse<PagedResult<PhanAnhResponseDto>>.Ok(new PagedResult<PhanAnhResponseDto>
            {
                Items = items.Select(ToDto).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            }));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var pa = await _db.PhanAnhs
                .Include(x => x.SinhVien).ThenInclude(sv => sv.NguoiDung)
                .Include(x => x.Phong)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (pa == null) throw AppException.NotFound($"Không tìm thấy phản ánh có id = {id}.");
            return Ok(ApiResponse<PhanAnhResponseDto>.Ok(ToDto(pa)));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PhanAnhCreateDto dto)
        {
            var vaiTro = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            SinhVien sv;

            if (vaiTro == VaiTro.SinhVien.ToString())
            {
                var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                sv = await _db.SinhViens
                    .Include(x => x.NguoiDung)
                    .Include(x => x.HopDongs)
                        .ThenInclude(x => x.Giuong)
                    .FirstOrDefaultAsync(x => x.NguoiDung.Email == email);

                if (sv == null)
                    throw AppException.Forbidden("Tài khoản chưa có hồ sơ sinh viên tương ứng.");
            }
            else
            {
                sv = await _db.SinhViens.FindAsync(dto.SinhVienId);
            }

            if (sv == null) throw AppException.NotFound($"Không tìm thấy sinh viên có id = {dto.SinhVienId}.");

            if (string.IsNullOrWhiteSpace(dto.NoiDung))
                throw new Common.AppException("Nội dung phản ánh không được để trống.", 400);

            var phongId = dto.PhongId;
            if (vaiTro == VaiTro.SinhVien.ToString())
            {
                var hopDongHienTai = sv.HopDongs
                    .FirstOrDefault(h => h.TrangThai == TrangThaiHopDong.DangHieuLuc);
                phongId = hopDongHienTai == null
                    ? null
                    : await _db.Giuongs
                        .Where(g => g.Id == hopDongHienTai.GiuongId)
                        .Select(g => (int?)g.PhongId)
                        .FirstOrDefaultAsync();
            }

            var phanAnh = new PhanAnh
            {
                SinhVienId = sv.Id,
                PhongId = phongId,
                NoiDung = dto.NoiDung.Trim(),
                TrangThai = TrangThaiPhanAnh.Moi,
                NgayTao = DateTime.UtcNow
            };
            _db.PhanAnhs.Add(phanAnh);
            await _db.SaveChangesAsync();

            var created = await _db.PhanAnhs
                .Include(x => x.SinhVien).ThenInclude(x => x.NguoiDung)
                .Include(x => x.Phong)
                .FirstAsync(x => x.Id == phanAnh.Id);

            return CreatedAtAction(nameof(GetById), new { id = phanAnh.Id },
                ApiResponse<PhanAnhResponseDto>.Ok(ToDto(created), "Đã gửi phản ánh."));
        }

        // PUT /api/phananh/{id}/trang-thai — Quản lý xử lý & phân nhóm phản ánh
        [HttpPut("{id}/trang-thai")]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> UpdateTrangThai(int id, [FromBody] PhanAnhUpdateTrangThaiDto dto)
        {
            var pa = await _db.PhanAnhs.FindAsync(id);
            if (pa == null) throw AppException.NotFound($"Không tìm thấy phản ánh có id = {id}.");

            pa.TrangThai = dto.TrangThai;
            if (!string.IsNullOrWhiteSpace(dto.NhomVanDe)) pa.NhomVanDe = dto.NhomVanDe;

            await _db.SaveChangesAsync();
            return Ok(ApiResponse<PhanAnh>.Ok(pa, "Đã cập nhật trạng thái phản ánh."));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> Delete(int id)
        {
            var pa = await _db.PhanAnhs.FindAsync(id);
            if (pa == null) throw AppException.NotFound($"Không tìm thấy phản ánh có id = {id}.");

            _db.PhanAnhs.Remove(pa);
            await _db.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(null, "Đã xóa phản ánh."));
        }
    }
}
