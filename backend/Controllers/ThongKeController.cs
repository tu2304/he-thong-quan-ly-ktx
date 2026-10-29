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
    [Route("api/thongke")]
    [Authorize(Roles = "QuanLy,KeToan")]
    public class ThongKeController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ThongKeController(AppDbContext db) => _db = db;

        // GET /api/thongke/tong-quan — dữ liệu cho dashboard (tiêu chí KT2 #5)
        [HttpGet("tong-quan")]
        public async Task<IActionResult> TongQuan()
        {
            var tongSoPhong = await _db.Phongs.CountAsync();
            var tongSoGiuong = await _db.Giuongs.CountAsync();
            var soGiuongDangO = await _db.Giuongs.CountAsync(g => g.TrangThai == TrangThaiGiuong.DaO);
            var soGiuongTrong = tongSoGiuong - soGiuongDangO;
            var tiLeLapDay = tongSoGiuong == 0 ? 0 : Math.Round(soGiuongDangO * 100.0 / tongSoGiuong, 1);

            // Lưu ý: SumAsync trên cột decimal không dịch được sang SQL với EF Core + SQLite
            // (provider lưu decimal dưới dạng TEXT nên không thể SUM() ở phía server).
            // Giải pháp: lấy danh sách SoTien về rồi cộng ở phía client (số dòng nhỏ, phù hợp demo).
            var tongPhiChuaThu = (await _db.Phis.Where(p => p.TrangThaiThanhToan == TrangThaiThanhToan.ChuaThu).Select(p => p.SoTien).ToListAsync()).Sum();
            var tongPhiQuaHan = (await _db.Phis.Where(p => p.TrangThaiThanhToan != TrangThaiThanhToan.DaThu && p.HanThanhToan < DateTime.UtcNow).Select(p => p.SoTien).ToListAsync()).Sum();
            var tongPhiDaThu = (await _db.Phis.Where(p => p.TrangThaiThanhToan == TrangThaiThanhToan.DaThu).Select(p => p.SoTien).ToListAsync()).Sum();

            var soPhanAnhMoi = await _db.PhanAnhs.CountAsync(p => p.TrangThai == TrangThaiPhanAnh.Moi);
            var soPhanAnhDangXuLy = await _db.PhanAnhs.CountAsync(p => p.TrangThai == TrangThaiPhanAnh.DangXuLy);
            var soPhanAnhDaXuLy = await _db.PhanAnhs.CountAsync(p => p.TrangThai == TrangThaiPhanAnh.DaXuLy);

            var tongSoSinhVien = await _db.SinhViens.CountAsync();
            var tongSoViPham = await _db.ViPhams.CountAsync();

            var dto = new ThongKeTongQuanDto
            {
                TongSoPhong = tongSoPhong,
                TongSoGiuong = tongSoGiuong,
                SoGiuongDangO = soGiuongDangO,
                SoGiuongTrong = soGiuongTrong,
                TiLeLapDay = tiLeLapDay,
                TongPhiChuaThu = tongPhiChuaThu,
                TongPhiQuaHan = tongPhiQuaHan,
                TongPhiDaThu = tongPhiDaThu,
                SoPhanAnhMoi = soPhanAnhMoi,
                SoPhanAnhDangXuLy = soPhanAnhDangXuLy,
                SoPhanAnhDaXuLy = soPhanAnhDaXuLy,
                TongSoSinhVien = tongSoSinhVien,
                TongSoViPham = tongSoViPham
            };

            return Ok(ApiResponse<ThongKeTongQuanDto>.Ok(dto));
        }

        // GET /api/thongke/phananh-theo-nhom — dữ liệu vẽ biểu đồ phân loại phản ánh
        [HttpGet("phananh-theo-nhom")]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> PhanAnhTheoNhom()
        {
            var data = await _db.PhanAnhs
                .GroupBy(p => p.NhomVanDe ?? "Chưa phân loại")
                .Select(g => new ThongKePhanAnhTheoNhomDto { NhomVanDe = g.Key, SoLuong = g.Count() })
                .OrderByDescending(x => x.SoLuong)
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(data));
        }
    }
}
