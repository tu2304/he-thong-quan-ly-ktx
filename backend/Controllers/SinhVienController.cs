using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KtxAI.Api.Common;
using KtxAI.Api.Data;
using KtxAI.Api.Dtos;
using KtxAI.Api.Models;

namespace KtxAI.Api.Controllers
{
    [ApiController]
    [Route("api/sinhvien")]
    [Authorize(Roles = "QuanLy,KeToan,SinhVien")]
    public class SinhVienController : ControllerBase
    {
        private readonly AppDbContext _db;
        public SinhVienController(AppDbContext db) => _db = db;

        private static SinhVienResponseDto ToDto(SinhVien sv, string phongHienTai) => new SinhVienResponseDto
        {
            Id = sv.Id,
            MaSv = sv.MaSv,
            HoTen = sv.NguoiDung.HoTen,
            Email = sv.NguoiDung.Email,
            GioiTinh = sv.GioiTinh.ToString(),
            NgaySinh = sv.NgaySinh,
            Lop = sv.Lop,
            Sdt = sv.Sdt,
            PhongHienTai = phongHienTai
        };

        // GET /api/sinhvien?keyword=&lop=&sortBy=hoten&sortDir=asc&page=1&pageSize=20
        // Tiêu chí KT2 #4: tìm kiếm/lọc/sắp xếp.
        [HttpGet]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> GetAll(
            [FromQuery] string keyword,
            [FromQuery] string lop,
            [FromQuery] string sortBy = "hoten",
            [FromQuery] string sortDir = "asc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _db.SinhViens.Include(sv => sv.NguoiDung).AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(sv => sv.MaSv.Contains(keyword) || sv.NguoiDung.HoTen.Contains(keyword));

            if (!string.IsNullOrWhiteSpace(lop))
                query = query.Where(sv => sv.Lop == lop);

            query = (sortBy?.ToLower(), sortDir?.ToLower()) switch
            {
                ("masv", "desc") => query.OrderByDescending(sv => sv.MaSv),
                ("masv", _) => query.OrderBy(sv => sv.MaSv),
                ("hoten", "desc") => query.OrderByDescending(sv => sv.NguoiDung.HoTen),
                _ => query.OrderBy(sv => sv.NguoiDung.HoTen)
            };

            var totalCount = await query.CountAsync();
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var svList = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            // Lấy phòng hiện tại (nếu có hợp đồng đang hiệu lực) cho từng sinh viên
            var svIds = svList.Select(sv => sv.Id).ToList();
            var phongHienTaiMap = await _db.HopDongs
                .Where(h => svIds.Contains(h.SinhVienId) && h.TrangThai == TrangThaiHopDong.DangHieuLuc)
                .Include(h => h.Giuong).ThenInclude(g => g.Phong)
                .ToDictionaryAsync(h => h.SinhVienId, h => h.Giuong.Phong.SoPhong);

            var items = svList.Select(sv => ToDto(sv, phongHienTaiMap.GetValueOrDefault(sv.Id))).ToList();

            var result = new PagedResult<SinhVienResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<SinhVienResponseDto>>.Ok(result));
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> GetById(int id)
        {
            var sv = await _db.SinhViens.Include(x => x.NguoiDung).FirstOrDefaultAsync(x => x.Id == id);
            if (sv == null) throw AppException.NotFound($"Không tìm thấy sinh viên có id = {id}.");

            var hopDong = await _db.HopDongs
                .Include(h => h.Giuong).ThenInclude(g => g.Phong)
                .FirstOrDefaultAsync(h => h.SinhVienId == id && h.TrangThai == TrangThaiHopDong.DangHieuLuc);

            return Ok(ApiResponse<SinhVienResponseDto>.Ok(ToDto(sv, hopDong?.Giuong?.Phong?.SoPhong)));
        }

        // GET /api/sinhvien/me — sinh viên tự lấy hồ sơ của chính mình (dùng để gửi phản ánh, xem hợp đồng...)
        // Đặt route literal "me" — ASP.NET Core luôn ưu tiên khớp đoạn literal trước đoạn tham số "{id}",
        // nên endpoint này không bị nhầm với GetById(int id) dù khai báo sau.
        [HttpGet("me")]
        [Authorize(Roles = "SinhVien")]
        public async Task<IActionResult> GetMe()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var sv = await _db.SinhViens.Include(x => x.NguoiDung).FirstOrDefaultAsync(x => x.NguoiDungId.ToString() == userId);
            if (sv == null) throw AppException.NotFound("Tài khoản hiện tại không có hồ sơ sinh viên tương ứng.");

