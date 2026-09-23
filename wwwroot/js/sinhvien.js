requireLogin();

const user = getCurrentUser();
if (user) {
  document.getElementById("userInfo").innerText = `${user.hoTen} (${user.vaiTro})`;
}

let currentPage = 1;
const pageSize = 10;

function setModalMode(isEdit) {
  // Khi sửa: ẩn các trường chỉ dùng lúc tạo mới (email đăng nhập, mật khẩu, mã SV, ngày sinh
  // không cho sửa vì mã SV là khóa nghiệp vụ và ngày sinh hiếm khi đổi qua form này).
  document.getElementById("groupCreateOnly").style.display = isEdit ? "none" : "";
  document.getElementById("groupPasswordOnly").style.display = isEdit ? "none" : "";
  document.getElementById("groupMaSvOnly").style.display = isEdit ? "none" : "";
  document.getElementById("groupNgaySinhOnly").style.display = isEdit ? "none" : "";
  document.getElementById("svModalTitle").innerText = isEdit ? "Sửa thông tin sinh viên" : "Thêm sinh viên";

  document.getElementById("svEmail").required = !isEdit;
  document.getElementById("svMaSv").required = !isEdit;
}

function resetForm() {
  document.getElementById("svForm").reset();
  document.getElementById("svId").value = "";
  document.getElementById("svFormError").innerText = "";
}

document.getElementById("btnAdd").addEventListener("click", () => {
  resetForm();
  setModalMode(false);
});

function renderPagination(totalCount, page) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const container = document.getElementById("pagination");
  container.innerHTML = `
    <div class="small text-muted">Tổng số: ${totalCount} sinh viên</div>
    <div class="btn-group">
      <button class="btn btn-sm btn-outline-secondary" id="btnPrev" ${page <= 1 ? "disabled" : ""}>&laquo; Trước</button>
      <button class="btn btn-sm btn-outline-secondary disabled">Trang ${page}/${totalPages}</button>
      <button class="btn btn-sm btn-outline-secondary" id="btnNext" ${page >= totalPages ? "disabled" : ""}>Sau &raquo;</button>
    </div>
  `;
  document.getElementById("btnPrev")?.addEventListener("click", () => {
    if (currentPage > 1) {
      currentPage--;
      loadSinhViens(currentPage);
    }
  });
  document.getElementById("btnNext")?.addEventListener("click", () => {
    if (currentPage < totalPages) {
      currentPage++;
      loadSinhViens(currentPage);
    }
  });
}

async function loadSinhViens(page = 1) {
  currentPage = page;
  const keyword = document.getElementById("fKeyword").value.trim();
  const lop = document.getElementById("fLop").value.trim();
  const sortBy = document.getElementById("fSortBy").value;

  const params = new URLSearchParams();
  if (keyword) params.set("keyword", keyword);
  if (lop) params.set("lop", lop);
  params.set("sortBy", sortBy);
  params.set("page", page);
  params.set("pageSize", pageSize);

  try {
    const res = await apiFetch(`/sinhvien?${params.toString()}`);
    const data = res.data;
    const tbody = document.getElementById("svTableBody");
    tbody.innerHTML = "";

    if (!data.items || data.items.length === 0) {
      tbody.innerHTML = `<tr><td colspan="7" class="text-center text-muted py-3">Không có sinh viên phù hợp.</td></tr>`;
    } else {
      data.items.forEach((sv) => {
        const tr = document.createElement("tr");
        tr.innerHTML = `
          <td>${sv.maSv}</td>
          <td>${sv.hoTen}</td>
          <td>${sv.gioiTinh === "Nam" ? "Nam" : sv.gioiTinh === "Nu" ? "Nữ" : "Khác"}</td>
          <td>${sv.lop ?? ""}</td>
          <td>${sv.sdt ?? ""}</td>
          <td>${sv.phongHienTai ?? '<span class="text-muted">Chưa xếp phòng</span>'}</td>
          <td class="text-end">
            ${!sv.phongHienTai ? `<button class="btn btn-sm btn-outline-success btn-assign" data-id="${sv.id}" data-name="${sv.hoTen}"><i class="bi bi-door-open me-1"></i>Xếp phòng</button>` : ""}
            <button class="btn btn-sm btn-outline-primary btn-edit" data-id="${sv.id}">Sửa</button>
            <button class="btn btn-sm btn-outline-danger btn-delete" data-id="${sv.id}">Xóa</button>
          </td>
        `;
        tbody.appendChild(tr);
      });
    }

    renderPagination(data.totalCount, data.page);
    bindRowActions();
  } catch {
    // Lỗi đã hiển thị qua toast trong apiFetch
  }
}

