-- SQL Server: database KtxAI, exported from the existing SQLite database.
-- Run in SSMS. Only imports into an empty database; existing tables are preserved.
USE [master];
GO
IF DB_ID(N'KtxAI') IS NULL CREATE DATABASE [KtxAI];
GO
USE [KtxAI];
GO
IF EXISTS (SELECT 1 FROM sys.tables WHERE is_ms_shipped = 0)
BEGIN
    RAISERROR(N'KtxAI already contains tables. Import into an empty database.', 16, 1);
    SET NOEXEC ON;
END
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO
CREATE TABLE [NguoiDungs] (
    [Id] int NOT NULL IDENTITY,
    [HoTen] nvarchar(100) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [MatKhauHash] nvarchar(max) NOT NULL,
    [VaiTro] nvarchar(max) NOT NULL,
    [DangHoatDong] bit NOT NULL,
    [NgayTao] datetime2 NOT NULL,
    CONSTRAINT [PK_NguoiDungs] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [NoiQuys] (
    [Id] int NOT NULL IDENTITY,
    [TieuDe] nvarchar(150) NOT NULL,
    [NoiDung] nvarchar(max) NOT NULL,
    [PhienBan] nvarchar(20) NULL,
    CONSTRAINT [PK_NoiQuys] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ToaNhas] (
    [Id] int NOT NULL IDENTITY,
    [TenToa] nvarchar(50) NOT NULL,
    CONSTRAINT [PK_ToaNhas] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SinhViens] (
    [Id] int NOT NULL IDENTITY,
    [NguoiDungId] int NOT NULL,
    [MaSv] nvarchar(20) NOT NULL,
    [GioiTinh] nvarchar(max) NOT NULL,
    [NgaySinh] datetime2 NOT NULL,
    [Lop] nvarchar(50) NULL,
    [Sdt] nvarchar(15) NULL,
    CONSTRAINT [PK_SinhViens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SinhViens_NguoiDungs_NguoiDungId] FOREIGN KEY ([NguoiDungId]) REFERENCES [NguoiDungs] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Tangs] (
    [Id] int NOT NULL IDENTITY,
    [ToaId] int NOT NULL,
    [SoTang] int NOT NULL,
    CONSTRAINT [PK_Tangs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Tangs_ToaNhas_ToaId] FOREIGN KEY ([ToaId]) REFERENCES [ToaNhas] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [ViPhams] (
    [Id] int NOT NULL IDENTITY,
    [SinhVienId] int NOT NULL,
    [DieuKhoanId] int NULL,
    [MoTa] nvarchar(max) NULL,
    [MucDo] nvarchar(max) NOT NULL,
    [NgayLap] datetime2 NOT NULL,
    CONSTRAINT [PK_ViPhams] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ViPhams_NoiQuys_DieuKhoanId] FOREIGN KEY ([DieuKhoanId]) REFERENCES [NoiQuys] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_ViPhams_SinhViens_SinhVienId] FOREIGN KEY ([SinhVienId]) REFERENCES [SinhViens] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Phongs] (
    [Id] int NOT NULL IDENTITY,
    [TangId] int NOT NULL,
    [SoPhong] nvarchar(10) NOT NULL,
    [LoaiPhong] nvarchar(30) NULL,
    [SucChua] int NOT NULL,
    [TrangThai] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Phongs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Phongs_Tangs_TangId] FOREIGN KEY ([TangId]) REFERENCES [Tangs] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Giuongs] (
    [Id] int NOT NULL IDENTITY,
    [PhongId] int NOT NULL,
    [SoGiuong] nvarchar(10) NOT NULL,
    [TrangThai] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Giuongs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Giuongs_Phongs_PhongId] FOREIGN KEY ([PhongId]) REFERENCES [Phongs] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PhanAnhs] (
    [Id] int NOT NULL IDENTITY,
    [SinhVienId] int NOT NULL,
    [PhongId] int NULL,
    [NoiDung] nvarchar(max) NOT NULL,
    [NhomVanDe] nvarchar(50) NULL,
    [TrangThai] nvarchar(max) NOT NULL,
    [NgayTao] datetime2 NOT NULL,
    CONSTRAINT [PK_PhanAnhs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PhanAnhs_Phongs_PhongId] FOREIGN KEY ([PhongId]) REFERENCES [Phongs] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_PhanAnhs_SinhViens_SinhVienId] FOREIGN KEY ([SinhVienId]) REFERENCES [SinhViens] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [HopDongs] (
    [Id] int NOT NULL IDENTITY,
    [SinhVienId] int NOT NULL,
    [GiuongId] int NOT NULL,
    [NgayBatDau] datetime2 NOT NULL,
    [NgayKetThuc] datetime2 NULL,
    [TrangThai] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_HopDongs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_HopDongs_Giuongs_GiuongId] FOREIGN KEY ([GiuongId]) REFERENCES [Giuongs] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_HopDongs_SinhViens_SinhVienId] FOREIGN KEY ([SinhVienId]) REFERENCES [SinhViens] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Phis] (
    [Id] int NOT NULL IDENTITY,
    [HopDongId] int NOT NULL,
    [LoaiPhi] nvarchar(max) NOT NULL,
    [SoTien] decimal(18,2) NOT NULL,
    [HanThanhToan] datetime2 NOT NULL,
    [TrangThaiThanhToan] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Phis] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Phis_HopDongs_HopDongId] FOREIGN KEY ([HopDongId]) REFERENCES [HopDongs] ([Id]) ON DELETE CASCADE
);
GO


