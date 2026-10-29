document.getElementById("loginForm").addEventListener("submit", async (e) => {
  e.preventDefault();
  const email = document.getElementById("email").value.trim();
  const matKhau = document.getElementById("matKhau").value;
  const errorBox = document.getElementById("loginError");
  errorBox.innerText = "";

  try {
    const res = await apiFetch("/auth/login", {
      method: "POST",
      auth: false,
      body: { email, matKhau },
    });

    localStorage.setItem("ktx_token", res.data.token);
    localStorage.setItem("ktx_user", JSON.stringify(res.data));

    window.location.href = homePage(res.data.vaiTro);
  } catch (err) {
    errorBox.innerText = "Email hoặc mật khẩu không đúng. Vui lòng thử lại.";
  }
});

// Nếu đã đăng nhập rồi thì vào thẳng dashboard
if (getToken() && ["QuanLy", "KeToan", "SinhVien"].includes(getCurrentUser()?.vaiTro)) {
  window.location.href = homePage();
}
