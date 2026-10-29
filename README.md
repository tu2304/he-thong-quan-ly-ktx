# Hệ thống quản lý ký túc xá

Ứng dụng quản lý ký túc xá với ASP.NET Core 8, Entity Framework Core, SQL Server và xác thực JWT. Giao diện sử dụng HTML, CSS, JavaScript thuần; chatbot tích hợp Gemini.

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
│   ├── ktxai-sqlserver.sql # Cấu trúc và dữ liệu cho SQL Server
│   └── ktxai.sql           # Bản xuất SQLite cũ để tham khảo
├── .vscode/                # Cấu hình chạy/debug trong VS Code
├── .gitignore
└── README.md
```

## Chạy ứng dụng

Cài SQL Server và SQL Server Management Studio (SSMS). Cài .NET SDK 8.0 trở lên có hỗ trợ target `net8.0` và .NET 8 Runtime. Frontend không cần Node.js hoặc bước build riêng.

Tại thư mục gốc, chạy PowerShell:

```powershell
cd backend
Copy-Item .env.example .env
# Sửa .env: đặt Jwt__Key, chuỗi kết nối SQL Server; thêm Gemini__ApiKey nếu dùng chatbot.
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
| `ConnectionStrings__DefaultConnection` | Kết nối SQL Server, mặc định server `localhost`, database `KtxAI`, Windows Authentication |

Ứng dụng dùng database **KtxAI** trên SQL Server. Chuỗi kết nối mặc định:

```text
Server=localhost;Database=KtxAI;Trusted_Connection=True;TrustServerCertificate=True
```

