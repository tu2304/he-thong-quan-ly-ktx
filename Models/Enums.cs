namespace KtxAI.Api.Models
{
    // Vai trò người dùng trong hệ thống
    public enum VaiTro
    {
        QuanLy = 0,
        SinhVien = 1,
        KeToan = 2
    }

    public enum GioiTinh
    {
        Nam = 0,
        Nu = 1,
        Khac = 2
    }

    public enum TrangThaiPhong
    {
        ConTrong = 0,
        Day = 1,
        KhoaTam = 2
    }

    public enum TrangThaiGiuong
    {
        Trong = 0,
        DaO = 1
    }

    public enum TrangThaiHopDong
    {
        DangHieuLuc = 0,
        DaKetThuc = 1
    }

    public enum LoaiPhi
    {
        PhiPhong = 0,
        PhiDichVu = 1,
        PhiKhac = 2
    }

    public enum TrangThaiThanhToan
    {
        ChuaThu = 0,
        DaThu = 1,
        QuaHan = 2
    }

    public enum TrangThaiPhanAnh
    {
        Moi = 0,
        DangXuLy = 1,
        DaXuLy = 2
    }

    public enum MucDoViPham
    {
        Nhe = 0,
        TrungBinh = 1,
        Nang = 2
    }
}
