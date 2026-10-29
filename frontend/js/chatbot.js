// chatbot.js — giao diện hỏi đáp nội quy KTX bằng AI (KT3).
// Gọi POST /api/chatbot/hoi qua lớp dùng chung apiFetch (đã tự gắn JWT, xử lý lỗi 401/403).

requireLogin();

const user = getCurrentUser();
if (user) {
  document.getElementById("userInfo").innerText = `${user.hoTen} (${user.vaiTro})`;
}

const chatWindow = document.getElementById("chatWindow");
const chatForm = document.getElementById("chatForm");
const chatInput = document.getElementById("chatInput");
const btnSend = document.getElementById("btnSend");
const chatLoading = document.getElementById("chatLoading");

function appendMessage(role, text, sources = [], isWarning = false) {
  const row = document.createElement("div");
  row.className = `msg-row ${role}`;

  const bubble = document.createElement("div");
  bubble.className = `msg-bubble ${role}${isWarning ? " warning" : ""}`;
  bubble.innerText = text;

  if (sources.length > 0) {
    const src = document.createElement("div");
    src.className = "msg-sources";
    src.append("Nguồn: ");
    sources.forEach((source) => {
      const badge = document.createElement("span");
      badge.className = "badge me-1";
      badge.textContent = source;
      src.appendChild(badge);
    });
    bubble.appendChild(src);
  }

  row.appendChild(bubble);
  chatWindow.appendChild(row);
  chatWindow.scrollTop = chatWindow.scrollHeight;
}

// Lời chào ban đầu, giúp người dùng biết chatbot dùng để làm gì (tiêu chí #10 - UX tự nhiên).
appendMessage(
  "ai",
  "Chào bạn! Mình hỗ trợ tra cứu hồ sơ, phòng ở, hợp đồng, hóa đơn và phản ánh của chính bạn; " +
  "hướng dẫn đăng ký, chuyển/trả phòng, nộp tiền và gửi phản ánh. Bạn cũng có thể hỏi phòng trống và nội quy KTX."
);

const suggestions = document.getElementById("studentSuggestions");
if (user?.vaiTro !== "SinhVien") suggestions.hidden = true;
suggestions.querySelectorAll("[data-question]").forEach(button => {
  button.addEventListener("click", () => {
    if (btnSend.disabled) return;
    chatInput.value = button.dataset.question;
    chatForm.requestSubmit();
  });
});

chatForm.addEventListener("submit", async (e) => {
  e.preventDefault();
  const cauHoi = chatInput.value.trim();
  if (!cauHoi) return;

  appendMessage("user", cauHoi);
  chatInput.value = "";
  btnSend.disabled = true;
  chatLoading.style.display = "block";

  try {
    const res = await apiFetch("/chatbot/hoi", { method: "POST", body: { cauHoi } });
    const d = res.data;
    appendMessage("ai", d.traLoi, d.nguonNoiQuy || [], !!d.coCanhBao);
  } catch (err) {
    // Lỗi mạng/timeout/rate-limit/... đã được apiFetch hiển thị qua toast (tiêu chí #7);
    // vẫn thêm một bong bóng chat báo lỗi để người dùng thấy ngay trong luồng hội thoại.
    appendMessage("ai", "Đã có lỗi xảy ra khi hỏi AI. Vui lòng thử lại sau.", [], true);
  } finally {
    btnSend.disabled = false;
    chatLoading.style.display = "none";
    chatInput.focus();
  }
});