CREATE INDEX [IX_Giuongs_PhongId] ON [Giuongs] ([PhongId]);
GO


CREATE INDEX [IX_HopDongs_GiuongId] ON [HopDongs] ([GiuongId]);
GO


CREATE INDEX [IX_HopDongs_SinhVienId] ON [HopDongs] ([SinhVienId]);
GO


CREATE UNIQUE INDEX [IX_NguoiDungs_Email] ON [NguoiDungs] ([Email]);
GO


CREATE INDEX [IX_PhanAnhs_PhongId] ON [PhanAnhs] ([PhongId]);
GO


CREATE INDEX [IX_PhanAnhs_SinhVienId] ON [PhanAnhs] ([SinhVienId]);
GO


CREATE INDEX [IX_Phis_HopDongId] ON [Phis] ([HopDongId]);
GO


CREATE INDEX [IX_Phongs_TangId] ON [Phongs] ([TangId]);
GO


CREATE UNIQUE INDEX [IX_SinhViens_MaSv] ON [SinhViens] ([MaSv]);
GO


CREATE UNIQUE INDEX [IX_SinhViens_NguoiDungId] ON [SinhViens] ([NguoiDungId]);
GO


CREATE INDEX [IX_Tangs_ToaId] ON [Tangs] ([ToaId]);
GO


CREATE INDEX [IX_ViPhams_DieuKhoanId] ON [ViPhams] ([DieuKhoanId]);
GO


CREATE INDEX [IX_ViPhams_SinhVienId] ON [ViPhams] ([SinhVienId]);
GO



