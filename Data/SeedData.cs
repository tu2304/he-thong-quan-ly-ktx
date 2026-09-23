using Microsoft.AspNetCore.Identity;
using KtxAI.Api.Models;

namespace KtxAI.Api.Data
{
    // Khởi tạo dữ liệu mẫu để demo (tiêu chí KT2 #7: "có dữ liệu mẫu để demo")
    public static class SeedData
    {
        public static void Initialize(AppDbContext db)
        {
            db.Database.EnsureCreated();

            if (db.NguoiDungs.Any())
            {
                return; // Đã có dữ liệu, không seed lại
            }

            var hasher = new PasswordHasher<NguoiDung>();

            // ---- Tài khoản mẫu ----
            var quanLy = new NguoiDung { HoTen = "Nguyễn Văn Quản", Email = "quanly@ktx.edu.vn", VaiTro = VaiTro.QuanLy };
            quanLy.MatKhauHash = hasher.HashPassword(quanLy, "QuanLy@123");

            var keToan = new NguoiDung { HoTen = "Trần Thị Kế", Email = "ketoan@ktx.edu.vn", VaiTro = VaiTro.KeToan };
            keToan.MatKhauHash = hasher.HashPassword(keToan, "KeToan@123");

            var taiKhoanSv1 = new NguoiDung { HoTen = "Lê Văn An", Email = "an.le@sv.edu.vn", VaiTro = VaiTro.SinhVien };
            taiKhoanSv1.MatKhauHash = hasher.HashPassword(taiKhoanSv1, "SinhVien@123");

            var taiKhoanSv2 = new NguoiDung { HoTen = "Phạm Thị Bình", Email = "binh.pham@sv.edu.vn", VaiTro = VaiTro.SinhVien };
            taiKhoanSv2.MatKhauHash = hasher.HashPassword(taiKhoanSv2, "SinhVien@123");

            var taiKhoanSv3 = new NguoiDung { HoTen = "Hoàng Văn Cường", Email = "cuong.hoang@sv.edu.vn", VaiTro = VaiTro.SinhVien };
            taiKhoanSv3.MatKhauHash = hasher.HashPassword(taiKhoanSv3, "SinhVien@123");

            db.NguoiDungs.AddRange(quanLy, keToan, taiKhoanSv1, taiKhoanSv2, taiKhoanSv3);
            db.SaveChanges();

            // ---- Cấu trúc tòa - tầng - phòng - giường ----
            var toaA = new ToaNha { TenToa = "Tòa A" };
            var toaB = new ToaNha { TenToa = "Tòa B" };
            db.ToaNhas.AddRange(toaA, toaB);
            db.SaveChanges();

            var tangA1 = new Tang { ToaId = toaA.Id, SoTang = 1 };
            var tangA2 = new Tang { ToaId = toaA.Id, SoTang = 2 };
            var tangB1 = new Tang { ToaId = toaB.Id, SoTang = 1 };
            db.Tangs.AddRange(tangA1, tangA2, tangB1);
            db.SaveChanges();

            var phongA101 = new Phong { TangId = tangA1.Id, SoPhong = "A101", LoaiPhong = "4 người", SucChua = 4, TrangThai = TrangThaiPhong.ConTrong };
            var phongA102 = new Phong { TangId = tangA1.Id, SoPhong = "A102", LoaiPhong = "4 người", SucChua = 4, TrangThai = TrangThaiPhong.ConTrong };
            var phongA201 = new Phong { TangId = tangA2.Id, SoPhong = "A201", LoaiPhong = "8 người", SucChua = 8, TrangThai = TrangThaiPhong.ConTrong };
            var phongB101 = new Phong { TangId = tangB1.Id, SoPhong = "B101", LoaiPhong = "4 người", SucChua = 4, TrangThai = TrangThaiPhong.ConTrong };
            db.Phongs.AddRange(phongA101, phongA102, phongA201, phongB101);
            db.SaveChanges();

            var giuongs = new List<Giuong>();
            foreach (var (phong, soLuong) in new[] { (phongA101, 4), (phongA102, 4), (phongA201, 8), (phongB101, 4) })
            {
                for (int i = 1; i <= soLuong; i++)
                {
                    giuongs.Add(new Giuong { PhongId = phong.Id, SoGiuong = $"G{i}", TrangThai = TrangThaiGiuong.Trong });
                }
            }
            db.Giuongs.AddRange(giuongs);
            db.SaveChanges();

            // ---- Hồ sơ sinh viên ----
            var sv1 = new SinhVien { NguoiDungId = taiKhoanSv1.Id, MaSv = "SV001", GioiTinh = GioiTinh.Nam, NgaySinh = new DateTime(2004, 3, 12), Lop = "CNTT1", Sdt = "0901111111" };
            var sv2 = new SinhVien { NguoiDungId = taiKhoanSv2.Id, MaSv = "SV002", GioiTinh = GioiTinh.Nu, NgaySinh = new DateTime(2004, 7, 20), Lop = "CNTT2", Sdt = "0902222222" };
            var sv3 = new SinhVien { NguoiDungId = taiKhoanSv3.Id, MaSv = "SV003", GioiTinh = GioiTinh.Nam, NgaySinh = new DateTime(2003, 11, 5), Lop = "KTPM1", Sdt = "0903333333" };
            db.SinhViens.AddRange(sv1, sv2, sv3);
            db.SaveChanges();

            // ---- Xếp phòng mẫu: sv1, sv2 đã có hợp đồng; sv3 chưa xếp phòng ----
            var giuongA101_G1 = giuongs.First(g => g.PhongId == phongA101.Id && g.SoGiuong == "G1");
            var giuongA101_G2 = giuongs.First(g => g.PhongId == phongA101.Id && g.SoGiuong == "G2");

            giuongA101_G1.TrangThai = TrangThaiGiuong.DaO;
            giuongA101_G2.TrangThai = TrangThaiGiuong.DaO;
            phongA101.TrangThai = TrangThaiPhong.ConTrong; // vẫn còn 2 giường trống trong phòng 4 người

            var hopDong1 = new HopDong { SinhVienId = sv1.Id, GiuongId = giuongA101_G1.Id, NgayBatDau = DateTime.UtcNow.AddMonths(-2), TrangThai = TrangThaiHopDong.DangHieuLuc };
            var hopDong2 = new HopDong { SinhVienId = sv2.Id, GiuongId = giuongA101_G2.Id, NgayBatDau = DateTime.UtcNow.AddMonths(-1), TrangThai = TrangThaiHopDong.DangHieuLuc };
            db.HopDongs.AddRange(hopDong1, hopDong2);
            db.SaveChanges();

            // ---- Phí mẫu ----
            db.Phis.AddRange(
                new Phi { HopDongId = hopDong1.Id, LoaiPhi = LoaiPhi.PhiPhong, SoTien = 1200000, HanThanhToan = DateTime.UtcNow.AddDays(10), TrangThaiThanhToan = TrangThaiThanhToan.ChuaThu },
                new Phi { HopDongId = hopDong1.Id, LoaiPhi = LoaiPhi.PhiDichVu, SoTien = 200000, HanThanhToan = DateTime.UtcNow.AddDays(-5), TrangThaiThanhToan = TrangThaiThanhToan.QuaHan },
                new Phi { HopDongId = hopDong2.Id, LoaiPhi = LoaiPhi.PhiPhong, SoTien = 1200000, HanThanhToan = DateTime.UtcNow.AddDays(15), TrangThaiThanhToan = TrangThaiThanhToan.DaThu }
            );

            // ---- Nội quy mẫu ----
            var nq1 = new NoiQuy { TieuDe = "Điều 5 - Giờ giới nghiêm", NoiDung = "Sinh viên nội trú phải có mặt tại phòng ở trước 23h00 hằng ngày, trừ trường hợp có đơn xin phép được quản lý KTX phê duyệt.", PhienBan = "1.0" };
            var nq2 = new NoiQuy { TieuDe = "Điều 8 - Vệ sinh phòng ở", NoiDung = "Sinh viên có trách nhiệm giữ gìn vệ sinh chung trong phòng và khu vực sinh hoạt chung; không xả rác bừa bãi.", PhienBan = "1.0" };
            var nq3 = new NoiQuy { TieuDe = "Điều 12 - Khách ra vào", NoiDung = "Khách đến thăm phải đăng ký tại bàn bảo vệ và rời khỏi ký túc xá trước 21h00.", PhienBan = "1.0" };
            var nq4 = new NoiQuy { TieuDe = "Điều 15 - Cấm hút thuốc và chất cấm", NoiDung = "Nghiêm cấm hút thuốc, sử dụng rượu bia, chất kích thích và mang chất cấm vào khu vực ký túc xá dưới mọi hình thức.", PhienBan = "1.0" };
            var nq5 = new NoiQuy { TieuDe = "Điều 18 - An toàn điện, phòng cháy chữa cháy", NoiDung = "Không tự ý câu mắc, sửa chữa thiết bị điện; không sử dụng bếp gas, bếp điện công suất lớn trong phòng ở để đảm bảo an toàn phòng cháy chữa cháy.", PhienBan = "1.0" };
            var nq6 = new NoiQuy { TieuDe = "Điều 20 - Xử lý vi phạm", NoiDung = "Sinh viên vi phạm nội quy lần đầu sẽ bị nhắc nhở và lập biên bản; vi phạm nhiều lần hoặc vi phạm nghiêm trọng có thể bị đình chỉ chỗ ở tùy theo mức độ, do Ban quản lý KTX quyết định.", PhienBan = "1.0" };
            db.NoiQuys.AddRange(nq1, nq2, nq3, nq4, nq5, nq6);
            db.SaveChanges();

            // ---- Phản ánh mẫu ----
            db.PhanAnhs.AddRange(
                new PhanAnh { SinhVienId = sv1.Id, PhongId = phongA101.Id, NoiDung = "Vòi nước trong nhà vệ sinh chung bị rò rỉ, đề nghị sửa chữa.", NhomVanDe = "Cơ sở vật chất", TrangThai = TrangThaiPhanAnh.Moi },
                new PhanAnh { SinhVienId = sv2.Id, PhongId = phongA101.Id, NoiDung = "Khu vực hành lang tầng 1 chưa được dọn dẹp trong 2 ngày qua.", NhomVanDe = "Vệ sinh", TrangThai = TrangThaiPhanAnh.DangXuLy }
            );

            // ---- Vi phạm mẫu ----
            db.ViPhams.Add(new ViPham { SinhVienId = sv3.Id, DieuKhoanId = nq1.Id, MoTa = "Về phòng lúc 23h45, quá giờ giới nghiêm quy định.", MucDo = MucDoViPham.Nhe });

            db.SaveChanges();
        }
    }
}
