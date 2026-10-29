requireLogin();

const user = getCurrentUser();
if (user) {
  document.getElementById("userInfo").innerText = `${user.hoTen} (${user.vaiTro})`;
}

const isQuanLy = user && user.vaiTro === "QuanLy";
if (!isQuanLy) {
  document.getElementById("btnAdd").style.display = "none";
}

let currentPage = 1;
const pageSize = 10;
let currentPhongId = null; // phòng đang mở trong modal giường
let currentGiuongId = null;
let currentGiuongLabel = "";
let currentAssignRoomId = null;

const trangThaiLabel = {
  ConTrong: "Còn trống",
  Day: "Đầy",
  KhoaTam: "Khóa tạm",
  Trong: "Trống",
  DaO: "Đã ở",
};

async function loadToaOptions() {
  try {
    const res = await apiFetch("/toanha");
    const selects = [document.getElementById("fToa"), document.getElementById("mToa")];
    selects.forEach((sel) => {
      // Giữ option đầu tiên (Tất cả / placeholder), xóa các option cũ còn lại
      while (sel.options.length > (sel.id === "fToa" ? 1 : 0)) sel.remove(sel.options.length - 1);
      res.data.forEach((t) => {
        const opt = document.createElement("option");
        opt.value = t.id;
        opt.innerText = t.tenToa;
        sel.appendChild(opt);
      });
    });
    if (document.getElementById("mToa").options.length > 0) {
      await loadTangOptions(document.getElementById("mToa").value);
    }
  } catch { /* toast đã hiển thị lỗi */ }
}

async function loadTangOptions(toaId) {
  const sel = document.getElementById("mTang");
  sel.innerHTML = "";
  if (!toaId) return;
  try {
    const res = await apiFetch(`/tang?toaId=${toaId}`);
    res.data.forEach((t) => {
      const opt = document.createElement("option");
      opt.value = t.id;
      opt.innerText = `Tầng ${t.soTang}`;
      sel.appendChild(opt);
    });
  } catch { /* toast đã hiển thị lỗi */ }
}

document.getElementById("mToa").addEventListener("change", (e) => loadTangOptions(e.target.value));

function setModalMode(isEdit) {
  document.getElementById("groupCreateOnly").style.display = isEdit ? "none" : "";
  document.getElementById("groupTrangThaiOnly").style.display = isEdit ? "" : "none";
  document.getElementById("phongModalTitle").innerText = isEdit ? "Sửa thông tin phòng" : "Thêm phòng";
}

function resetPhongForm() {
  document.getElementById("phongForm").reset();
  document.getElementById("phongId").value = "";
  document.getElementById("phongFormError").innerText = "";
}

document.getElementById("btnAdd").addEventListener("click", () => {
  resetPhongForm();
  setModalMode(false);
});

function renderPagination(totalCount, page) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const container = document.getElementById("pagination");
  container.innerHTML = `
    <div class="small text-muted">Tổng số: ${totalCount} phòng</div>
    <div class="btn-group">
      <button class="btn btn-sm btn-outline-secondary" id="btnPrev" ${page <= 1 ? "disabled" : ""}>&laquo; Trước</button>
      <button class="btn btn-sm btn-outline-secondary disabled">Trang ${page}/${totalPages}</button>
      <button class="btn btn-sm btn-outline-secondary" id="btnNext" ${page >= totalPages ? "disabled" : ""}>Sau &raquo;</button>
    </div>
  `;
  document.getElementById("btnPrev")?.addEventListener("click", () => {
    if (currentPage > 1) { currentPage--; loadPhongs(currentPage); }
  });
  document.getElementById("btnNext")?.addEventListener("click", () => {
    if (currentPage < totalPages) { currentPage++; loadPhongs(currentPage); }
  });
}

