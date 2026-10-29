// api.js — lớp gọi API dùng chung cho toàn bộ frontend.
// Xử lý gắn JWT token, đọc lỗi trả về từ ApiResponse<T> và hiển thị thông báo (tiêu chí KT2 #6, #8).

const API_BASE = "/api";

function getToken() {
  return localStorage.getItem("ktx_token");
}

function getCurrentUser() {
  const raw = localStorage.getItem("ktx_user");
  try { return raw ? JSON.parse(raw) : null; } catch { return null; }
}

const pageRoles = {
  "/dashboard.html": ["QuanLy", "KeToan"],
  "/sinhvien.html": ["QuanLy"],
  "/phong.html": ["QuanLy", "KeToan", "SinhVien"],
  "/phananh.html": ["QuanLy", "SinhVien"],
  "/phi.html": ["QuanLy", "KeToan", "SinhVien"],
  "/chatbot.html": ["QuanLy", "KeToan", "SinhVien"],
};

function homePage(role = getCurrentUser()?.vaiTro) {
  return role === "SinhVien" ? "/phi.html" : "/dashboard.html";
}

function requireLogin() {
  const role = getCurrentUser()?.vaiTro;
  if (!getToken() || !["QuanLy", "KeToan", "SinhVien"].includes(role)) {
    window.location.replace("/index.html");
    throw new Error("Login required");
  }
  if (pageRoles[window.location.pathname] && !pageRoles[window.location.pathname].includes(role)) {
    window.location.replace(homePage(role));
    throw new Error("Page forbidden");
  }
  document.querySelectorAll("a[href]").forEach(link => {
    const allowed = pageRoles[link.getAttribute("href")];
    if (allowed && !allowed.includes(role)) link.hidden = true;
    if (role === "SinhVien" && link.getAttribute("href") === "/phi.html")
      link.textContent = "Hóa đơn của tôi";
  });
  const labels = { QuanLy: "Quản lý", KeToan: "Kế toán", SinhVien: "Sinh viên" };
  const userInfo = document.getElementById("userInfo");
  if (userInfo) userInfo.title = `Vai trò: ${labels[role]}`;
}

function logout() {
  localStorage.removeItem("ktx_token");
  localStorage.removeItem("ktx_user");
  window.location.href = "/index.html";
}

// Hiển thị thông báo toast (thành công/lỗi) — phản hồi rõ ràng cho người dùng.
function showToast(message, type = "success") {
  let container = document.getElementById("toastContainer");
  if (!container) {
    container = document.createElement("div");
    container.id = "toastContainer";
    document.body.appendChild(container);
  }

  const el = document.createElement("div");
  el.className = `alert alert-${type === "error" ? "danger" : "success"} shadow-sm`;
  el.role = "alert";
  el.innerText = message;
  container.appendChild(el);

  setTimeout(() => el.remove(), 4000);
}

/**
 * Gọi API với JSON body, tự gắn Authorization header nếu có token.
 * Luôn trả về { success, message, data }. Khi lỗi mạng/hết hạn token,
 * tự điều hướng về trang đăng nhập (401) hoặc báo lỗi rõ ràng.
 */
async function apiFetch(path, { method = "GET", body = null, auth = true } = {}) {
  const headers = { "Content-Type": "application/json" };
  if (auth) {
    const token = getToken();
    if (token) headers["Authorization"] = `Bearer ${token}`;
  }

  let response;
  try {
    response = await fetch(`${API_BASE}${path}`, {
      method,
      headers,
      body: body ? JSON.stringify(body) : undefined,
    });
  } catch (networkErr) {
    showToast("Không thể kết nối tới server. Vui lòng kiểm tra kết nối mạng.", "error");
    throw networkErr;
  }

  let payload = null;
  try {
    payload = await response.json();
  } catch {
    // Một số response (ví dụ 204 No Content) không có body JSON — bỏ qua.
  }

  if (response.status === 401) {
    showToast("Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại.", "error");
    logout();
    throw new Error("Unauthorized");
  }

  if (response.status === 403) {
    showToast(payload?.message || "Bạn không có quyền thực hiện thao tác này.", "error");
    throw new Error("Forbidden");
  }

  if (!response.ok) {
    // payload?.message: định dạng lỗi tự viết trong code (ApiResponse<T>).
    // payload?.errors: định dạng lỗi validate mặc định của ASP.NET Core (ValidationProblemDetails),
    // ví dụ khi thiếu trường bắt buộc hoặc gửi sai kiểu dữ liệu — không có field "message" nên
    // phải tự ghép lại từ "errors" để người dùng thấy lý do cụ thể, thay vì chỉ thấy "mã 400".
    let msg = payload?.message;
    if (!msg && payload?.errors) {
      const chiTietLoi = Object.values(payload.errors).flat();
      if (chiTietLoi.length > 0) msg = chiTietLoi.join(" ");
    }
    if (!msg) msg = payload?.title || `Đã có lỗi xảy ra (mã ${response.status}).`;
    showToast(msg, "error");
    throw new Error(msg);
  }

  return payload;
}

function badgeClass(trangThai) {
  const map = {
    ConTrong: "badge-trong", Trong: "badge-trong",
    Day: "badge-day", DaO: "badge-day",
    KhoaTam: "badge-khoatam",
    Moi: "badge-moi", DangXuLy: "badge-dangxuly", DaXuLy: "badge-daxuly",
  };
  return map[trangThai] || "bg-secondary";
}

function formatCurrency(value) {
  return new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(value || 0);
}

function formatDate(value) {
  if (!value) return "—";
  return new Date(value).toLocaleDateString("vi-VN");
}
