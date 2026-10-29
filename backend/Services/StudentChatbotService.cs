using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using KtxAI.Api.Common;
using KtxAI.Api.Data;
using KtxAI.Api.Dtos;
using KtxAI.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace KtxAI.Api.Services;

// Personal queries are scoped in SQL using the authenticated account, never a supplied student ID.
// Personal data is rendered locally and is not sent to the external AI provider.
public class StudentChatbotService(AppDbContext db)
{
    private static string Normalize(string text) => string.Concat(text.ToLowerInvariant()
        .Replace('đ', 'd').Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));

    private static bool Match(string text, string pattern) => Regex.IsMatch(text, pattern);

    private static ChatbotTraLoiDto Reply(string text, string source, bool warning = false) => new()
    {
        TraLoi = text,
        NguonNoiQuy = new() { source },
        CoCanhBao = warning,
        GhiChu = "Thông tin từ hệ thống; chatbot chỉ tra cứu và hướng dẫn, không tự thay đổi dữ liệu."
    };

    public async Task<ChatbotTraLoiDto> AnswerAsync(string question, ClaimsPrincipal user)
    {
        var q = Normalize(question);
        var how = Match(q, @"lam sao|lam the nao|the nao|cach |huong dan|thu tuc|muon |xin ");
        if (Match(q, @"chuyen phong|doi phong"))
            return Reply("Để chuyển phòng: mở mục Phòng xem chỗ trống, sau đó liên hệ Ban quản lý và cung cấp phòng/giường mong muốn. Chỉ Quản lý được xác nhận chuyển phòng trong hệ thống. Chatbot chưa tạo hay duyệt yêu cầu chuyển phòng.", "Hướng dẫn chức năng chuyển phòng");
        if (Match(q, @"tra phong|roi ktx|roi ky tuc xa"))
            return Reply("Để trả phòng: xem Hóa đơn của tôi và kiểm tra các khoản chưa thanh toán, rồi liên hệ Ban quản lý để kết thúc hợp đồng. Chỉ Quản lý được ghi nhận trả phòng. Khoản nợ còn lại vẫn được theo dõi sau khi trả phòng; chatbot chưa thực hiện trả phòng.", "Hướng dẫn chức năng trả phòng");
        if (Match(q, @"dang ky (o|phong|ktx)|xep phong"))
            return Reply("Bạn có thể xem phòng và giường còn trống tại mục Phòng, sau đó liên hệ Ban quản lý để đăng ký. Quản lý kiểm tra hồ sơ và xếp giường, tạo hợp đồng. Sinh viên và chatbot không tự xếp phòng trong hệ thống.", "Hướng dẫn chức năng đăng ký lưu trú");
        if (Match(q, @"gia han"))
            return Reply("Hệ thống hiện chưa có chức năng gia hạn hợp đồng cho sinh viên. Bạn hãy liên hệ Ban quản lý; chatbot không thể xác nhận gia hạn hoặc tự đặt thời hạn mới.", "Phạm vi chức năng hiện có", true);
        if (Match(q, @"(sua|doi|cap nhat) (ho so|thong tin|so dien thoai|email)"))
            return Reply("Để sửa hồ sơ, hãy liên hệ Ban quản lý và cung cấp thông tin cần điều chỉnh. Sinh viên chỉ xem hồ sơ của mình; chức năng sửa hồ sơ hiện dành cho Quản lý. Chatbot không cập nhật hồ sơ.", "Hướng dẫn quản lý hồ sơ sinh viên");
        if ((how && Match(q, @"thanh toan|dong tien|nop tien") && !Match(q, @"da .*chua|chua .*thanh toan|con no|muon (xem|biet)")) ||
            Match(q, @"^(thanh toan|dong tien|nop tien)( giup| ho| ngay|$)"))
            return Reply("Mở Hóa đơn của tôi để xem số tiền và hạn thanh toán, rồi liên hệ Kế toán hoặc Ban quản lý để được hướng dẫn nộp tiền. Hệ thống hiện chưa tích hợp thanh toán trực tuyến. Chỉ Kế toán hoặc Quản lý xác nhận đã thu; chatbot không xác nhận hay thực hiện thanh toán.", "Hướng dẫn chức năng hóa đơn và thu tiền");
        if ((how && Match(q, @"phan anh|bao hong|khieu nai|su co") && !Match(q, @"xem|trang thai|xu ly|tinh trang")) || Match(q, @"^(gui|tao) phan anh"))
            return Reply("Mở mục Phản ánh, nhập nội dung và nhấn gửi. Hệ thống tự gắn phản ánh với tài khoản và phòng đang ở của bạn; bạn chỉ xem phản ánh của mình. Ban quản lý tiếp nhận, phân loại và cập nhật trạng thái. Chatbot chưa gửi phản ánh thay bạn.", "Hướng dẫn chức năng phản ánh");

        var profile = Match(q, @"ho so|thong tin ca nhan|ma sinh vien|ma sv|email cua|so dien thoai cua");
        var invoices = Match(q, @"hoa don|cong no|\btien\b|\bphi\b|dong tien|nop tien|thanh toan|\bno\b|han dong");
        var complaints = Match(q, @"phan anh|khieu nai|bao hong|su co");
        var contracts = Match(q, @"hop dong|lich su (o|luu tru)|ngay vao|ngay het han");
        var room = !q.Contains("trong") && Match(q, @"\b(phong|giuong|tang|toa)\b") &&
            Match(q, @"dang o|o dau|phong nao|giuong nao|tang may|toa nao|so phong (cua )?(toi|minh|em)|(phong|giuong) cua (toi|minh|em)");
        if (!profile && !invoices && !complaints && !contracts && !room)
        {
            if (Match(q, @"nghiep vu|ban lam duoc gi|ho tro gi|chao|hello|^hi$"))
                return Reply("Mình có thể tra cứu hồ sơ, phòng/giường đang ở, hợp đồng, hóa đơn và trạng thái phản ánh của bạn; đếm phòng trống, giải đáp nội quy và hướng dẫn đăng ký, chuyển/trả phòng, nộp tiền, gửi phản ánh. Ví dụ: Tôi còn nợ bao nhiêu? Tôi đang ở phòng nào? Phản ánh của tôi xử lý chưa?", "Các chức năng hỗ trợ sinh viên");
            return null;
        }
        if (!user.IsInRole("SinhVien"))
            return Reply("Tra cứu cá nhân này dành cho tài khoản Sinh viên. Quản lý và Kế toán vui lòng sử dụng các màn hình nghiệp vụ theo quyền được cấp.", "Phân quyền chatbot", true);
        if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var accountId))
            throw AppException.Forbidden("Không xác định được tài khoản đăng nhập.");
        var student = await db.SinhViens.AsNoTracking().Where(s => s.NguoiDungId == accountId)
            .Select(s => new { s.Id, s.MaSv, s.Lop, s.Sdt, s.NgaySinh, s.NguoiDung.HoTen, s.NguoiDung.Email }).SingleOrDefaultAsync();
        if (student == null)
            return Reply("Tài khoản của bạn chưa có hồ sơ sinh viên. Vui lòng liên hệ Ban quản lý để bổ sung hồ sơ.", "Hồ sơ tài khoản đăng nhập", true);

        var answer = new StringBuilder("Dữ liệu của bạn (" + student.MaSv + ") tại thời điểm hỏi:\n");
        if (profile)
            answer.AppendLine($"Hồ sơ: {student.HoTen}; mã SV {student.MaSv}; lớp {student.Lop ?? "chưa cập nhật"}; ngày sinh {student.NgaySinh:dd/MM/yyyy}; email {student.Email}; điện thoại {student.Sdt ?? "chưa cập nhật"}.");
        if (room || contracts)
        {
            var query = db.HopDongs.AsNoTracking().Where(h => h.SinhVienId == student.Id);
            var active = await query.Where(h => h.TrangThai == TrangThaiHopDong.DangHieuLuc)
                .Select(h => new { h.Id, h.NgayBatDau, h.NgayKetThuc, h.Giuong.SoGiuong, h.Giuong.Phong.SoPhong, h.Giuong.Phong.Tang.SoTang, h.Giuong.Phong.Tang.ToaNha.TenToa }).ToListAsync();
            if (active.Count == 0) answer.AppendLine("Bạn chưa có hợp đồng đang hiệu lực và chưa được ghi nhận phòng ở hiện tại.");
            foreach (var h in active)
                answer.AppendLine($"Hợp đồng #{h.Id} đang hiệu lực: {h.TenToa}, tầng {h.SoTang}, phòng {h.SoPhong}, giường {h.SoGiuong}; bắt đầu {h.NgayBatDau:dd/MM/yyyy}; ngày kết thúc: {(h.NgayKetThuc.HasValue ? h.NgayKetThuc.Value.ToString("dd/MM/yyyy") : "chưa ghi nhận")}.");
            if (contracts)
            {
                answer.AppendLine($"Tổng lịch sử: {await query.CountAsync()} hợp đồng, gồm {await query.CountAsync(h => h.TrangThai == TrangThaiHopDong.DaKetThuc)} hợp đồng đã kết thúc.");
                var ended = await query.Where(h => h.TrangThai == TrangThaiHopDong.DaKetThuc)
                    .OrderByDescending(h => h.NgayBatDau).ThenByDescending(h => h.Id)
                    .Select(h => new { h.Id, h.NgayBatDau, h.NgayKetThuc, h.Giuong.Phong.SoPhong }).Take(5).ToListAsync();
                foreach (var h in ended)
                    answer.AppendLine($"- Hợp đồng cũ #{h.Id}, phòng {h.SoPhong}: {h.NgayBatDau:dd/MM/yyyy} đến {(h.NgayKetThuc.HasValue ? h.NgayKetThuc.Value.ToString("dd/MM/yyyy") : "chưa ghi nhận ngày kết thúc")}.");
            }
        }
        if (invoices)
        {
            var fees = await db.Phis.AsNoTracking().Where(p => p.HopDong.SinhVienId == student.Id)
                .Select(p => new { p.Id, p.SoTien, p.LoaiPhi, p.HanThanhToan, p.TrangThaiThanhToan }).ToListAsync();
            var unpaid = fees.Where(p => p.TrangThaiThanhToan != TrangThaiThanhToan.DaThu).ToList();
            var today = DateTime.UtcNow.AddHours(7).Date;
            var overdue = unpaid.Where(p => p.HanThanhToan.Date < today).ToList();
            string Money(decimal value) => value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " đồng";
            answer.AppendLine($"Hóa đơn: {fees.Count} khoản; đã thu {Money(fees.Where(p => p.TrangThaiThanhToan == TrangThaiThanhToan.DaThu).Sum(p => p.SoTien))}; còn phải thanh toán {Money(unpaid.Sum(p => p.SoTien))} ({unpaid.Count} khoản), trong đó quá hạn {Money(overdue.Sum(p => p.SoTien))} ({overdue.Count} khoản).");
            foreach (var fee in unpaid.OrderBy(p => p.HanThanhToan).Take(5))
                answer.AppendLine($"- Hóa đơn #{fee.Id}: {fee.LoaiPhi switch { LoaiPhi.PhiPhong => "Phí phòng", LoaiPhi.PhiDichVu => "Phí dịch vụ", _ => "Phí khác" }}, {Money(fee.SoTien)}, hạn {fee.HanThanhToan:dd/MM/yyyy}.");
            answer.AppendLine("Mở Hóa đơn của tôi để xem tất cả. Chỉ Kế toán/Quản lý xác nhận đã thu; chatbot chưa thực hiện thanh toán.");
        }
        if (complaints)
        {
            var query = db.PhanAnhs.AsNoTracking().Where(p => p.SinhVienId == student.Id);
            var recent = await query.OrderByDescending(p => p.NgayTao).ThenByDescending(p => p.Id)
                .Select(p => new { p.Id, p.TrangThai, p.NgayTao }).Take(5).ToListAsync();
            answer.AppendLine($"Bạn có {await query.CountAsync()} phản ánh. Các phản ánh gần nhất:");
            if (recent.Count == 0) answer.AppendLine("Chưa có phản ánh nào. Bạn có thể tạo tại mục Phản ánh.");
            foreach (var p in recent)
                answer.AppendLine($"- Phản ánh #{p.Id} ({p.NgayTao:dd/MM/yyyy}): {p.TrangThai switch { TrangThaiPhanAnh.Moi => "Mới tiếp nhận", TrangThaiPhanAnh.DangXuLy => "Đang xử lý", _ => "Đã xử lý" }}.");
            answer.AppendLine("Xem nội dung và toàn bộ danh sách tại mục Phản ánh.");
        }
        return Reply(answer.ToString().Trim(), "SQL Server — dữ liệu của tài khoản sinh viên đang đăng nhập");
    }
}
