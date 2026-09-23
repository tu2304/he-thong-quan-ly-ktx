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
    [Route("api/phi")]
    [Authorize]
    public class PhiController : ControllerBase
    {
        private readonly AppDbContext _db;
        public PhiController(AppDbContext db) => _db = db;

        private static PhiResponseDto ToDto(Phi p) => new PhiResponseDto
        {
            Id = p.Id,
            HopDongId = p.HopDongId,
            MaSv = p.HopDong?.SinhVien?.MaSv,
            TenSinhVien = p.HopDong?.SinhVien?.NguoiDung?.HoTen,
            SoPhong = p.HopDong?.Giuong?.Phong?.SoPhong,
            LoaiPhi = p.LoaiPhi.ToString(),
            SoTien = p.SoTien,
            HanThanhToan = p.HanThanhToan,
            TrangThaiThanhToan = p.TrangThaiThanhToan.ToString()
        };

        // GET /api/phi?sinhVienId=&trangThai=&quaHan=true — tiêu chí #4: lọc công nợ
        [HttpGet]
        [Authorize(Roles = "QuanLy,KeToan")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? sinhVienId,
            [FromQuery] TrangThaiThanhToan? trangThai,
            [FromQuery] bool? quaHan,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _db.Phis
                .Include(p => p.HopDong).ThenInclude(h => h.SinhVien).ThenInclude(sv => sv.NguoiDung)
                .Include(p => p.HopDong).ThenInclude(h => h.Giuong).ThenInclude(g => g.Phong)
                .AsQueryable();

            if (sinhVienId.HasValue) query = query.Where(p => p.HopDong.SinhVienId == sinhVienId);
            if (trangThai.HasValue) query = query.Where(p => p.TrangThaiThanhToan == trangThai);
            if (quaHan == true) query = query.Where(p => p.TrangThaiThanhToan != TrangThaiThanhToan.DaThu && p.HanThanhToan < DateTime.UtcNow);

            query = query.OrderBy(p => p.HanThanhToan);

            var totalCount = await query.CountAsync();
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return Ok(ApiResponse<PagedResult<PhiResponseDto>>.Ok(new PagedResult<PhiResponseDto>
            {
                Items = items.Select(ToDto).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            }));
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "QuanLy,KeToan")]
        public async Task<IActionResult> GetById(int id)
        {
            var p = await _db.Phis
                .Include(x => x.HopDong).ThenInclude(h => h.SinhVien).ThenInclude(sv => sv.NguoiDung)
                .Include(x => x.HopDong).ThenInclude(h => h.Giuong).ThenInclude(g => g.Phong)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (p == null) throw AppException.NotFound($"Không tìm thấy khoản phí có id = {id}.");
            return Ok(ApiResponse<PhiResponseDto>.Ok(ToDto(p)));
        }

        [HttpGet("cua-toi")]
        [Authorize(Roles = "SinhVien")]
        public async Task<IActionResult> GetCuaToi(
            [FromQuery] TrangThaiThanhToan? trangThai,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var sinhVien = await _db.SinhViens
                .FirstOrDefaultAsync(sv => sv.NguoiDung.Email == email);
            if (sinhVien == null) throw AppException.Forbidden("Tài khoản chưa có hồ sơ sinh viên.");

            var query = _db.Phis
                .Include(p => p.HopDong).ThenInclude(h => h.SinhVien).ThenInclude(sv => sv.NguoiDung)
                .Include(p => p.HopDong).ThenInclude(h => h.Giuong).ThenInclude(g => g.Phong)
                .Where(p => p.HopDong.SinhVienId == sinhVien.Id)
                .AsQueryable();

            if (trangThai.HasValue) query = query.Where(p => p.TrangThaiThanhToan == trangThai);
            query = query.OrderBy(p => p.HanThanhToan);

            var totalCount = await query.CountAsync();
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return Ok(ApiResponse<PagedResult<PhiResponseDto>>.Ok(new PagedResult<PhiResponseDto>
            {
                Items = items.Select(ToDto).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            }));
        }

        [HttpPost]
        [Authorize(Roles = "QuanLy,KeToan")]
        public async Task<IActionResult> Create([FromBody] PhiCreateDto dto)
        {
            if (dto.SoTien <= 0) throw new AppException("Số tiền hóa đơn phải lớn hơn 0.", 400);
            if (dto.HanThanhToan.Date < DateTime.UtcNow.Date)
                throw new AppException("Hạn thanh toán không được ở trong quá khứ.", 400);

            var hopDong = await _db.HopDongs.AnyAsync(h => h.Id == dto.HopDongId);
            if (!hopDong) throw AppException.NotFound($"Không tìm thấy hợp đồng có id = {dto.HopDongId}.");

            var phi = new Phi
            {
                HopDongId = dto.HopDongId,
                LoaiPhi = dto.LoaiPhi,
                SoTien = dto.SoTien,
                HanThanhToan = dto.HanThanhToan,
                TrangThaiThanhToan = TrangThaiThanhToan.ChuaThu
            };
            _db.Phis.Add(phi);
            await _db.SaveChangesAsync();

            var created = await _db.Phis
                .Include(x => x.HopDong).ThenInclude(h => h.SinhVien).ThenInclude(sv => sv.NguoiDung)
                .Include(x => x.HopDong).ThenInclude(h => h.Giuong).ThenInclude(g => g.Phong)
                .FirstAsync(x => x.Id == phi.Id);

            return CreatedAtAction(nameof(GetById), new { id = phi.Id },
                ApiResponse<PhiResponseDto>.Ok(ToDto(created), "Đã tạo hóa đơn."));
        }

        // PUT /api/phi/{id}/thanh-toan — đánh dấu đã thu
        [HttpPut("{id}/thanh-toan")]
        [Authorize(Roles = "KeToan,QuanLy")]
        public async Task<IActionResult> DanhDauDaThu(int id)
        {
            var phi = await _db.Phis.FindAsync(id);
            if (phi == null) throw AppException.NotFound($"Không tìm thấy khoản phí có id = {id}.");

            phi.TrangThaiThanhToan = TrangThaiThanhToan.DaThu;
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<Phi>.Ok(phi, "Đã ghi nhận thanh toán."));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> Delete(int id)
        {
            var phi = await _db.Phis.FindAsync(id);
            if (phi == null) throw AppException.NotFound($"Không tìm thấy khoản phí có id = {id}.");

            _db.Phis.Remove(phi);
            await _db.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(null, "Đã xóa khoản phí."));
        }
    }
}