function bindRowActions() {
  document.querySelectorAll(".btn-assign").forEach((btn) => {
    btn.addEventListener("click", async () => {
      const id = Number(btn.getAttribute("data-id"));
      const name = btn.getAttribute("data-name");
      document.getElementById("assignSinhVienId").value = id;
      document.getElementById("assignSinhVienName").value = name;
      const errorBox = document.getElementById("assignRoomError");
      errorBox.innerText = "";

      try {
        const res = await apiFetch("/phong/cho-trong");
        const select = document.getElementById("assignGiuongSelect");
        select.innerHTML = '<option value="">-- Chọn giường trống --</option>';

        if (!res.data || res.data.length === 0) {
          select.innerHTML = '<option value="">Không còn giường trống</option>';
        } else {
          res.data.forEach((g) => {
            const opt = document.createElement("option");
            opt.value = g.id;
            opt.textContent = `${g.soPhong} - Giường ${g.soGiuong}`;
            select.appendChild(opt);
          });
        }

        new bootstrap.Modal(document.getElementById("assignRoomModal")).show();
      } catch {
        errorBox.innerText = "Không thể tải danh sách giường trống.";
      }
    });
  });

  document.querySelectorAll(".btn-edit").forEach((btn) => {
    btn.addEventListener("click", async () => {
      const id = btn.getAttribute("data-id");
      try {
        const res = await apiFetch(`/sinhvien/${id}`);
        const sv = res.data;
        resetForm();
        setModalMode(true);
        document.getElementById("svId").value = sv.id;
        document.getElementById("svHoTen").value = sv.hoTen;
        document.getElementById("svGioiTinh").value = sv.gioiTinh;
        document.getElementById("svNgaySinh").value = sv.ngaySinh ? sv.ngaySinh.substring(0, 10) : "";
        document.getElementById("svLop").value = sv.lop ?? "";
        document.getElementById("svSdt").value = sv.sdt ?? "";
        new bootstrap.Modal(document.getElementById("svModal")).show();
      } catch { /* toast đã hiển thị lỗi */ }
    });
  });

  document.querySelectorAll(".btn-delete").forEach((btn) => {
    btn.addEventListener("click", async () => {
      const id = btn.getAttribute("data-id");
      if (!confirm("Bạn có chắc muốn xóa sinh viên này? Hành động này không thể hoàn tác.")) return;
      try {
        await apiFetch(`/sinhvien/${id}`, { method: "DELETE" });
        showToast("Đã xóa sinh viên.", "success");
        loadSinhViens(currentPage);
      } catch { /* toast đã hiển thị lỗi (VD: còn hợp đồng đang hiệu lực) */ }
    });
  });
}

document.getElementById("btnSearch").addEventListener("click", () => loadSinhViens(1));

document.getElementById("btnSaveSv").addEventListener("click", async () => {
  const errorBox = document.getElementById("svFormError");
  errorBox.innerText = "";

  const id = document.getElementById("svId").value;
  const isEdit = !!id;

  const hoTen = document.getElementById("svHoTen").value.trim();
  const gioiTinh = document.getElementById("svGioiTinh").value;
  const ngaySinh = document.getElementById("svNgaySinh").value;
  const lop = document.getElementById("svLop").value.trim();
  const sdt = document.getElementById("svSdt").value.trim();

  if (!hoTen) {
    errorBox.innerText = "Vui lòng nhập họ tên.";
    return;
  }

  try {
    if (isEdit) {
      await apiFetch(`/sinhvien/${id}`, {
        method: "PUT",
        body: { hoTen, gioiTinh, ngaySinh: ngaySinh || null, lop, sdt },
      });
      showToast("Đã cập nhật thông tin sinh viên.", "success");
    } else {
      const email = document.getElementById("svEmail").value.trim();
      const matKhau = document.getElementById("svMatKhau").value;
      const maSv = document.getElementById("svMaSv").value.trim();

      if (!email || !maSv) {
        errorBox.innerText = "Vui lòng nhập đầy đủ email và mã số sinh viên.";
        return;
      }
      if (!matKhau || matKhau.length < 6) {
        errorBox.innerText = "Mật khẩu phải có ít nhất 6 ký tự.";
        return;
      }

      await apiFetch("/sinhvien", {
        method: "POST",
        body: { email, matKhau, maSv, hoTen, gioiTinh, ngaySinh: ngaySinh || null, lop, sdt },
      });
      showToast("Đã thêm sinh viên mới.", "success");
    }

    bootstrap.Modal.getInstance(document.getElementById("svModal"))?.hide();
    loadSinhViens(currentPage);
  } catch (err) {
    errorBox.innerText = err.message && err.message !== "Forbidden"
      ? err.message
      : "Không thể lưu thông tin sinh viên. Vui lòng kiểm tra lại dữ liệu.";
  }
});

document.getElementById("btnAssignRoom").addEventListener("click", async () => {
  const errorBox = document.getElementById("assignRoomError");
  errorBox.innerText = "";

  const sinhVienId = Number(document.getElementById("assignSinhVienId").value);
  const giuongId = Number(document.getElementById("assignGiuongSelect").value);

  if (!sinhVienId || !giuongId) {
    errorBox.innerText = "Vui lòng chọn giường trống để xếp phòng.";
    return;
  }

  try {
    await apiFetch("/hopdong/xep-phong", {
      method: "POST",
      body: { sinhVienId, giuongId },
    });

    showToast("Đã xếp sinh viên vào phòng thành công.", "success");
    bootstrap.Modal.getInstance(document.getElementById("assignRoomModal"))?.hide();
    loadSinhViens(currentPage);
  } catch (err) {
    errorBox.innerText = err.message && err.message !== "Forbidden"
      ? err.message
      : "Không thể xếp phòng cho sinh viên.";
  }
});

loadSinhViens(1);