SET IDENTITY_INSERT [NguoiDungs] ON;
INSERT INTO [NguoiDungs] ([Id],[HoTen],[Email],[MatKhauHash],[VaiTro],[DangHoatDong],[NgayTao]) VALUES(1,N'Nguyễn Văn Quản',N'quanly@ktx.edu.vn',N'AQAAAAIAAYagAAAAEF3DwWvwnPJesjT4Oho+PDs295U64Pu+1GZ796+AuyfzL3NMCtyyiMhVvr0yxzNhqw==',N'QuanLy',1,N'2026-09-22 15:34:05.7552992');
INSERT INTO [NguoiDungs] ([Id],[HoTen],[Email],[MatKhauHash],[VaiTro],[DangHoatDong],[NgayTao]) VALUES(2,N'Trần Thị Kế',N'ketoan@ktx.edu.vn',N'AQAAAAIAAYagAAAAELtZWcyNOdrpOvU8axQUfZcVaT8A5MLeSx/AvQzms39rxBC4XHmFpCbTr36slkE/MA==',N'KeToan',1,N'2026-09-22 15:34:05.8285404');
INSERT INTO [NguoiDungs] ([Id],[HoTen],[Email],[MatKhauHash],[VaiTro],[DangHoatDong],[NgayTao]) VALUES(3,N'Lê Văn An',N'an.le@sv.edu.vn',N'AQAAAAIAAYagAAAAEHUoONZ4wjOzfn/TRB9+AE2S7EoNKYV5E2Kbnnjnf+ftGmU8oatRn1SQEUzYqp+Cgw==',N'SinhVien',1,N'2026-09-22 15:34:05.8980319');
INSERT INTO [NguoiDungs] ([Id],[HoTen],[Email],[MatKhauHash],[VaiTro],[DangHoatDong],[NgayTao]) VALUES(4,N'Phạm Thị Bình',N'binh.pham@sv.edu.vn',N'AQAAAAIAAYagAAAAEEBbwD5eHrrjMlwJ9li9MuKseqerbrjwzEflQg3UCxhnMv5ZzHhtt9tUfAHapbLIPw==',N'SinhVien',1,N'2026-09-22 15:34:05.9670766');
INSERT INTO [NguoiDungs] ([Id],[HoTen],[Email],[MatKhauHash],[VaiTro],[DangHoatDong],[NgayTao]) VALUES(5,N'Hoàng Văn Cường',N'cuong.hoang@sv.edu.vn',N'AQAAAAIAAYagAAAAEG18QRaShn42lWQUZ4faW5Kg/iVvjtMXVkL4VYAJORha6CePjEk96+h1DeLRzaibfw==',N'SinhVien',1,N'2026-09-22 15:34:06.0351138');
INSERT INTO [NguoiDungs] ([Id],[HoTen],[Email],[MatKhauHash],[VaiTro],[DangHoatDong],[NgayTao]) VALUES(6,N'đuvc',N'emtu@gmail.com',N'AQAAAAIAAYagAAAAELu+tunU6pgkWn2aZ60acDKzNWy/3jXteYav74wz5tyBcIUCXckXuXLM8Imf0c7weg==',N'SinhVien',1,N'2026-09-23 03:23:45.9369411');
INSERT INTO [NguoiDungs] ([Id],[HoTen],[Email],[MatKhauHash],[VaiTro],[DangHoatDong],[NgayTao]) VALUES(7,N'qeqe',N'eeeee@gmail.com',N'AQAAAAIAAYagAAAAEGt4zh/nhao9iSwT3n77/Mu7YJ4gTEbqwnrLgp4S6fngpXTkOPHuiGm2twkn81ER2Q==',N'SinhVien',1,N'2026-09-23 03:36:02.6221162');
INSERT INTO [NguoiDungs] ([Id],[HoTen],[Email],[MatKhauHash],[VaiTro],[DangHoatDong],[NgayTao]) VALUES(8,N'trang',N'eqeeee@gmail.com',N'AQAAAAIAAYagAAAAEBaLTYPp/9vn92M/x3Tv0HyzoD5SjhTIno7Zj6DgRy+PcF4E3I+NVoXakU28uQeJag==',N'SinhVien',1,N'2026-09-23 03:51:14.531135');

