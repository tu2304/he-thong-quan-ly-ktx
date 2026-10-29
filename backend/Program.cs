using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using KtxAI.Api.Common;
using KtxAI.Api.Data;
using KtxAI.Api.Middleware;
using KtxAI.Api.Services;

// Nạp biến môi trường từ file .env (nếu có) TRƯỚC khi tạo builder,
// để các giá trị như Jwt__Key, ConnectionStrings__DefaultConnection
// được ASP.NET Core Configuration đọc thấy ngay từ đầu.
DotNetEnv.Env.Load();

// Resolve paths before creating the host so static files work in source and publish.
var contentRoot = Directory.GetCurrentDirectory();
var frontendPath = Path.GetFullPath(Path.Combine(contentRoot, "..", "frontend"));
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = Directory.Exists(frontendPath) ? frontendPath : "wwwroot"
});
// ---------- Cấu hình DbContext (SQLite) ----------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=ktxai.db";
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

// ---------- JWT Authentication ----------
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    // Không chặn khởi động ở môi trường demo, nhưng cảnh báo rõ ràng.
    // Production PHẢI đặt Jwt__Key qua .env hoặc biến môi trường thật.
    jwtKey = "DEV-ONLY-CHANGE-ME-32-CHARACTERS-MIN";
    Console.WriteLine("[CẢNH BÁO] Chưa cấu hình Jwt:Key trong .env — đang dùng khóa demo, KHÔNG dùng cho production.");

    // Ghi lại vào Configuration để JwtService (sinh token lúc đăng nhập) dùng
    // CHÍNH XÁC cùng một khóa với phần xác thực token bên dưới — nếu không,
    // JwtService sẽ không đọc được Jwt:Key và làm API đăng nhập báo lỗi 500.
    builder.Configuration["Jwt:Key"] = jwtKey;
}
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "KtxAI.Api";

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtIssuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// ---------- CORS (cho phép frontend tách rời gọi API nếu cần) ----------
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
    });
});

// ---------- Services ----------
builder.Services.AddScoped<IJwtService, JwtService>();

// ---------- Chatbot AI (Google Gemini) — KT3 ----------
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));
builder.Services.AddHttpClient<IChatbotService, GeminiChatbotService>();

// Lỗi 400 "Đã có lỗi xảy ra" khi Thêm sinh viên / Cập nhật trạng thái phản ánh:
// mặc định System.Text.Json đọc/ghi enum (GioiTinh, TrangThaiPhanAnh, ...) dưới dạng SỐ
// (0,1,2...), nhưng frontend gửi lên CHUỖI ("Nam", "Moi", ...) từ các thẻ <select>.
// Khi model binding không parse được chuỗi thành enum, ASP.NET Core tự trả về 400
// (ValidationProblemDetails không có field "message" nên client hiện thông báo chung
// "Đã có lỗi xảy ra (mã 400)."). Thêm JsonStringEnumConverter để (de)serialize enum
// bằng tên chuỗi ở MỌI request/response — khớp đúng với dữ liệu mà frontend đang gửi.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "KTX AI - API quản lý ký túc xá",
        Version = "v1",
        Description = "API cho hệ thống quản lý ký túc xá có tích hợp AI (KT2)"
    });

    var jwtScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Description = "Nhập token JWT (không cần gõ chữ 'Bearer ')"
    };
    c.AddSecurityDefinition("Bearer", jwtScheme);
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { new Microsoft.OpenApi.Models.OpenApiSecurityScheme { Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }, new List<string>() }
    });
});

var app = builder.Build();

// ---------- Seed dữ liệu mẫu khi khởi động ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    SeedData.Initialize(db);
}

// ---------- Middleware pipeline ----------
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("DefaultCors");

app.UseDefaultFiles(); // cho phép truy cập / -> wwwroot/index.html
app.UseStaticFiles();  // phục vụ CSS/JS/HTML tĩnh trong wwwroot

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
