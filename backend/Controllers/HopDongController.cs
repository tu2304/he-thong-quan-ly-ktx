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
    [Route("api/hopdong")]
    [Authorize(Roles = "QuanLy,KeToan,SinhVien")]
    public class HopDongController : ControllerBase
    {
        private readonly AppDbContext _db;
        public HopDongController(AppDbContext db) => _db = db;

        private static HopDongResponseDto ToDto(HopDong h) => new HopDongResponseDto
        {
            Id = h.Id,
            SinhVienId = h.SinhVienId,
            TenSinhVien = h.SinhVien?.NguoiDung?.HoTen,
            MaSv = h.SinhVien?.MaSv,
            SoPhong = h.Giuong?.Phong?.SoPhong,
            SoGiuong = h.Giuong?.SoGiuong,
            NgayBatDau = h.NgayBatDau,
            NgayKetThuc = h.NgayKetThuc,
            TrangThai = h.TrangThai.ToString()
        };

        // GET /api/hopdong?sinhVienId=&trangThai=&page=1&pageSize=20  (UC-05: lịch sử ở)
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? sinhVienId,
            [FromQuery] int? phongId,
            [FromQuery] TrangThaiHopDong? trangThai,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _db.HopDongs
                .Include(h => h.SinhVien).ThenInclude(sv => sv.NguoiDung)
                .Include(h => h.Giuong).ThenInclude(g => g.Phong)
                .AsQueryable();

            if (User.IsInRole("SinhVien"))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                query = query.Where(h => h.SinhVien.NguoiDungId.ToString() == userId);
            }

            if (sinhVienId.HasValue) query = query.Where(h => h.SinhVienId == sinhVienId);
            if (phongId.HasValue) query = query.Where(h => h.Giuong.PhongId == phongId);
            if (trangThai.HasValue) query = query.Where(h => h.TrangThai == trangThai);

            query = query.OrderByDescending(h => h.NgayBatDau);

            var totalCount = await query.CountAsync();
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(h => h).ToListAsync();
            var dtoList = items.Select(ToDto).ToList();

            return Ok(ApiResponse<PagedResult<HopDongResponseDto>>.Ok(new PagedResult<HopDongResponseDto>
            {
                Items = dtoList,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            }));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var h = await _db.HopDongs
                .Include(x => x.SinhVien).ThenInclude(sv => sv.NguoiDung)
                .Include(x => x.Giuong).ThenInclude(g => g.Phong)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (h == null) throw AppException.NotFound($"Không tìm thấy hợp đồng có id = {id}.");
            if (User.IsInRole("SinhVien") && h.SinhVien.NguoiDungId.ToString() !=
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value)
                throw AppException.NotFound("Không tìm thấy hợp đồng.");
            return Ok(ApiResponse<HopDongResponseDto>.Ok(ToDto(h)));
        }

        // POST /api/hopdong/xep-phong — UC-01: Đăng ký ở & xếp phòng
        [HttpPost("xep-phong")]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> XepPhong([FromBody] XepPhongDto dto)
        {
            var sinhVien = await _db.SinhViens.FindAsync(dto.SinhVienId);
            if (sinhVien == null) throw AppException.NotFound($"Không tìm thấy sinh viên có id = {dto.SinhVienId}.");

            var daCoPhong = await _db.HopDongs.AnyAsync(h => h.SinhVienId == dto.SinhVienId && h.TrangThai == TrangThaiHopDong.DangHieuLuc);
            if (daCoPhong) throw AppException.Conflict("Sinh viên này đang có hợp đồng lưu trú hiệu lực, không thể xếp phòng mới.");

            var giuong = await _db.Giuongs.Include(g => g.Phong).FirstOrDefaultAsync(g => g.Id == dto.GiuongId);
            if (giuong == null) throw AppException.NotFound($"Không tìm thấy giường có id = {dto.GiuongId}.");

            // Kiểm tra lại trạng thái ngay trước khi xác nhận, tránh xếp trùng giường (rủi ro đã nêu tại KT1 mục 10.3)
            if (giuong.TrangThai == TrangThaiGiuong.DaO)
                throw AppException.Conflict($"Giường '{giuong.SoGiuong}' đã có người ở, vui lòng chọn giường khác.");

            var hopDong = new HopDong
            {
                SinhVienId = dto.SinhVienId,
                GiuongId = dto.GiuongId,
                NgayBatDau = DateTime.UtcNow,
                TrangThai = TrangThaiHopDong.DangHieuLuc
            };
            _db.HopDongs.Add(hopDong);

            giuong.TrangThai = TrangThaiGiuong.DaO;
            var conTrong = await _db.Giuongs.AnyAsync(g => g.PhongId == giuong.PhongId && g.Id != giuong.Id && g.TrangThai == TrangThaiGiuong.Trong);
            giuong.Phong.TrangThai = conTrong ? TrangThaiPhong.ConTrong : TrangThaiPhong.Day;

            await _db.SaveChangesAsync();

            hopDong.SinhVien = sinhVien;
            hopDong.Giuong = giuong;
            return CreatedAtAction(nameof(GetById), new { id = hopDong.Id }, ApiResponse<HopDongResponseDto>.Ok(ToDto(hopDong), "Đã xếp phòng thành công."));
        }

        // POST /api/hopdong/{id}/chuyen-phong — UC-02
        [HttpPost("{id}/chuyen-phong")]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> ChuyenPhong(int id, [FromBody] ChuyenPhongDto dto)
        {
            var hopDong = await _db.HopDongs.Include(h => h.Giuong).ThenInclude(g => g.Phong).FirstOrDefaultAsync(h => h.Id == id);
            if (hopDong == null) throw AppException.NotFound($"Không tìm thấy hợp đồng có id = {id}.");
            if (hopDong.TrangThai != TrangThaiHopDong.DangHieuLuc)
                throw AppException.Conflict("Hợp đồng này không còn hiệu lực, không thể chuyển phòng.");

            var giuongMoi = await _db.Giuongs.Include(g => g.Phong).FirstOrDefaultAsync(g => g.Id == dto.GiuongMoiId);
            if (giuongMoi == null) throw AppException.NotFound($"Không tìm thấy giường có id = {dto.GiuongMoiId}.");
            if (giuongMoi.TrangThai == TrangThaiGiuong.DaO)
                throw AppException.Conflict($"Giường '{giuongMoi.SoGiuong}' đã có người ở.");

            var giuongCu = hopDong.Giuong;
            giuongCu.TrangThai = TrangThaiGiuong.Trong;
            giuongCu.Phong.TrangThai = TrangThaiPhong.ConTrong;

            giuongMoi.TrangThai = TrangThaiGiuong.DaO;
            var conTrongPhongMoi = await _db.Giuongs.AnyAsync(g => g.PhongId == giuongMoi.PhongId && g.Id != giuongMoi.Id && g.TrangThai == TrangThaiGiuong.Trong);
            giuongMoi.Phong.TrangThai = conTrongPhongMoi ? TrangThaiPhong.ConTrong : TrangThaiPhong.Day;

            hopDong.GiuongId = giuongMoi.Id;

            await _db.SaveChangesAsync();
            hopDong.Giuong = giuongMoi;
            return Ok(ApiResponse<HopDongResponseDto>.Ok(ToDto(hopDong), "Đã chuyển phòng thành công."));
        }

        // POST /api/hopdong/{id}/tra-phong — UC-02
        [HttpPost("{id}/tra-phong")]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> TraPhong(int id)
        {
            var hopDong = await _db.HopDongs
                .Include(h => h.Giuong).ThenInclude(g => g.Phong)
                .Include(h => h.Phis)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hopDong == null) throw AppException.NotFound($"Không tìm thấy hợp đồng có id = {id}.");
            if (hopDong.TrangThai != TrangThaiHopDong.DangHieuLuc)
                throw AppException.Conflict("Hợp đồng này đã kết thúc trước đó.");

            var conNoChuaThu = hopDong.Phis.Any(p => p.TrangThaiThanhToan != TrangThaiThanhToan.DaThu);

            hopDong.TrangThai = TrangThaiHopDong.DaKetThuc;
            hopDong.NgayKetThuc = DateTime.UtcNow;

            hopDong.Giuong.TrangThai = TrangThaiGiuong.Trong;
            hopDong.Giuong.Phong.TrangThai = TrangThaiPhong.ConTrong;

            await _db.SaveChangesAsync();

            var canhBao = conNoChuaThu ? " Lưu ý: sinh viên vẫn còn khoản phí chưa thu, cần tiếp tục theo dõi công nợ." : "";
            return Ok(ApiResponse<HopDongResponseDto>.Ok(ToDto(hopDong), $"Đã trả phòng thành công.{canhBao}"));
        }
    }
}