async function loadPhongs(page = 1) {
  currentPage = page;
  const keyword = document.getElementById("fKeyword").value.trim();
  const toaId = document.getElementById("fToa").value;
  const trangThai = document.getElementById("fTrangThai").value;
  const sortBy = document.getElementById("fSortBy").value;

  const params = new URLSearchParams();
  if (keyword) params.set("keyword", keyword);
  if (toaId) params.set("toaId", toaId);
  if (trangThai) params.set("trangThai", trangThai);
  params.set("sortBy", sortBy);
  params.set("page", page);
  params.set("pageSize", pageSize);

  try {
    const res = await apiFetch(`/phong?${params.toString()}`);
    const data = res.data;
    const tbody = document.getElementById("phongTableBody");
    tbody.innerHTML = "";

    if (!data.items || data.items.length === 0) {
      tbody.innerHTML = `<tr><td colspan="8" class="text-center text-muted py-3">Không có phòng phù hợp.</td></tr>`;
    } else {
      data.items.forEach((p) => {
        const tr = document.createElement("tr");
        tr.innerHTML = `
          <td>${p.soPhong}</td>
          <td>${p.tenToa}</td>
          <td>${p.soTang}</td>
          <td>${p.loaiPhong ?? ""}</td>
          <td>${p.sucChua}</td>
          <td>${p.soGiuongTrong}/${p.tongSoGiuong}</td>
          <td><span class="badge ${badgeClass(p.trangThai)}">${trangThaiLabel[p.trangThai] ?? p.trangThai}</span></td>
          <td class="text-end">
            <button class="btn btn-sm btn-outline-secondary btn-giuong" data-id="${p.id}" data-so="${p.soPhong}">Giường</button>
            ${isQuanLy ? `<button class="btn btn-sm btn-outline-success btn-assign-room" data-id="${p.id}" data-so="${p.soPhong}"><i class="bi bi-person-plus me-1"></i>Thêm SV</button>` : ""}
            ${isQuanLy ? `<button class="btn btn-sm btn-outline-primary btn-edit" data-id="${p.id}">Sửa</button>
            <button class="btn btn-sm btn-outline-danger btn-delete" data-id="${p.id}">Xóa</button>` : ""}
          </td>
        `;
        tbody.appendChild(tr);
      });
    }

    renderPagination(data.totalCount, data.page);
    bindRowActions();
  } catch { /* toast đã hiển thị lỗi */ }
}

function bindRowActions() {
  document.querySelectorAll(".btn-assign-room").forEach((btn) => {
    btn.addEventListener("click", () => openAssignRoomModal(btn.getAttribute("data-id"), btn.getAttribute("data-so")));
  });

  document.querySelectorAll(".btn-edit").forEach((btn) => {
    btn.addEventListener("click", async () => {
      const id = btn.getAttribute("data-id");
      try {
        const res = await apiFetch(`/phong/${id}`);
        const p = res.data;
        resetPhongForm();
        setModalMode(true);
        document.getElementById("phongId").value = p.id;
        document.getElementById("mSoPhong").value = p.soPhong;
        document.getElementById("mLoaiPhong").value = p.loaiPhong ?? "";
        document.getElementById("mSucChua").value = p.sucChua;
        document.getElementById("mTrangThai").value = p.trangThai;
        new bootstrap.Modal(document.getElementById("phongModal")).show();
      } catch { /* toast đã hiển thị lỗi */ }
    });
  });

  document.querySelectorAll(".btn-delete").forEach((btn) => {
    btn.addEventListener("click", async () => {
      const id = btn.getAttribute("data-id");
      if (!confirm("Bạn có chắc muốn xóa phòng này?")) return;
      try {
        await apiFetch(`/phong/${id}`, { method: "DELETE" });
        showToast("Đã xóa phòng.", "success");
        loadPhongs(currentPage);
      } catch { /* toast đã hiển thị lỗi (VD: còn sinh viên ở) */ }
    });
  });

  document.querySelectorAll(".btn-giuong").forEach((btn) => {
    btn.addEventListener("click", () => {
      currentPhongId = btn.getAttribute("data-id");
      document.getElementById("giuongPhongTen").innerText = btn.getAttribute("data-so");
      document.getElementById("giuongFormError").innerText = "";
      document.getElementById("mSoGiuongMoi").value = "";
      loadGiuongList();
      new bootstrap.Modal(document.getElementById("giuongModal")).show();
    });
  });
}

