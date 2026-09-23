using System.ComponentModel.DataAnnotations;

namespace KtxAI.Api.Models
{
    // Tài khoản đăng nhập dùng chung cho cả ba vai trò (Quản lý, Sinh viên, Kế toán)
    public class NguoiDung
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string HoTen { get; set; }

        [Required, MaxLength(100)]
        public string Email { get; set; }

        [Required]
        public string MatKhauHash { get; set; }

        [Required]
        public VaiTro VaiTro { get; set; }

        public bool DangHoatDong { get; set; } = true;

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        // Điều hướng 1-1 tới hồ sơ sinh viên (chỉ có giá trị khi VaiTro = SinhVien)
        public SinhVien SinhVien { get; set; }
    }
}