SET IDENTITY_INSERT [NguoiDungs] OFF;
SET IDENTITY_INSERT [NoiQuys] ON;
INSERT INTO [NoiQuys] ([Id],[TieuDe],[NoiDung],[PhienBan]) VALUES(1,N'Điều 5 - Giờ giới nghiêm',N'Sinh viên nội trú phải có mặt tại phòng ở trước 23h00 hằng ngày, trừ trường hợp có đơn xin phép được quản lý KTX phê duyệt.',N'1.0');
INSERT INTO [NoiQuys] ([Id],[TieuDe],[NoiDung],[PhienBan]) VALUES(2,N'Điều 8 - Vệ sinh phòng ở',N'Sinh viên có trách nhiệm giữ gìn vệ sinh chung trong phòng và khu vực sinh hoạt chung; không xả rác bừa bãi.',N'1.0');
INSERT INTO [NoiQuys] ([Id],[TieuDe],[NoiDung],[PhienBan]) VALUES(3,N'Điều 12 - Khách ra vào',N'Khách đến thăm phải đăng ký tại bàn bảo vệ và rời khỏi ký túc xá trước 21h00.',N'1.0');
INSERT INTO [NoiQuys] ([Id],[TieuDe],[NoiDung],[PhienBan]) VALUES(4,N'Điều 15 - Cấm hút thuốc và chất cấm',N'Nghiêm cấm hút thuốc, sử dụng rượu bia, chất kích thích và mang chất cấm vào khu vực ký túc xá dưới mọi hình thức.',N'1.0');
INSERT INTO [NoiQuys] ([Id],[TieuDe],[NoiDung],[PhienBan]) VALUES(5,N'Điều 18 - An toàn điện, phòng cháy chữa cháy',N'Không tự ý câu mắc, sửa chữa thiết bị điện; không sử dụng bếp gas, bếp điện công suất lớn trong phòng ở để đảm bảo an toàn phòng cháy chữa cháy.',N'1.0');
INSERT INTO [NoiQuys] ([Id],[TieuDe],[NoiDung],[PhienBan]) VALUES(6,N'Điều 20 - Xử lý vi phạm',N'Sinh viên vi phạm nội quy lần đầu sẽ bị nhắc nhở và lập biên bản; vi phạm nhiều lần hoặc vi phạm nghiêm trọng có thể bị đình chỉ chỗ ở tùy theo mức độ, do Ban quản lý KTX quyết định.',N'1.0');

SET IDENTITY_INSERT [NoiQuys] OFF;
SET IDENTITY_INSERT [ToaNhas] ON;
INSERT INTO [ToaNhas] ([Id],[TenToa]) VALUES(1,N'Tòa A');
INSERT INTO [ToaNhas] ([Id],[TenToa]) VALUES(2,N'Tòa B');

SET IDENTITY_INSERT [ToaNhas] OFF;
SET IDENTITY_INSERT [SinhViens] ON;
INSERT INTO [SinhViens] ([Id],[NguoiDungId],[MaSv],[GioiTinh],[NgaySinh],[Lop],[Sdt]) VALUES(1,3,N'SV001',N'Nam',N'2004-03-12 00:00:00',N'CNTT1',N'0901111111');
INSERT INTO [SinhViens] ([Id],[NguoiDungId],[MaSv],[GioiTinh],[NgaySinh],[Lop],[Sdt]) VALUES(2,4,N'SV002',N'Nu',N'2004-07-20 00:00:00',N'CNTT2',N'0902222222');
INSERT INTO [SinhViens] ([Id],[NguoiDungId],[MaSv],[GioiTinh],[NgaySinh],[Lop],[Sdt]) VALUES(3,5,N'SV003',N'Nam',N'2003-11-05 00:00:00',N'KTPM1',N'0903333333');
INSERT INTO [SinhViens] ([Id],[NguoiDungId],[MaSv],[GioiTinh],[NgaySinh],[Lop],[Sdt]) VALUES(4,6,N'dtc2344444444',N'Nam',N'2026-09-09 00:00:00',N'ntcc',N'21941929412');
INSERT INTO [SinhViens] ([Id],[NguoiDungId],[MaSv],[GioiTinh],[NgaySinh],[Lop],[Sdt]) VALUES(5,7,N'dtc1234',N'Nam',N'2026-09-25 00:00:00',N'123',N'21941929');
INSERT INTO [SinhViens] ([Id],[NguoiDungId],[MaSv],[GioiTinh],[NgaySinh],[Lop],[Sdt]) VALUES(6,8,N'sv3',N'Nu',N'2026-06-15 00:00:00',N'5',N'2194192966');

SET IDENTITY_INSERT [SinhViens] OFF;
SET IDENTITY_INSERT [Tangs] ON;
INSERT INTO [Tangs] ([Id],[ToaId],[SoTang]) VALUES(1,1,1);
INSERT INTO [Tangs] ([Id],[ToaId],[SoTang]) VALUES(2,1,2);
INSERT INTO [Tangs] ([Id],[ToaId],[SoTang]) VALUES(3,2,1);