async function loadGiuongList() {
  const container = document.getElementById("giuongList");
  container.innerHTML = '<div class="text-muted small">Đang tải...</div>';
  try {
    const res = await apiFetch(`/giuong?phongId=${currentPhongId}`);
    if (!res.data || res.data.length === 0) {
      container.innerHTML = '<div class="text-muted small">Phòng chưa có giường nào.</div>';
      return;
    }
    container.innerHTML = "";
    const table = document.createElement("table");
    table.className = "table table-sm";
    table.innerHTML = `<thead><tr><th>Số giường</th><th>Trạng thái</th><th></th></tr></thead><tbody></tbody>`;
    const tbody = table.querySelector("tbody");
    res.data.forEach((g) => {
      const tr = document.createElement("tr");
      tr.innerHTML = `
        <td>${g.soGiuong}</td>
        <td><span class="badge ${badgeClass(g.trangThai)}">${trangThaiLabel[g.trangThai] ?? g.trangThai}</span></td>
        <td class="text-end">
          ${isQuanLy && g.trangThai === "Trong" ? `<button class="btn btn-sm btn-outline-success btn-add-student" data-id="${g.id}" data-label="${g.soPhong} - ${g.soGiuong}"><i class="bi bi-person-plus me-1"></i>Thêm sinh viên</button>` : ""}
          ${isQuanLy ? `<button class="btn btn-sm btn-outline-danger btn-del-giuong" data-id="${g.id}">Xóa</button>` : ""}
        </td>
      `;
      tbody.appendChild(tr);
    });
    container.appendChild(table);

    document.querySelectorAll(".btn-add-student").forEach((btn) => {
      btn.addEventListener("click", () => openAssignBedModal(btn.getAttribute("data-id"), btn.getAttribute("data-label")));
    });

    document.querySelectorAll(".btn-del-giuong").forEach((btn) => {
      btn.addEventListener("click", async () => {
        if (!confirm("Xóa giường này?")) return;
        try {
          await apiFetch(`/giuong/${btn.getAttribute("data-id")}`, { method: "DELETE" });
          showToast("Đã xóa giường.", "success");
          loadGiuongList();
          loadPhongs(currentPage);
        } catch { /* toast đã hiển thị lỗi */ }
      });
    });
  } catch {
    container.innerHTML = '<div class="text-danger small">Không thể tải danh sách giường.</div>';
  }
}

async function openAssignBedModal(giuongId, label) {
  currentGiuongId = Number(giuongId);
  currentAssignRoomId = null;
  currentGiuongLabel = label;
  document.getElementById("assignBedName").value = label;
  document.getElementById("assignBedError").innerText = "";
  document.getElementById("assignRoomBedSelect").innerHTML = `<option value="${currentGiuongId}">${label}</option>`;

  const select = document.getElementById("assignStudentSelect");
  select.innerHTML = '<option value="">Đang tải danh sách sinh viên...</option>';

  try {
    const res = await apiFetch("/sinhvien?page=1&pageSize=100&sortBy=hoten");
    const students = (res.data?.items || []).filter((sv) => !sv.phongHienTai);
    select.innerHTML = '<option value="">-- Chọn sinh viên --</option>';

    students.forEach((sv) => {
      const option = document.createElement("option");
      option.value = sv.id;
      option.textContent = `${sv.maSv} - ${sv.hoTen}${sv.lop ? ` (${sv.lop})` : ""}`;
      select.appendChild(option);
    });

    if (students.length === 0) {
      select.innerHTML = '<option value="">Không còn sinh viên chưa có phòng</option>';
    }

    new bootstrap.Modal(document.getElementById("assignBedModal")).show();
  } catch {
    select.innerHTML = '<option value="">Không thể tải danh sách sinh viên</option>';
  }
}

async function openAssignRoomModal(phongId, soPhong) {
  currentAssignRoomId = Number(phongId);
  currentGiuongId = null;
  currentGiuongLabel = soPhong;
  document.getElementById("assignBedName").value = `Phòng ${soPhong}`;
  document.getElementById("assignBedError").innerText = "";

  const bedSelect = document.getElementById("assignRoomBedSelect");
  const studentSelect = document.getElementById("assignStudentSelect");
  bedSelect.innerHTML = '<option value="">Đang tải giường trống...</option>';
  studentSelect.innerHTML = '<option value="">Đang tải sinh viên...</option>';

  try {
    const [bedsRes, studentsRes] = await Promise.all([
      apiFetch(`/giuong?phongId=${phongId}&trangThai=Trong`),
      apiFetch("/sinhvien?page=1&pageSize=100&sortBy=hoten"),
    ]);
    const beds = bedsRes.data || [];
    const students = (studentsRes.data?.items || []).filter((sv) => !sv.phongHienTai);

    bedSelect.innerHTML = '<option value="">-- Chọn giường trống --</option>';
    beds.forEach((bed) => {
      const option = document.createElement("option");
      option.value = bed.id;
      option.textContent = `Giường ${bed.soGiuong}`;
      bedSelect.appendChild(option);
    });

    studentSelect.innerHTML = '<option value="">-- Chọn sinh viên --</option>';
    students.forEach((sv) => {
      const option = document.createElement("option");
      option.value = sv.id;
      option.textContent = `${sv.maSv} - ${sv.hoTen}${sv.lop ? ` (${sv.lop})` : ""}`;
      studentSelect.appendChild(option);
    });

    if (beds.length === 0) {
      bedSelect.innerHTML = '<option value="">Phòng chưa có giường trống - hãy thêm giường trước</option>';
    }
    if (students.length === 0) {
      studentSelect.innerHTML = '<option value="">Không còn sinh viên chưa có phòng</option>';
    }
    new bootstrap.Modal(document.getElementById("assignBedModal")).show();
  } catch {
    bedSelect.innerHTML = '<option value="">Không thể tải giường trống</option>';
    studentSelect.innerHTML = '<option value="">Không thể tải sinh viên</option>';
  }
}

