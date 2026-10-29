# Hệ thống quản lý ký túc xá

Ứng dụng quản lý ký túc xá với ASP.NET Core 8, Entity Framework Core, SQLite và xác thực JWT. Giao diện sử dụng HTML, CSS, JavaScript thuần; chatbot tích hợp Google Gemini.

## Cấu trúc thư mục

```text
he-thong-quan-ly-ktx/
├── backend/
│   ├── Common/             # Kiểu phản hồi, lỗi và cấu hình chung
│   ├── Controllers/        # Các API
│   ├── Data/               # DbContext và dữ liệu mẫu
│   ├── Dtos/               # Dữ liệu request/response
│   ├── Middleware/         # Xử lý lỗi
│   ├── Models/             # Mô hình dữ liệu
│   ├── Prompts/            # Prompt hệ thống cho chatbot
│   ├── Properties/         # Cấu hình khởi chạy
│   ├── Services/           # JWT và chatbot
│   ├── .env.example        # Mẫu cấu hình cục bộ
│   ├── appsettings.json
│   ├── KtxAI.Api.csproj
│   └── Program.cs
├── frontend/
│   ├── css/
│   ├── js/
│   └── *.html
├── database/
│   └── ktxai.sql           # Cấu trúc và dữ liệu xuất từ SQLite
├── .vscode/                # Cấu hình chạy/debug trong VS Code
├── .gitignore
└── README.md
```

## Chạy ứng dụng

Cài .NET SDK 8.0 trở lên có hỗ trợ target `net8.0` và .NET 8 Runtime. Frontend không cần Node.js hoặc bước build riêng.

Tại thư mục gốc, chạy PowerShell:

```powershell
cd backend
Copy-Item .env.example .env
# Sửa .env: đặt Jwt__Key; thêm Gemini__ApiKey nếu sử dụng chatbot.
dotnet restore
dotnet run --launch-profile KtxAI.Api
```

- Giao diện: <http://localhost:8080>
- Swagger (môi trường Development): <http://localhost:8080/swagger>
- API: `http://localhost:8080/api`

Backend phục vụ trực tiếp các tệp trong `frontend/`, vì vậy chỉ cần chạy một tiến trình. Mã nguồn hai phần được tách riêng nhưng dùng chung địa chỉ; frontend gọi API qua đường dẫn `/api`. Không mở HTML bằng `file://`. VS Code có sẵn cấu hình F5 để chạy ứng dụng.

## Cấu hình và dữ liệu

Các thiết lập mặc định nằm trong `backend/appsettings.json`. Có thể ghi đè bằng biến môi trường hoặc `backend/.env` khi chạy từ thư mục backend:

| Biến | Ý nghĩa |
| --- | --- |
| `Jwt__Key` | Khóa ký JWT, tối thiểu 32 ký tự; cần thay khóa demo khi triển khai |
| `Gemini__ApiKey` | API key cho chatbot Gemini |
| `Gemini__Model` | Tên model Gemini mà tài khoản có quyền sử dụng |
| `ConnectionStrings__DefaultConnection` | Kết nối SQLite, mặc định `Data Source=ktxai.db` |

SQLite lưu tại `backend/ktxai.db` khi chạy theo hướng dẫn. Dữ liệu hiện có được giữ lại khi sắp xếp thư mục. Nếu chưa có cơ sở dữ liệu, ứng dụng tạo bảng và dữ liệu mẫu lúc khởi động. Sao lưu SQLite khi ứng dụng đã dừng; không đưa `.env` hoặc cơ sở dữ liệu chứa dữ liệu thực lên Git.

Các tài khoản mẫu khi tạo mới cơ sở dữ liệu:

| Vai trò | Email | Mật khẩu |
| --- | --- | --- |
| Quản lý | `quanly@ktx.edu.vn` | `QuanLy@123` |
| Kế toán | `ketoan@ktx.edu.vn` | `KeToan@123` |
| Sinh viên | `an.le@sv.edu.vn` | `SinhVien@123` |

## File cơ sở dữ liệu

File `database/ktxai.sql` chứa cấu trúc bảng, khóa ngoại, chỉ mục và dữ liệu được xuất từ cơ sở dữ liệu hiện có của hệ thống. File dùng cho **SQLite**, bao gồm 11 bảng nghiệp vụ và mật khẩu tài khoản dưới dạng hash.

Để khôi phục, cài công cụ dòng lệnh `sqlite3`, dừng ứng dụng và chạy từ thư mục gốc. Nếu `backend/ktxai.db` đã tồn tại, sao lưu rồi chuyển file đó cùng các file `-wal`/`-shm` còn lại ra nơi khác trước khi nhập; script dùng để tạo cơ sở dữ liệu mới, không gộp vào dữ liệu cũ.

```powershell
sqlite3 backend/ktxai.db ".read database/ktxai.sql"
sqlite3 backend/ktxai.db "PRAGMA integrity_check;"
```

Kết quả kiểm tra hợp lệ là `ok`. Sau đó chạy backend như hướng dẫn ở trên. File SQL là bản chụp dữ liệu tại thời điểm xuất, không tự cập nhật theo ứng dụng.

## Chức năng

- Đăng nhập và phân quyền theo vai trò.
- Quản lý sinh viên, tòa nhà, tầng, phòng, giường và hợp đồng.
- Quản lý phí, phản ánh, nội quy và thống kê dashboard.
- Chatbot hỗ trợ thông tin ký túc xá qua Gemini.

## Build và publish

Từ thư mục gốc:

```powershell
dotnet build backend/KtxAI.Api.csproj
dotnet publish backend/KtxAI.Api.csproj -c Release -o publish
cd publish
dotnet KtxAI.Api.dll --urls http://localhost:8080
```

Khi publish, frontend được chép vào `publish/wwwroot/`, prompt AI vào `publish/Prompts/`. Chạy từ thư mục publish để ứng dụng đọc đúng cấu hình, prompt và SQLite. Cấu hình bí mật qua biến môi trường hoặc `.env` riêng tại nơi triển khai. Thư mục build, cache IDE và file SQLite cục bộ được liệt kê trong `.gitignore`.
