requireLogin();

const user = getCurrentUser();
if (user) {
  document.getElementById("userInfo").innerText = `${user.hoTen} (${user.vaiTro})`;
}
const isSinhVien = user?.vaiTro === "SinhVien";
const canCreateInvoice = user?.vaiTro === "QuanLy" || user?.vaiTro === "KeToan";

const state = {
  page: 1,
  pageSize: 10,
};

function renderPagination(totalCount, page) {
  const totalPages = Math.max(1, Math.ceil(totalCount / state.pageSize));
  const container = document.getElementById("pagination");
  container.innerHTML = `
    <div class="small text-muted">Tổng số: ${totalCount} khoản phí</div>
    <div class="btn-group">
      <button class="btn btn-sm btn-outline-secondary" id="btnPrev" ${page <= 1 ? "disabled" : ""}>&laquo; Trước</button>
      <button class="btn btn-sm btn-outline-secondary disabled">Trang ${page}/${totalPages}</button>
      <button class="btn btn-sm btn-outline-secondary" id="btnNext" ${page >= totalPages ? "disabled" : ""}>Sau &raquo;</button>
    </div>
  `;

  document.getElementById("btnPrev")?.addEventListener("click", () => {
    if (state.page > 1) {
      state.page -= 1;
      loadPhi();
    }
  });

  document.getElementById("btnNext")?.addEventListener("click", () => {
    if (state.page < totalPages) {
      state.page += 1;
      loadPhi();
    }
  });
}

function renderRows(items) {
  const tbody = document.getElementById("phiTableBody");
  if (!items || items.length === 0) {
    tbody.innerHTML = '<tr><td colspan="7" class="text-center text-muted py-3">Không có khoản phí nào phù hợp.</td></tr>';
    return;
  }

  tbody.innerHTML = items.map((item) => {
    const statusClass = item.trangThaiThanhToan === "DaThu" ? "bg-success" : item.trangThaiThanhToan === "QuaHan" ? "bg-danger" : "bg-warning text-dark";
    const statusText = item.trangThaiThanhToan === "DaThu" ? "Đã thu" : item.trangThaiThanhToan === "QuaHan" ? "Quá hạn" : "Chưa thu";
    const isDisabled = item.trangThaiThanhToan === "DaThu";

    return `
      <tr>
        <td>
          <div class="fw-semibold">${item.tenSinhVien || "—"}</div>
          <small class="text-muted">${item.maSv || "—"}</small>
        </td>
        <td>${item.soPhong || "—"}</td>
        <td>${item.loaiPhi || "—"}</td>
        <td>${formatCurrency(item.soTien)}</td>
        <td>${formatDate(item.hanThanhToan)}</td>
        <td><span class="badge ${statusClass}">${statusText}</span></td>
        <td class="text-end">
          ${!isSinhVien ? `<button class="btn btn-sm btn-success btn-thanh-toan" data-id="${item.id}" ${isDisabled ? "disabled" : ""}>Đã thu</button>` : ""}
          ${user?.vaiTro === "QuanLy" ? `<button class="btn btn-sm btn-outline-danger btn-delete-phi ms-1" data-id="${item.id}">Xóa</button>` : ""}
        </td>
      </tr>
    `;
  }).join("");

  document.querySelectorAll(".btn-thanh-toan").forEach((btn) => {
    btn.addEventListener("click", async () => {
      const id = Number(btn.getAttribute("data-id"));
      try {
        await apiFetch(`/phi/${id}/thanh-toan`, { method: "PUT" });
        showToast("Đã ghi nhận thanh toán.", "success");
        loadPhi();
      } catch {
        // toast đã hiển thị lỗi
      }
    });
  });

  document.querySelectorAll(".btn-delete-phi").forEach((btn) => {
    btn.addEventListener("click", async () => {
      const id = Number(btn.getAttribute("data-id"));
      if (!confirm("Bạn có chắc muốn xóa khoản phí này?")) return;
      try {
        await apiFetch(`/phi/${id}`, { method: "DELETE" });
        showToast("Đã xóa khoản phí.", "success");
        loadPhi();
      } catch {
        // toast đã hiển thị lỗi
      }
    });
  });
}