document.getElementById("btnAssignBed")?.addEventListener("click", async () => {
  const errorBox = document.getElementById("assignBedError");
  errorBox.innerText = "";
  const selectedBedId = Number(document.getElementById("assignRoomBedSelect").value);
  const sinhVienId = Number(document.getElementById("assignStudentSelect").value);

  if (!selectedBedId || !sinhVienId) {
    errorBox.innerText = "Vui lòng chọn giường trống và sinh viên cần thêm vào phòng.";
    return;
  }

  try {
    await apiFetch("/hopdong/xep-phong", {
      method: "POST",
      body: { sinhVienId, giuongId: selectedBedId },
    });

    showToast(`Đã thêm sinh viên vào giường ${currentGiuongLabel}.`, "success");
    bootstrap.Modal.getInstance(document.getElementById("assignBedModal"))?.hide();
    loadGiuongList();
    loadPhongs(currentPage);
  } catch (err) {
    errorBox.innerText = err.message && err.message !== "Forbidden"
      ? err.message
      : "Không thể thêm sinh viên vào giường.";
  }
});

document.getElementById("btnAddGiuong")?.addEventListener("click", async () => {
  const errorBox = document.getElementById("giuongFormError");
  errorBox.innerText = "";
  const soGiuong = document.getElementById("mSoGiuongMoi").value.trim();
  if (!soGiuong) {
    errorBox.innerText = "Vui lòng nhập số giường.";
    return;
  }
  try {
    await apiFetch("/giuong", { method: "POST", body: { phongId: Number(currentPhongId), soGiuong } });
    showToast("Đã thêm giường.", "success");
    document.getElementById("mSoGiuongMoi").value = "";
    loadGiuongList();
    loadPhongs(currentPage);
  } catch (err) {
    errorBox.innerText = err.message && err.message !== "Forbidden" ? err.message : "Không thể thêm giường.";
  }
});

document.getElementById("btnSearch").addEventListener("click", () => loadPhongs(1));

document.getElementById("btnSavePhong").addEventListener("click", async () => {
  const errorBox = document.getElementById("phongFormError");
  errorBox.innerText = "";

  const id = document.getElementById("phongId").value;
  const isEdit = !!id;
  const soPhong = document.getElementById("mSoPhong").value.trim();
  const loaiPhong = document.getElementById("mLoaiPhong").value.trim();
  const sucChua = Number(document.getElementById("mSucChua").value);

  if (!soPhong || !sucChua) {
    errorBox.innerText = "Vui lòng nhập đầy đủ số phòng và sức chứa.";
    return;
  }

  try {
    if (isEdit) {
      const trangThai = document.getElementById("mTrangThai").value;
      await apiFetch(`/phong/${id}`, {
        method: "PUT",
        body: { soPhong, loaiPhong, sucChua, trangThai },
      });
      showToast("Đã cập nhật phòng.", "success");
    } else {
      const tangId = Number(document.getElementById("mTang").value);
      if (!tangId) {
        errorBox.innerText = "Vui lòng chọn tòa nhà và tầng.";
        return;
      }
      await apiFetch("/phong", {
        method: "POST",
        body: { tangId, soPhong, loaiPhong, sucChua },
      });
      showToast("Đã thêm phòng mới.", "success");
    }

    bootstrap.Modal.getInstance(document.getElementById("phongModal"))?.hide();
    loadPhongs(currentPage);
  } catch (err) {
    errorBox.innerText = err.message && err.message !== "Forbidden"
      ? err.message
      : "Không thể lưu thông tin phòng. Vui lòng kiểm tra lại dữ liệu.";
  }
});

loadToaOptions();
loadPhongs(1);