SET IDENTITY_INSERT [Tangs] OFF;
SET IDENTITY_INSERT [Phongs] ON;
INSERT INTO [Phongs] ([Id],[TangId],[SoPhong],[LoaiPhong],[SucChua],[TrangThai]) VALUES(1,1,N'A101',N'4 người',4,N'ConTrong');
INSERT INTO [Phongs] ([Id],[TangId],[SoPhong],[LoaiPhong],[SucChua],[TrangThai]) VALUES(2,1,N'A102',N'4 người',4,N'ConTrong');
INSERT INTO [Phongs] ([Id],[TangId],[SoPhong],[LoaiPhong],[SucChua],[TrangThai]) VALUES(3,2,N'A201',N'8 người',8,N'ConTrong');
INSERT INTO [Phongs] ([Id],[TangId],[SoPhong],[LoaiPhong],[SucChua],[TrangThai]) VALUES(4,3,N'B101',N'4 người',4,N'ConTrong');
INSERT INTO [Phongs] ([Id],[TangId],[SoPhong],[LoaiPhong],[SucChua],[TrangThai]) VALUES(5,1,N'2',N'2',4,N'ConTrong');
INSERT INTO [Phongs] ([Id],[TangId],[SoPhong],[LoaiPhong],[SucChua],[TrangThai]) VALUES(6,2,N'111',N'2',2,N'ConTrong');
INSERT INTO [Phongs] ([Id],[TangId],[SoPhong],[LoaiPhong],[SucChua],[TrangThai]) VALUES(7,1,N'3',N'2',2,N'Day');
INSERT INTO [Phongs] ([Id],[TangId],[SoPhong],[LoaiPhong],[SucChua],[TrangThai]) VALUES(8,2,N'333',N'4',4,N'ConTrong');
INSERT INTO [Phongs] ([Id],[TangId],[SoPhong],[LoaiPhong],[SucChua],[TrangThai]) VALUES(9,1,N'5',N'4',4,N'ConTrong');
INSERT INTO [Phongs] ([Id],[TangId],[SoPhong],[LoaiPhong],[SucChua],[TrangThai]) VALUES(10,1,N'9',N'4',4,N'ConTrong');

