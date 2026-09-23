requireLogin();

const user = getCurrentUser();
if (user) {
  document.getElementById("userInfo").innerText = `${user.hoTen} (${user.vaiTro})`;
}

async function loadDashboard() {
  try {
    const res = await apiFetch("/thongke/tong-quan");
    const d = res.data;

    document.getElementById("statLapDay").innerText = `${d.tiLeLapDay}%`;
    document.getElementById("statGiuongChiTiet").innerText = `${d.soGiuongDangO}/${d.tongSoGiuong} giường đang ở`;
    document.getElementById("statTongPhong").innerText = d.tongSoPhong;
    document.getElementById("statTongSinhVien").innerText = d.tongSoSinhVien;
    document.getElementById("statViPham").innerText = d.tongSoViPham;

    document.getElementById("statPhiChuaThu").innerText = formatCurrency(d.tongPhiChuaThu);
    document.getElementById("statPhiQuaHan").innerText = formatCurrency(d.tongPhiQuaHan);
    document.getElementById("statPhiDaThu").innerText = formatCurrency(d.tongPhiDaThu);

    document.getElementById("paMoi").innerText = d.soPhanAnhMoi;
    document.getElementById("paDangXuLy").innerText = d.soPhanAnhDangXuLy;
    document.getElementById("paDaXuLy").innerText = d.soPhanAnhDaXuLy;
  } catch (err) {
    // Lỗi đã được apiFetch hiển thị qua toast; nếu là lỗi quyền truy cập (sinh viên
    // không được xem dashboard) thì điều hướng về trang phù hợp.
    if (err.message === "Forbidden") {
      window.location.href = "/phananh.html";
    }
  }

  try {
    const res2 = await apiFetch("/thongke/phananh-theo-nhom");
    const tbody = document.querySelector("#tblNhomVanDe tbody");
    tbody.innerHTML = "";
    res2.data.forEach((row) => {
      const tr = document.createElement("tr");
      tr.innerHTML = `<td>${row.nhomVanDe}</td><td class="text-end">${row.soLuong}</td>`;
      tbody.appendChild(tr);
    });
  } catch { /* đã có toast lỗi */ }
}

loadDashboard();
