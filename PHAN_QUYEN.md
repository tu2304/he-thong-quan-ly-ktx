# Phân quyền hệ thống KTX

Quyền được kiểm tra tại API bằng JWT và vai trò trong tài khoản. Ẩn menu/nút trên giao diện chỉ hỗ trợ sử dụng; tự nhập URL hoặc gọi API vẫn phải qua kiểm tra quyền.

| Chức năng | Quản lý (`QuanLy`) | Kế toán (`KeToan`) | Sinh viên (`SinhVien`) |
| --- | --- | --- | --- |
| Dashboard tổng hợp | Xem | Xem số liệu tổng hợp | Không |
| Danh sách, chi tiết sinh viên | Xem, thêm, sửa, xóa | Không | Không |
| Hồ sơ cá nhân sinh viên `/api/sinhvien/me` | Không áp dụng | Không áp dụng | Chỉ hồ sơ của mình |
| Tòa, tầng, phòng, giường | Xem, thêm, sửa, xóa theo API hiện có | Chỉ xem | Chỉ xem |
| Hợp đồng lưu trú | Xem tất cả, xếp/chuyển/trả phòng | Xem để lập hóa đơn | Chỉ xem hợp đồng của mình |
| Hóa đơn | Xem, tạo, xác nhận đã thu, xóa | Xem, tạo, xác nhận đã thu; không xóa | Chỉ xem hóa đơn của mình |
| Phản ánh | Xem tất cả, tạo hộ sinh viên, xử lý, xóa | Không | Tạo và xem phản ánh của mình |
| Thống kê phản ánh theo nhóm | Xem | Không | Không |
| Vi phạm | Xem, tạo, xóa | Không | Không |
| Nội quy | Xem, thêm, sửa, xóa | Xem | Xem |
| Chatbot | Sử dụng | Sử dụng | Sử dụng |

Sinh viên đăng nhập được chuyển tới **Hóa đơn của tôi**; Quản lý và Kế toán tới Dashboard. Menu Sinh viên chỉ xuất hiện với Quản lý. Menu Phản ánh không xuất hiện với Kế toán. Các nút quản lý phòng chỉ xuất hiện với Quản lý.

Khách chưa đăng nhập chỉ được gọi API đăng nhập; API nghiệp vụ trả `401`. Người đã đăng nhập nhưng không đúng vai trò nhận `403`. Với hợp đồng và phản ánh không thuộc sinh viên đang đăng nhập, API trả `404`, kể cả khi đoán ID hoặc truyền `sinhVienId` của người khác.

Tài khoản bị khóa, xóa hoặc đổi vai trò sẽ không tiếp tục sử dụng token cũ; phải đăng nhập lại. Vai trò được lấy từ database và token do backend ký, không lấy từ dữ liệu vai trò do trình duyệt gửi lên.

Chatbot sinh viên tra cứu hồ sơ, phòng/giường hiện tại, hợp đồng, công nợ và phản ánh theo ID tài khoản trong JWT. Truyền `sinhVienId`, `userId` hoặc nhắc mã SV khác trong câu hỏi không đổi được phạm vi dữ liệu. Phần tra cứu cá nhân được xử lý trực tiếp tại backend, không gửi dữ liệu này sang Gemini. Chatbot không cập nhật hồ sơ, xếp/chuyển/trả phòng, thanh toán hoặc gửi phản ánh; nó hướng dẫn tới chức năng tương ứng và người có thẩm quyền.

Một số chức năng (hợp đồng, nội quy, vi phạm) hiện được cung cấp qua API, chưa có trang quản trị riêng. Bảng này mô tả các quyền trên chức năng hiện có.

## Kiểm tra

Khi backend đang chạy với database có các tài khoản mẫu:

```powershell
powershell -ExecutionPolicy Bypass -File tests/Authorization/check-api.ps1
node tests/Authorization/check-ui.cjs
```

Kiểm tra API chỉ đọc dữ liệu và thử các thao tác bị cấm, không sửa dữ liệu. Bao gồm truy cập chéo sinh viên, giả mạo bộ lọc, quyền Kế toán và khách chưa đăng nhập.