SET IDENTITY_INSERT [Phongs] OFF;
SET IDENTITY_INSERT [Giuongs] ON;
INSERT INTO [Giuongs] ([Id],[PhongId],[SoGiuong],[TrangThai]) VALUES(1,1,N'G1',N'DaO');
INSERT INTO [Giuongs] ([Id],[PhongId],[SoGiuong],[TrangThai]) VALUES(2,1,N'G2',N'DaO');
INSERT INTO [Giuongs] ([Id],[PhongId],[SoGiuong],[TrangThai]) VALUES(3,1,N'G3',N'DaO');
INSERT INTO [Giuongs] ([Id],[PhongId],[SoGiuong],[TrangThai]) VALUES(5,2,N'G1',N'DaO');
INSERT INTO [Giuongs] ([Id],[PhongId],[SoGiuong],[TrangThai]) VALUES(6,2,N'G2',N'Trong');
INSERT INTO [Giuongs] ([Id],[PhongId],[SoGiuong],[TrangThai]) VALUES(7,2,N'G3',N'Trong');
INSERT INTO [Giuongs] ([Id],[PhongId],[SoGiuong],[TrangThai]) VALUES(8,2,N'G4',N'Trong');
INSERT INTO [Giuongs] ([Id],[PhongId],[SoGiuong],[TrangThai]) VALUES(9,3,N'G1',N'DaO');
INSERT INTO [Giuongs] ([Id],[PhongId],[SoGiuong],[TrangThai]) VALUES(10,3,N'G2',N'Trong');
INSERT INTO [Giuongs] ([Id],[PhongId],[SoGiuong],[TrangThai]) VALUES(11,3,N'G3',N'Trong');
INSERT INTO [Giuongs] ([Id],[PhongId],[SoGiuong],[TrangThai]) VALUES(12,3,N'G4',N'Trong');
INSERT INTO [Giuongs] ([Id],[PhongId],[SoGiuong],[TrangThai]) VALUES(13,3,N'G5',N'Trong');
INSERT INTO [Giuongs] ([Id],[PhongId],[SoGiuong],[TrangThai]) VALUES(14,3,N'G6',N'Trong');
INSERT INTO [Giuongs] ([Id],[PhongId],[SoGiuong],[TrangThai]) VALUES(15,3,N'G7',N'Trong');
INSERT INTO [Giuongs] ([Id],[PhongId],[SoGiuong],[TrangThai]) VALUES(16,3,N'G8',N'Trong');
INSERT INTO [Giuongs] ([Id],[PhongId],[SoGiuong],[TrangThai]) VALUES(17,4,N'G1',N'Trong');
INSERT INTO [Giuongs] ([Id],[PhongId],[SoGiuong],[TrangThai]) VALUES(18,4,N'G2',N'Trong');
INSERT INTO [Giuongs] ([Id],[PhongId],[SoGiuong],[TrangThai]) VALUES(19,4,N'G3',N'Trong');
INSERT INTO [Giuongs] ([Id],[PhongId],[SoGiuong],[TrangThai]) VALUES(20,4,N'G4',N'Trong');
INSERT INTO [Giuongs] ([Id],[PhongId],[SoGiuong],[TrangThai]) VALUES(21,6,N'3',N'Trong');
INSERT INTO [Giuongs] ([Id],[PhongId],[SoGiuong],[TrangThai]) VALUES(22,5,N'3',N'Trong');
INSERT INTO [Giuongs] ([Id],[PhongId],[SoGiuong],[TrangThai]) VALUES(23,7,N'333',N'DaO');
INSERT INTO [Giuongs] ([Id],[PhongId],[SoGiuong],[TrangThai]) VALUES(24,1,N'3',N'Trong');
INSERT INTO [Giuongs] ([Id],[PhongId],[SoGiuong],[TrangThai]) VALUES(25,6,N'G5',N'Trong');

SET IDENTITY_INSERT [Giuongs] OFF;
SET IDENTITY_INSERT [HopDongs] ON;
INSERT INTO [HopDongs] ([Id],[SinhVienId],[GiuongId],[NgayBatDau],[NgayKetThuc],[TrangThai]) VALUES(1,1,1,N'2026-07-22 15:34:06.325635',NULL,N'DangHieuLuc');
INSERT INTO [HopDongs] ([Id],[SinhVienId],[GiuongId],[NgayBatDau],[NgayKetThuc],[TrangThai]) VALUES(2,2,2,N'2026-08-22 15:34:06.3256899',NULL,N'DangHieuLuc');
INSERT INTO [HopDongs] ([Id],[SinhVienId],[GiuongId],[NgayBatDau],[NgayKetThuc],[TrangThai]) VALUES(3,3,3,N'2026-09-23 03:19:02.8901488',NULL,N'DangHieuLuc');
INSERT INTO [HopDongs] ([Id],[SinhVienId],[GiuongId],[NgayBatDau],[NgayKetThuc],[TrangThai]) VALUES(4,5,5,N'2026-09-23 03:38:30.136279',NULL,N'DangHieuLuc');
INSERT INTO [HopDongs] ([Id],[SinhVienId],[GiuongId],[NgayBatDau],[NgayKetThuc],[TrangThai]) VALUES(5,4,9,N'2026-09-23 03:38:34.1344751',NULL,N'DangHieuLuc');
INSERT INTO [HopDongs] ([Id],[SinhVienId],[GiuongId],[NgayBatDau],[NgayKetThuc],[TrangThai]) VALUES(6,6,23,N'2026-09-23 03:51:41.3517683',NULL,N'DangHieuLuc');