Trong SSMS, kết nối tới `localhost` bằng Windows Authentication và Refresh mục Databases để thấy `KtxAI`. Nếu dùng instance khác, đặt đúng Server trong `backend/.env`, ví dụ `Server=localhost\SQLEXPRESS;Database=KtxAI;Trusted_Connection=True;TrustServerCertificate=True`. Trong file `.env`, chỉ dùng một dấu `\` giữa tên máy và instance; trong JSON cần viết `\\`.

Nếu dùng SQL Server Authentication, thay `Trusted_Connection=True` bằng `User Id=TEN_DANG_NHAP;Password=MAT_KHAU` trong `.env`. Không đưa mật khẩu lên Git. `TrustServerCertificate=True` dành cho máy phát triển; môi trường triển khai cần cấu hình chứng chỉ phù hợp.

Khi chưa có database, backend tạo bảng và dữ liệu mẫu lúc khởi động nếu tài khoản có quyền tạo database. Muốn giữ dữ liệu từ SQLite, nhập file SQL Server bên dưới **trước khi chạy backend lần đầu**. File `backend/ktxai.db` được giữ làm bản dữ liệu cũ, backend không còn sử dụng file này.

Các tài khoản mẫu khi tạo mới cơ sở dữ liệu:

| Vai trò | Email | Mật khẩu |
| --- | --- | --- |
| Quản lý | `quanly@ktx.edu.vn` | `QuanLy@123` |
| Kế toán | `ketoan@ktx.edu.vn` | `KeToan@123` |
| Sinh viên | `an.le@sv.edu.vn` | `SinhVien@123` |

## Cấu hình chatbot Gemini

File `backend/.env` đã được chuẩn bị trên máy phát triển. Mở file này, điền Gemini API key vào dòng:

```dotenv
Gemini__ApiKey=YOUR_GEMINI_API_KEY
```

Lấy key tại <https://aistudio.google.com/apikey>. Giữ nguyên các dòng cấu hình JWT, SQL Server và model, rồi khởi động lại backend bằng F5. File `.env.example` chỉ là mẫu, không điền key thật vào đó. Khi clone dự án sang máy khác, sao chép `backend/.env.example` thành `backend/.env` trước khi cấu hình.

Chatbot dùng [Gemini generateContent API](https://ai.google.dev/api/generate-content), mặc định model `gemini-3.5-flash` (có thể đổi bằng `Gemini__Model`). Backend gửi câu hỏi cùng nội quy liên quan trong SQL Server, không gửi hồ sơ sinh viên hoặc thanh toán. API key chỉ được sử dụng ở backend.

Đăng nhập, mở trang Chatbot và hỏi ví dụ: **“Giờ giới nghiêm là mấy giờ?”**. Khi chưa có key hoặc dịch vụ lỗi/giới hạn lượt gọi, chatbot trả nội quy nội bộ kèm cảnh báo. Câu hỏi tổng số phòng/giường trống được trả lời trực tiếp từ SQL Server, loại phòng đầy/khóa và phòng không có giường trống; không gửi dữ liệu người ở tới AI. Phòng còn chỗ được phân biệt với phòng hoàn toàn chưa có người ở. Câu hỏi khác không khớp nội quy không gọi AI. Quyền sử dụng model và hạn mức phụ thuộc tài khoản Gemini của bạn.

Kiểm tra bằng phản hồi giả lập, không gọi Gemini thật:

```powershell
dotnet run --project tests/ChatbotChecks/ChatbotChecks.csproj -c Release
```

## File cơ sở dữ liệu

`database/ktxai-sqlserver.sql` là file chạy trong **SQL Server Management Studio**, chứa 11 bảng, khóa ngoại, chỉ mục và dữ liệu chuyển từ SQLite; giữ ID, mật khẩu dạng hash và nội dung tiếng Việt.

1. Mở SSMS, kết nối server `localhost` bằng Windows Authentication.
2. Chọn File → Open → File, mở `database/ktxai-sqlserver.sql`.
3. Nhấn Execute (F5), rồi Refresh mục Databases để thấy **KtxAI**.
4. Chạy backend theo hướng dẫn trên.

Script chỉ nhập vào database chưa có bảng, không dùng để gộp dữ liệu hay chạy lại trên database đang sử dụng. Trên máy đã nhập thành công thì không cần chạy lại. File SQL là bản chụp tại thời điểm xuất, không tự cập nhật theo ứng dụng.

File `database/ktxai.sql` là bản SQLite cũ, **không chạy trong SSMS**.

## Chức năng

Chi tiết quyền của từng vai trò và cách kiểm tra: [PHAN_QUYEN.md](PHAN_QUYEN.md).

Sinh viên có thể hỏi chatbot (có hoặc không dấu):

- “Hồ sơ của tôi”, “Tôi đang ở phòng nào?”, “Hợp đồng của tôi thế nào?”
- “Tôi còn nợ bao nhiêu tiền?”, “Hóa đơn của tôi”, “Phản ánh của tôi xử lý chưa?”
- “Làm sao đăng ký ở KTX?”, “Tôi muốn chuyển phòng”, “Thủ tục trả phòng”
- “Hướng dẫn thanh toán”, “Cách gửi phản ánh”, “Tôi muốn sửa thông tin”

Dữ liệu cá nhân được tra cứu tại backend theo tài khoản đăng nhập, không gửi sang Gemini. Chatbot chỉ đọc và hướng dẫn, không tự tạo giao dịch hay thay đổi dữ liệu. Khi thiếu hồ sơ, hóa đơn, hợp đồng hoặc phản ánh, câu trả lời nêu rõ chưa có dữ liệu. Gia hạn và thanh toán trực tuyến chưa được hệ thống hỗ trợ. Trang chat có nút gợi ý cho các câu hỏi phổ biến.

Chatbot cũng hỗ trợ các cách hỏi tổng số phòng, giường, tòa nhà, tầng và số chỗ còn trống. Với câu hỏi chưa khớp từ khóa, Gemini nhận ngữ cảnh công khai gồm số liệu tổng hợp, nội quy và hướng dẫn chức năng để diễn giải câu hỏi tự nhiên; không có quyền chạy SQL hoặc thực hiện thao tác. Giá phòng, lịch, liên hệ hoặc chính sách chưa được cung cấp sẽ không được tự suy đoán.

Kiểm tra nghiệp vụ chatbot khi backend chạy với dữ liệu mẫu:

```powershell
powershell -ExecutionPolicy Bypass -File tests/Authorization/check-student-chat.ps1
```

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

Khi publish, frontend được chép vào `publish/wwwroot/`, prompt AI vào `publish/Prompts/`. Chạy từ thư mục publish để ứng dụng đọc đúng cấu hình và prompt; dữ liệu nằm trên SQL Server. Cấu hình bí mật qua biến môi trường hoặc `.env` riêng tại nơi triển khai. Thư mục build, cache IDE và file SQLite cục bộ được liệt kê trong `.gitignore`.
