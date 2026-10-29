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
    src.innerHTML = "Nguồn: " + sources.map((s) => `<span class="badge">${s}</span>`).join(" ");
    bubble.appendChild(src);
  }

  row.appendChild(bubble);
  chatWindow.appendChild(row);
  chatWindow.scrollTop = chatWindow.scrollHeight;
}

// Lời chào ban đầu, giúp người dùng biết chatbot dùng để làm gì (tiêu chí #10 - UX tự nhiên).
appendMessage(
  "ai",
  "Chào bạn! Mình là trợ lý AI hỏi đáp Nội quy Ký túc xá. Bạn có thể hỏi mình về giờ giới nghiêm, " +
  "quy định khách ra vào, vệ sinh phòng ở, v.v. Câu trả lời chỉ mang tính tham khảo."
);

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