SET IDENTITY_INSERT [HopDongs] OFF;
SET IDENTITY_INSERT [PhanAnhs] ON;
INSERT INTO [PhanAnhs] ([Id],[SinhVienId],[PhongId],[NoiDung],[NhomVanDe],[TrangThai],[NgayTao]) VALUES(1,1,1,N'Vòi nước trong nhà vệ sinh chung bị rò rỉ, đề nghị sửa chữa.',N'Cơ sở vật chất',N'DangXuLy',N'2026-09-22 15:34:06.3791867');
INSERT INTO [PhanAnhs] ([Id],[SinhVienId],[PhongId],[NoiDung],[NhomVanDe],[TrangThai],[NgayTao]) VALUES(2,2,1,N'Khu vực hành lang tầng 1 chưa được dọn dẹp trong 2 ngày qua.',N'Vệ sinh',N'DaXuLy',N'2026-09-22 15:34:06.3792993');
INSERT INTO [PhanAnhs] ([Id],[SinhVienId],[PhongId],[NoiDung],[NhomVanDe],[TrangThai],[NgayTao]) VALUES(3,1,NULL,N'hỏng vòi xịt',NULL,N'DangXuLy',N'2026-09-23 03:40:57.3920792');
INSERT INTO [PhanAnhs] ([Id],[SinhVienId],[PhongId],[NoiDung],[NhomVanDe],[TrangThai],[NgayTao]) VALUES(4,1,NULL,N'Kiem tra gui phan anh',NULL,N'Moi',N'2026-09-23 03:42:35.523079');
INSERT INTO [PhanAnhs] ([Id],[SinhVienId],[PhongId],[NoiDung],[NhomVanDe],[TrangThai],[NgayTao]) VALUES(5,1,1,N'Kiem tra gui phan anh sau khi sua',NULL,N'Moi',N'2026-09-23 03:46:33.2523944');
INSERT INTO [PhanAnhs] ([Id],[SinhVienId],[PhongId],[NoiDung],[NhomVanDe],[TrangThai],[NgayTao]) VALUES(6,1,1,N'Kiem tra gui phan anh thanh cong',NULL,N'Moi',N'2026-09-23 03:48:12.7104763');
INSERT INTO [PhanAnhs] ([Id],[SinhVienId],[PhongId],[NoiDung],[NhomVanDe],[TrangThai],[NgayTao]) VALUES(7,1,1,N'hỏng vòi',NULL,N'Moi',N'2026-09-23 03:48:59.4135597');

SET IDENTITY_INSERT [PhanAnhs] OFF;
SET IDENTITY_INSERT [ViPhams] ON;
INSERT INTO [ViPhams] ([Id],[SinhVienId],[DieuKhoanId],[MoTa],[MucDo],[NgayLap]) VALUES(1,3,1,N'Về phòng lúc 23h45, quá giờ giới nghiêm quy định.',N'Nhe',N'2026-09-22 15:34:06.3915181');

SET IDENTITY_INSERT [ViPhams] OFF;
SET IDENTITY_INSERT [Phis] ON;
INSERT INTO [Phis] ([Id],[HopDongId],[LoaiPhi],[SoTien],[HanThanhToan],[TrangThaiThanhToan]) VALUES(1,1,N'PhiPhong',N'1200000.0',N'2026-10-02 15:34:06.348238',N'DaThu');
INSERT INTO [Phis] ([Id],[HopDongId],[LoaiPhi],[SoTien],[HanThanhToan],[TrangThaiThanhToan]) VALUES(3,2,N'PhiPhong',N'1200000.0',N'2026-10-07 15:34:06.348282',N'DaThu');
INSERT INTO [Phis] ([Id],[HopDongId],[LoaiPhi],[SoTien],[HanThanhToan],[TrangThaiThanhToan]) VALUES(4,1,N'PhiPhong',N'1500000.0',N'2026-10-07 00:00:00',N'DaThu');
INSERT INTO [Phis] ([Id],[HopDongId],[LoaiPhi],[SoTien],[HanThanhToan],[TrangThaiThanhToan]) VALUES(5,5,N'PhiPhong',N'1000.0',N'2026-09-30 00:00:00',N'DaThu');

SET IDENTITY_INSERT [Phis] OFF;
COMMIT TRANSACTION;
GO

SET NOEXEC OFF;
GO