async function loadPhi() {
  const keyword = document.getElementById("fKeyword").value.trim();
  const trangThai = document.getElementById("fTrangThai").value;
  const loaiPhi = document.getElementById("fLoaiPhi").value;

  const params = new URLSearchParams();
  params.set("page", state.page);
  params.set("pageSize", state.pageSize);
  if (trangThai) params.set("trangThai", trangThai);

  try {
    const endpoint = isSinhVien ? `/phi/cua-toi?${params.toString()}` : `/phi?${params.toString()}`;
    const res = await apiFetch(endpoint);
    let items = res.data?.items || [];

    if (keyword) {
      items = items.filter((item) => {
        const haystack = `${item.tenSinhVien || ""} ${item.maSv || ""} ${item.soPhong || ""}`.toLowerCase();
        return haystack.includes(keyword.toLowerCase());
      });
    }

    if (loaiPhi) {
      items = items.filter((item) => item.loaiPhi === loaiPhi);
    }

    renderRows(items);
    renderPagination(res.data?.totalCount || items.length, state.page);
  } catch {
    // toast đã hiển thị lỗi
  }
}

async function openInvoiceModal() {
  const select = document.getElementById("invoiceContract");
  const errorBox = document.getElementById("invoiceFormError");
  errorBox.innerText = "";
  select.innerHTML = '<option value="">Đang tải hợp đồng...</option>';
  try {
    const res = await apiFetch("/hopdong?trangThai=DangHieuLuc&page=1&pageSize=100");
    const contracts = res.data?.items || [];
    select.innerHTML = '<option value="">-- Chọn sinh viên --</option>';
    contracts.forEach((contract) => {
      const option = document.createElement("option");
      option.value = contract.id;
      option.textContent = `${contract.maSv} - ${contract.tenSinhVien} - Phòng ${contract.soPhong || "—"}`;
      select.appendChild(option);
    });
    if (contracts.length === 0) select.innerHTML = '<option value="">Chưa có sinh viên đang ở</option>';
    document.getElementById("invoiceDueDate").value = new Date(Date.now() + 7 * 86400000).toISOString().substring(0, 10);
    new bootstrap.Modal(document.getElementById("invoiceModal")).show();
  } catch {
    errorBox.innerText = "Không thể tải danh sách sinh viên đang ở.";
  }
}

document.getElementById("btnCreateInvoice").addEventListener("click", openInvoiceModal);
document.getElementById("btnSaveInvoice").addEventListener("click", async () => {
  const errorBox = document.getElementById("invoiceFormError");
  errorBox.innerText = "";
  const hopDongId = Number(document.getElementById("invoiceContract").value);
  const soTien = Number(document.getElementById("invoiceAmount").value);
  const hanThanhToan = document.getElementById("invoiceDueDate").value;
  const loaiPhi = document.getElementById("invoiceType").value;

  if (!hopDongId || !soTien || !hanThanhToan) {
    errorBox.innerText = "Vui lòng chọn sinh viên, nhập số tiền và hạn thanh toán.";
    return;
  }
  try {
    await apiFetch("/phi", { method: "POST", body: { hopDongId, loaiPhi, soTien, hanThanhToan } });
    showToast("Đã tạo hóa đơn tiền phòng.", "success");
    bootstrap.Modal.getInstance(document.getElementById("invoiceModal"))?.hide();
    state.page = 1;
    loadPhi();
  } catch (err) {
    errorBox.innerText = err.message || "Không thể tạo hóa đơn.";
  }
});

if (isSinhVien) {
  document.getElementById("pageTitle").innerText = "Hóa đơn của tôi";
  document.getElementById("btnCreateInvoice").style.display = "none";
  document.getElementById("fKeyword").closest(".col-md-3").style.display = "none";
  document.getElementById("fLoaiPhi").closest(".col-md-3").style.display = "none";
}
if (!canCreateInvoice) {
  document.getElementById("btnCreateInvoice").hidden = true;
  document.getElementById("invoiceModal").remove();
}

document.getElementById("btnRefresh").addEventListener("click", () => {
  state.page = 1;
  loadPhi();
});

document.getElementById("btnSearch").addEventListener("click", () => {
  state.page = 1;
  loadPhi();
});

loadPhi();
