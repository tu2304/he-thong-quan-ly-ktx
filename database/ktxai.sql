PRAGMA foreign_keys=OFF;
BEGIN TRANSACTION;
CREATE TABLE IF NOT EXISTS "NguoiDungs" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_NguoiDungs" PRIMARY KEY AUTOINCREMENT,
    "HoTen" TEXT NOT NULL,
    "Email" TEXT NOT NULL,
    "MatKhauHash" TEXT NOT NULL,
    "VaiTro" TEXT NOT NULL,
    "DangHoatDong" INTEGER NOT NULL,
    "NgayTao" TEXT NOT NULL
);
INSERT INTO NguoiDungs VALUES(1,'Nguyễn Văn Quản','quanly@ktx.edu.vn','AQAAAAIAAYagAAAAEF3DwWvwnPJesjT4Oho+PDs295U64Pu+1GZ796+AuyfzL3NMCtyyiMhVvr0yxzNhqw==','QuanLy',1,'2026-09-22 15:34:05.7552992');
INSERT INTO NguoiDungs VALUES(2,'Trần Thị Kế','ketoan@ktx.edu.vn','AQAAAAIAAYagAAAAELtZWcyNOdrpOvU8axQUfZcVaT8A5MLeSx/AvQzms39rxBC4XHmFpCbTr36slkE/MA==','KeToan',1,'2026-09-22 15:34:05.8285404');
INSERT INTO NguoiDungs VALUES(3,'Lê Văn An','an.le@sv.edu.vn','AQAAAAIAAYagAAAAEHUoONZ4wjOzfn/TRB9+AE2S7EoNKYV5E2Kbnnjnf+ftGmU8oatRn1SQEUzYqp+Cgw==','SinhVien',1,'2026-09-22 15:34:05.8980319');
INSERT INTO NguoiDungs VALUES(4,'Phạm Thị Bình','binh.pham@sv.edu.vn','AQAAAAIAAYagAAAAEEBbwD5eHrrjMlwJ9li9MuKseqerbrjwzEflQg3UCxhnMv5ZzHhtt9tUfAHapbLIPw==','SinhVien',1,'2026-09-22 15:34:05.9670766');
INSERT INTO NguoiDungs VALUES(5,'Hoàng Văn Cường','cuong.hoang@sv.edu.vn','AQAAAAIAAYagAAAAEG18QRaShn42lWQUZ4faW5Kg/iVvjtMXVkL4VYAJORha6CePjEk96+h1DeLRzaibfw==','SinhVien',1,'2026-09-22 15:34:06.0351138');
INSERT INTO NguoiDungs VALUES(6,'đuvc','emtu@gmail.com','AQAAAAIAAYagAAAAELu+tunU6pgkWn2aZ60acDKzNWy/3jXteYav74wz5tyBcIUCXckXuXLM8Imf0c7weg==','SinhVien',1,'2026-09-23 03:23:45.9369411');
INSERT INTO NguoiDungs VALUES(7,'qeqe','eeeee@gmail.com','AQAAAAIAAYagAAAAEGt4zh/nhao9iSwT3n77/Mu7YJ4gTEbqwnrLgp4S6fngpXTkOPHuiGm2twkn81ER2Q==','SinhVien',1,'2026-09-23 03:36:02.6221162');
INSERT INTO NguoiDungs VALUES(8,'trang','eqeeee@gmail.com','AQAAAAIAAYagAAAAEBaLTYPp/9vn92M/x3Tv0HyzoD5SjhTIno7Zj6DgRy+PcF4E3I+NVoXakU28uQeJag==','SinhVien',1,'2026-09-23 03:51:14.531135');
CREATE TABLE IF NOT EXISTS "NoiQuys" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_NoiQuys" PRIMARY KEY AUTOINCREMENT,
    "TieuDe" TEXT NOT NULL,
    "NoiDung" TEXT NOT NULL,
    "PhienBan" TEXT NULL
);
INSERT INTO NoiQuys VALUES(1,'Điều 5 - Giờ giới nghiêm','Sinh viên nội trú phải có mặt tại phòng ở trước 23h00 hằng ngày, trừ trường hợp có đơn xin phép được quản lý KTX phê duyệt.','1.0');
INSERT INTO NoiQuys VALUES(2,'Điều 8 - Vệ sinh phòng ở','Sinh viên có trách nhiệm giữ gìn vệ sinh chung trong phòng và khu vực sinh hoạt chung; không xả rác bừa bãi.','1.0');
INSERT INTO NoiQuys VALUES(3,'Điều 12 - Khách ra vào','Khách đến thăm phải đăng ký tại bàn bảo vệ và rời khỏi ký túc xá trước 21h00.','1.0');
INSERT INTO NoiQuys VALUES(4,'Điều 15 - Cấm hút thuốc và chất cấm','Nghiêm cấm hút thuốc, sử dụng rượu bia, chất kích thích và mang chất cấm vào khu vực ký túc xá dưới mọi hình thức.','1.0');
INSERT INTO NoiQuys VALUES(5,'Điều 18 - An toàn điện, phòng cháy chữa cháy','Không tự ý câu mắc, sửa chữa thiết bị điện; không sử dụng bếp gas, bếp điện công suất lớn trong phòng ở để đảm bảo an toàn phòng cháy chữa cháy.','1.0');
INSERT INTO NoiQuys VALUES(6,'Điều 20 - Xử lý vi phạm','Sinh viên vi phạm nội quy lần đầu sẽ bị nhắc nhở và lập biên bản; vi phạm nhiều lần hoặc vi phạm nghiêm trọng có thể bị đình chỉ chỗ ở tùy theo mức độ, do Ban quản lý KTX quyết định.','1.0');
CREATE TABLE IF NOT EXISTS "ToaNhas" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ToaNhas" PRIMARY KEY AUTOINCREMENT,
    "TenToa" TEXT NOT NULL
);
INSERT INTO ToaNhas VALUES(1,'Tòa A');
INSERT INTO ToaNhas VALUES(2,'Tòa B');
CREATE TABLE IF NOT EXISTS "SinhViens" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_SinhViens" PRIMARY KEY AUTOINCREMENT,
    "NguoiDungId" INTEGER NOT NULL,
    "MaSv" TEXT NOT NULL,
    "GioiTinh" TEXT NOT NULL,
    "NgaySinh" TEXT NOT NULL,
    "Lop" TEXT NULL,
    "Sdt" TEXT NULL,
    CONSTRAINT "FK_SinhViens_NguoiDungs_NguoiDungId" FOREIGN KEY ("NguoiDungId") REFERENCES "NguoiDungs" ("Id") ON DELETE CASCADE
);
INSERT INTO SinhViens VALUES(1,3,'SV001','Nam','2004-03-12 00:00:00','CNTT1','0901111111');
INSERT INTO SinhViens VALUES(2,4,'SV002','Nu','2004-07-20 00:00:00','CNTT2','0902222222');
INSERT INTO SinhViens VALUES(3,5,'SV003','Nam','2003-11-05 00:00:00','KTPM1','0903333333');
INSERT INTO SinhViens VALUES(4,6,'dtc2344444444','Nam','2026-09-09 00:00:00','ntcc','21941929412');
INSERT INTO SinhViens VALUES(5,7,'dtc1234','Nam','2026-09-25 00:00:00','123','21941929');
INSERT INTO SinhViens VALUES(6,8,'sv3','Nu','2026-06-15 00:00:00','5','2194192966');
CREATE TABLE IF NOT EXISTS "Tangs" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Tangs" PRIMARY KEY AUTOINCREMENT,
    "ToaId" INTEGER NOT NULL,
    "SoTang" INTEGER NOT NULL,
    CONSTRAINT "FK_Tangs_ToaNhas_ToaId" FOREIGN KEY ("ToaId") REFERENCES "ToaNhas" ("Id") ON DELETE CASCADE
);
INSERT INTO Tangs VALUES(1,1,1);
INSERT INTO Tangs VALUES(2,1,2);
INSERT INTO Tangs VALUES(3,2,1);
CREATE TABLE IF NOT EXISTS "ViPhams" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ViPhams" PRIMARY KEY AUTOINCREMENT,
    "SinhVienId" INTEGER NOT NULL,
    "DieuKhoanId" INTEGER NULL,
    "MoTa" TEXT NULL,
    "MucDo" TEXT NOT NULL,
    "NgayLap" TEXT NOT NULL,
    CONSTRAINT "FK_ViPhams_NoiQuys_DieuKhoanId" FOREIGN KEY ("DieuKhoanId") REFERENCES "NoiQuys" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_ViPhams_SinhViens_SinhVienId" FOREIGN KEY ("SinhVienId") REFERENCES "SinhViens" ("Id") ON DELETE RESTRICT
);
INSERT INTO ViPhams VALUES(1,3,1,'Về phòng lúc 23h45, quá giờ giới nghiêm quy định.','Nhe','2026-09-22 15:34:06.3915181');
CREATE TABLE IF NOT EXISTS "Phongs" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Phongs" PRIMARY KEY AUTOINCREMENT,
    "TangId" INTEGER NOT NULL,
    "SoPhong" TEXT NOT NULL,
    "LoaiPhong" TEXT NULL,
    "SucChua" INTEGER NOT NULL,
    "TrangThai" TEXT NOT NULL,
    CONSTRAINT "FK_Phongs_Tangs_TangId" FOREIGN KEY ("TangId") REFERENCES "Tangs" ("Id") ON DELETE CASCADE
);
INSERT INTO Phongs VALUES(1,1,'A101','4 người',4,'ConTrong');
INSERT INTO Phongs VALUES(2,1,'A102','4 người',4,'ConTrong');
INSERT INTO Phongs VALUES(3,2,'A201','8 người',8,'ConTrong');
INSERT INTO Phongs VALUES(4,3,'B101','4 người',4,'ConTrong');
INSERT INTO Phongs VALUES(5,1,'2','2',4,'ConTrong');
INSERT INTO Phongs VALUES(6,2,'111','2',2,'ConTrong');
INSERT INTO Phongs VALUES(7,1,'3','2',2,'Day');
INSERT INTO Phongs VALUES(8,2,'333','4',4,'ConTrong');
INSERT INTO Phongs VALUES(9,1,'5','4',4,'ConTrong');
INSERT INTO Phongs VALUES(10,1,'9','4',4,'ConTrong');
CREATE TABLE IF NOT EXISTS "Giuongs" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Giuongs" PRIMARY KEY AUTOINCREMENT,
    "PhongId" INTEGER NOT NULL,
    "SoGiuong" TEXT NOT NULL,
    "TrangThai" TEXT NOT NULL,
    CONSTRAINT "FK_Giuongs_Phongs_PhongId" FOREIGN KEY ("PhongId") REFERENCES "Phongs" ("Id") ON DELETE CASCADE
);
INSERT INTO Giuongs VALUES(1,1,'G1','DaO');
INSERT INTO Giuongs VALUES(2,1,'G2','DaO');
INSERT INTO Giuongs VALUES(3,1,'G3','DaO');
INSERT INTO Giuongs VALUES(5,2,'G1','DaO');
INSERT INTO Giuongs VALUES(6,2,'G2','Trong');
INSERT INTO Giuongs VALUES(7,2,'G3','Trong');
INSERT INTO Giuongs VALUES(8,2,'G4','Trong');
INSERT INTO Giuongs VALUES(9,3,'G1','DaO');
INSERT INTO Giuongs VALUES(10,3,'G2','Trong');
INSERT INTO Giuongs VALUES(11,3,'G3','Trong');
INSERT INTO Giuongs VALUES(12,3,'G4','Trong');
INSERT INTO Giuongs VALUES(13,3,'G5','Trong');
INSERT INTO Giuongs VALUES(14,3,'G6','Trong');
INSERT INTO Giuongs VALUES(15,3,'G7','Trong');
INSERT INTO Giuongs VALUES(16,3,'G8','Trong');
INSERT INTO Giuongs VALUES(17,4,'G1','Trong');
INSERT INTO Giuongs VALUES(18,4,'G2','Trong');
INSERT INTO Giuongs VALUES(19,4,'G3','Trong');
INSERT INTO Giuongs VALUES(20,4,'G4','Trong');
INSERT INTO Giuongs VALUES(21,6,'3','Trong');
INSERT INTO Giuongs VALUES(22,5,'3','Trong');
INSERT INTO Giuongs VALUES(23,7,'333','DaO');
INSERT INTO Giuongs VALUES(24,1,'3','Trong');
INSERT INTO Giuongs VALUES(25,6,'G5','Trong');
CREATE TABLE IF NOT EXISTS "PhanAnhs" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_PhanAnhs" PRIMARY KEY AUTOINCREMENT,
    "SinhVienId" INTEGER NOT NULL,
    "PhongId" INTEGER NULL,
    "NoiDung" TEXT NOT NULL,
    "NhomVanDe" TEXT NULL,
    "TrangThai" TEXT NOT NULL,
    "NgayTao" TEXT NOT NULL,
    CONSTRAINT "FK_PhanAnhs_Phongs_PhongId" FOREIGN KEY ("PhongId") REFERENCES "Phongs" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_PhanAnhs_SinhViens_SinhVienId" FOREIGN KEY ("SinhVienId") REFERENCES "SinhViens" ("Id") ON DELETE RESTRICT
);
INSERT INTO PhanAnhs VALUES(1,1,1,'Vòi nước trong nhà vệ sinh chung bị rò rỉ, đề nghị sửa chữa.','Cơ sở vật chất','DangXuLy','2026-09-22 15:34:06.3791867');
INSERT INTO PhanAnhs VALUES(2,2,1,'Khu vực hành lang tầng 1 chưa được dọn dẹp trong 2 ngày qua.','Vệ sinh','DaXuLy','2026-09-22 15:34:06.3792993');
INSERT INTO PhanAnhs VALUES(3,1,NULL,'hỏng vòi xịt',NULL,'DangXuLy','2026-09-23 03:40:57.3920792');
INSERT INTO PhanAnhs VALUES(4,1,NULL,'Kiem tra gui phan anh',NULL,'Moi','2026-09-23 03:42:35.523079');
INSERT INTO PhanAnhs VALUES(5,1,1,'Kiem tra gui phan anh sau khi sua',NULL,'Moi','2026-09-23 03:46:33.2523944');
INSERT INTO PhanAnhs VALUES(6,1,1,'Kiem tra gui phan anh thanh cong',NULL,'Moi','2026-09-23 03:48:12.7104763');
INSERT INTO PhanAnhs VALUES(7,1,1,'hỏng vòi',NULL,'Moi','2026-09-23 03:48:59.4135597');
CREATE TABLE IF NOT EXISTS "HopDongs" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_HopDongs" PRIMARY KEY AUTOINCREMENT,
    "SinhVienId" INTEGER NOT NULL,
    "GiuongId" INTEGER NOT NULL,
    "NgayBatDau" TEXT NOT NULL,
    "NgayKetThuc" TEXT NULL,
    "TrangThai" TEXT NOT NULL,
    CONSTRAINT "FK_HopDongs_Giuongs_GiuongId" FOREIGN KEY ("GiuongId") REFERENCES "Giuongs" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_HopDongs_SinhViens_SinhVienId" FOREIGN KEY ("SinhVienId") REFERENCES "SinhViens" ("Id") ON DELETE RESTRICT
);
INSERT INTO HopDongs VALUES(1,1,1,'2026-07-22 15:34:06.325635',NULL,'DangHieuLuc');
INSERT INTO HopDongs VALUES(2,2,2,'2026-08-22 15:34:06.3256899',NULL,'DangHieuLuc');
INSERT INTO HopDongs VALUES(3,3,3,'2026-09-23 03:19:02.8901488',NULL,'DangHieuLuc');
INSERT INTO HopDongs VALUES(4,5,5,'2026-09-23 03:38:30.136279',NULL,'DangHieuLuc');
INSERT INTO HopDongs VALUES(5,4,9,'2026-09-23 03:38:34.1344751',NULL,'DangHieuLuc');
INSERT INTO HopDongs VALUES(6,6,23,'2026-09-23 03:51:41.3517683',NULL,'DangHieuLuc');
CREATE TABLE IF NOT EXISTS "Phis" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Phis" PRIMARY KEY AUTOINCREMENT,
    "HopDongId" INTEGER NOT NULL,
    "LoaiPhi" TEXT NOT NULL,
    "SoTien" TEXT NOT NULL,
    "HanThanhToan" TEXT NOT NULL,
    "TrangThaiThanhToan" TEXT NOT NULL,
    CONSTRAINT "FK_Phis_HopDongs_HopDongId" FOREIGN KEY ("HopDongId") REFERENCES "HopDongs" ("Id") ON DELETE CASCADE
);
INSERT INTO Phis VALUES(1,1,'PhiPhong','1200000.0','2026-10-02 15:34:06.348238','DaThu');
INSERT INTO Phis VALUES(3,2,'PhiPhong','1200000.0','2026-10-07 15:34:06.348282','DaThu');
INSERT INTO Phis VALUES(4,1,'PhiPhong','1500000.0','2026-10-07 00:00:00','DaThu');
INSERT INTO Phis VALUES(5,5,'PhiPhong','1000.0','2026-09-30 00:00:00','DaThu');
INSERT INTO sqlite_sequence VALUES('NguoiDungs',8);
INSERT INTO sqlite_sequence VALUES('ToaNhas',2);
INSERT INTO sqlite_sequence VALUES('Tangs',3);
INSERT INTO sqlite_sequence VALUES('Phongs',10);
INSERT INTO sqlite_sequence VALUES('Giuongs',25);
INSERT INTO sqlite_sequence VALUES('SinhViens',6);
INSERT INTO sqlite_sequence VALUES('HopDongs',6);
INSERT INTO sqlite_sequence VALUES('NoiQuys',6);
INSERT INTO sqlite_sequence VALUES('Phis',5);
INSERT INTO sqlite_sequence VALUES('PhanAnhs',7);
INSERT INTO sqlite_sequence VALUES('ViPhams',1);
CREATE INDEX "IX_Giuongs_PhongId" ON "Giuongs" ("PhongId");
CREATE INDEX "IX_HopDongs_GiuongId" ON "HopDongs" ("GiuongId");
CREATE INDEX "IX_HopDongs_SinhVienId" ON "HopDongs" ("SinhVienId");
CREATE UNIQUE INDEX "IX_NguoiDungs_Email" ON "NguoiDungs" ("Email");
CREATE INDEX "IX_PhanAnhs_PhongId" ON "PhanAnhs" ("PhongId");
CREATE INDEX "IX_PhanAnhs_SinhVienId" ON "PhanAnhs" ("SinhVienId");
CREATE INDEX "IX_Phis_HopDongId" ON "Phis" ("HopDongId");
CREATE INDEX "IX_Phongs_TangId" ON "Phongs" ("TangId");
CREATE UNIQUE INDEX "IX_SinhViens_MaSv" ON "SinhViens" ("MaSv");
CREATE UNIQUE INDEX "IX_SinhViens_NguoiDungId" ON "SinhViens" ("NguoiDungId");
CREATE INDEX "IX_Tangs_ToaId" ON "Tangs" ("ToaId");
CREATE INDEX "IX_ViPhams_DieuKhoanId" ON "ViPhams" ("DieuKhoanId");
CREATE INDEX "IX_ViPhams_SinhVienId" ON "ViPhams" ("SinhVienId");
COMMIT;
