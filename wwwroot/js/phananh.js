requireLogin();

const user = getCurrentUser();
if (user) {
  document.getElementById("userInfo").innerText = `${user.hoTen} (${user.vaiTro})`;
}

const isSinhVien = user && user.vaiTro === "SinhVien";
const isQuanLy = user && user.vaiTro === "QuanLy";

let currentPage = 1;
const pageSize = 10;
let mySinhVienId = null;

const trangThaiLabel = { Moi: "Mới", DangXuLy: "Đang xử lý", DaXuLy: "Đã xử lý" };

if (isSinhVien) {
  document.getElementById("btnAdd").style.display = "";
  document.querySelectorAll(".col-sv").forEach((el) => (el.style.display = "none"));
} else {
  document.getElementById("btnAdd").style.display = "none";
}

async function loadMySinhVienId() {
  if (!isSinhVien) return;
  try {
    const res = await apiFetch("/sinhvien/me");
    mySinhVienId = res.data.id;
  } catch {
    showToast("Không tìm thấy hồ sơ sinh viên gắn với tài khoản này.", "error");
  }
}

function renderPagination(totalCount, page) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const container = document.getElementById("pagination");
  container.innerHTML = `
    <div class="small text-muted">Tổng số: ${totalCount} phản ánh</div>
    <div class="btn-group">
      <button class="btn btn-sm btn-outline-secondary" id="btnPrev" ${page <= 1 ? "disabled" : ""}>&laquo; Trước</button>
      <button class="btn btn-sm btn-outline-secondary disabled">Trang ${page}/${totalPages}</button>
      <button class="btn btn-sm btn-outline-secondary" id="btnNext" ${page >= totalPages ? "disabled" : ""}>Sau &raquo;</button>
    </div>
  `;
  document.getElementById("btnPrev")?.addEventListener("click", () => {
    if (currentPage > 1) { currentPage--; loadPhanAnhs(currentPage); }
  });
  document.getElementById("btnNext")?.addEventListener("click", () => {
    if (currentPage < totalPages) { currentPage++; loadPhanAnhs(currentPage); }
  });
}

async function loadPhanAnhs(page = 1) {
  currentPage = page;
  const keyword = document.getElementById("fKeyword").value.trim();
  const nhomVanDe = document.getElementById("fNhomVanDe").value.trim();
  const trangThai = document.getElementById("fTrangThai").value;

  const params = new URLSearchParams();
  if (keyword) params.set("keyword", keyword);
  if (nhomVanDe) params.set("nhomVanDe", nhomVanDe);
  if (trangThai) params.set("trangThai", trangThai);
  params.set("page", page);
  params.set("pageSize", pageSize);

  try {
    const res = await apiFetch(`/phananh?${params.toString()}`);
    const data = res.data;
    const tbody = document.getElementById("paTableBody");
    tbody.innerHTML = "";

    if (!data.items || data.items.length === 0) {
      tbody.innerHTML = `<tr><td colspan="7" class="text-center text-muted py-3">Không có phản ánh phù hợp.</td></tr>`;
    } else {
      data.items.forEach((pa) => {
        const tr = document.createElement("tr");
        tr.innerHTML = `
          <td>${formatDate(pa.ngayTao)}</td>
          <td class="col-sv">${pa.tenSinhVien ?? ""}</td>
          <td>${pa.soPhong ?? "—"}</td>
          <td style="max-width:280px;">${escapeHtml(pa.noiDung)}</td>
          <td>${pa.nhomVanDe ?? '<span class="text-muted">Chưa phân loại</span>'}</td>
          <td><span class="badge ${badgeClass(pa.trangThai)}">${trangThaiLabel[pa.trangThai] ?? pa.trangThai}</span></td>
          <td class="text-end">
            ${isQuanLy ? `
              <button class="btn btn-sm btn-outline-primary btn-status" data-id="${pa.id}" data-trangthai="${pa.trangThai}" data-nhom="${pa.nhomVanDe ?? ""}">Cập nhật</button>
              <button class="btn btn-sm btn-outline-danger btn-delete" data-id="${pa.id}">Xóa</button>
            ` : ""}
          </td>
        `;
        tbody.appendChild(tr);
      });
    }

    if (isSinhVien) {
      document.querySelectorAll(".col-sv").forEach((el) => (el.style.display = "none"));
    }

    renderPagination(data.totalCount, data.page);
    bindRowActions();
  } catch { /* toast đã hiển thị lỗi */ }
}

function escapeHtml(text) {
  const div = document.createElement("div");
  div.innerText = text ?? "";
  return div.innerHTML;
}

function bindRowActions() {
  document.querySelectorAll(".btn-status").forEach((btn) => {
    btn.addEventListener("click", () => {
      document.getElementById("statusPaId").value = btn.getAttribute("data-id");
      document.getElementById("statusTrangThai").value = btn.getAttribute("data-trangthai");
      document.getElementById("statusNhomVanDe").value = btn.getAttribute("data-nhom") || "";
      document.getElementById("statusFormError").innerText = "";
      new bootstrap.Modal(document.getElementById("statusModal")).show();
    });
  });

  document.querySelectorAll(".btn-delete").forEach((btn) => {
    btn.addEventListener("click", async () => {
      const id = btn.getAttribute("data-id");
      if (!confirm("Bạn có chắc muốn xóa phản ánh này?")) return;
      try {
        await apiFetch(`/phananh/${id}`, { method: "DELETE" });
        showToast("Đã xóa phản ánh.", "success");
        loadPhanAnhs(currentPage);
      } catch { /* toast đã hiển thị lỗi */ }
    });
  });
}

document.getElementById("btnSearch").addEventListener("click", () => loadPhanAnhs(1));

document.getElementById("btnSendPa").addEventListener("click", async () => {
  const errorBox = document.getElementById("paFormError");
  errorBox.innerText = "";
  const noiDung = document.getElementById("paNoiDung").value.trim();

  if (!noiDung) {
    errorBox.innerText = "Vui lòng nhập nội dung phản ánh.";
    return;
  }
  if (!mySinhVienId) {
    errorBox.innerText = "Không xác định được hồ sơ sinh viên của bạn. Vui lòng thử đăng nhập lại.";
    return;
  }

  try {
    await apiFetch("/phananh", {
      method: "POST",
      body: { sinhVienId: mySinhVienId, noiDung },
    });
    showToast("Đã gửi phản ánh.", "success");
    document.getElementById("paForm").reset();
    bootstrap.Modal.getInstance(document.getElementById("paModal"))?.hide();
    loadPhanAnhs(1);
  } catch (err) {
    errorBox.innerText = err.message && err.message !== "Forbidden" ? err.message : "Không thể gửi phản ánh.";
  }
});

document.getElementById("btnSaveStatus").addEventListener("click", async () => {
  const errorBox = document.getElementById("statusFormError");
  errorBox.innerText = "";
  const id = document.getElementById("statusPaId").value;
  const trangThai = document.getElementById("statusTrangThai").value;
  const nhomVanDe = document.getElementById("statusNhomVanDe").value.trim();

  try {
    await apiFetch(`/phananh/${id}/trang-thai`, {
      method: "PUT",
      body: { trangThai, nhomVanDe },
    });
    showToast("Đã cập nhật phản ánh.", "success");
    bootstrap.Modal.getInstance(document.getElementById("statusModal"))?.hide();
    loadPhanAnhs(currentPage);
  } catch (err) {
    errorBox.innerText = err.message && err.message !== "Forbidden" ? err.message : "Không thể cập nhật phản ánh.";
  }
});

(async function init() {
  await loadMySinhVienId();
  loadPhanAnhs(1);
})();