            var hopDong = await _db.HopDongs
                .Include(h => h.Giuong).ThenInclude(g => g.Phong)
                .FirstOrDefaultAsync(h => h.SinhVienId == sv.Id && h.TrangThai == TrangThaiHopDong.DangHieuLuc);

            return Ok(ApiResponse<SinhVienResponseDto>.Ok(ToDto(sv, hopDong?.Giuong?.Phong?.SoPhong)));
        }

        // Tạo sinh viên = tạo tài khoản NguoiDung (vai trò SinhVien) + hồ sơ SinhVien
        [HttpPost]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> Create([FromBody] SinhVienCreateDto dto)
        {
            var emailTrung = await _db.NguoiDungs.AnyAsync(x => x.Email == dto.Email);
            if (emailTrung) throw AppException.Conflict($"Email '{dto.Email}' đã được sử dụng.");

            var maSvTrung = await _db.SinhViens.AnyAsync(x => x.MaSv == dto.MaSv);
            if (maSvTrung) throw AppException.Conflict($"Mã số sinh viên '{dto.MaSv}' đã tồn tại.");

            using var transaction = await _db.Database.BeginTransactionAsync();

            var nguoiDung = new NguoiDung { HoTen = dto.HoTen, Email = dto.Email, VaiTro = VaiTro.SinhVien };
            var hasher = new PasswordHasher<NguoiDung>();
            nguoiDung.MatKhauHash = hasher.HashPassword(nguoiDung, dto.MatKhau);

            _db.NguoiDungs.Add(nguoiDung);
            await _db.SaveChangesAsync();

            var sinhVien = new SinhVien
            {
                NguoiDungId = nguoiDung.Id,
                MaSv = dto.MaSv,
                GioiTinh = dto.GioiTinh,
                NgaySinh = dto.NgaySinh,
                Lop = dto.Lop,
                Sdt = dto.Sdt
            };
            _db.SinhViens.Add(sinhVien);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();

            sinhVien.NguoiDung = nguoiDung;
            return CreatedAtAction(nameof(GetById), new { id = sinhVien.Id }, ApiResponse<SinhVienResponseDto>.Ok(ToDto(sinhVien, null), "Đã tạo hồ sơ sinh viên."));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> Update(int id, [FromBody] SinhVienUpdateDto dto)
        {
            var sv = await _db.SinhViens.Include(x => x.NguoiDung).FirstOrDefaultAsync(x => x.Id == id);
            if (sv == null) throw AppException.NotFound($"Không tìm thấy sinh viên có id = {id}.");

            sv.NguoiDung.HoTen = dto.HoTen;
            sv.Lop = dto.Lop;
            sv.Sdt = dto.Sdt;
            sv.GioiTinh = dto.GioiTinh;

            await _db.SaveChangesAsync();
            return Ok(ApiResponse<SinhVienResponseDto>.Ok(ToDto(sv, null), "Đã cập nhật hồ sơ sinh viên."));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> Delete(int id)
        {
            var sv = await _db.SinhViens.Include(x => x.NguoiDung).FirstOrDefaultAsync(x => x.Id == id);
            if (sv == null) throw AppException.NotFound($"Không tìm thấy sinh viên có id = {id}.");

            var conHopDong = await _db.HopDongs.AnyAsync(h => h.SinhVienId == id && h.TrangThai == TrangThaiHopDong.DangHieuLuc);
            if (conHopDong) throw AppException.Conflict("Không thể xóa sinh viên đang có hợp đồng lưu trú hiệu lực. Vui lòng trả phòng trước.");

            _db.SinhViens.Remove(sv); // NguoiDung sẽ bị xóa theo (cascade) qua khóa ngoại NguoiDungId
            _db.NguoiDungs.Remove(sv.NguoiDung);
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(null, "Đã xóa sinh viên."));
        }
    }
}
