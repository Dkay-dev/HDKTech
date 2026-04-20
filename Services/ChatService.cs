using HDKTech.Models;
using HDKTech.Repositories.Interfaces;
using HDKTech.Services.Interfaces;

namespace HDKTech.Services
{
    /// <summary>
    /// Xử lý toàn bộ business logic cho hệ thống chat.
    /// Hỗ trợ cả user đã đăng nhập lẫn khách vãng lai (guest).
    /// </summary>
    public class ChatService : IChatService
    {
        private readonly IChatRepository _chatRepo;
        private readonly ILogger<ChatService> _logger;

        public ChatService(IChatRepository chatRepo, ILogger<ChatService> logger)
        {
            _chatRepo = chatRepo;
            _logger = logger;
        }

        // ── Guest/User ChatController ────────────────────────────────────

        public async Task<IEnumerable<object>> GetHistoryAsync(int sessionId)
        {
            var messages = await _chatRepo.GetMessagesBySessionIdAsync(sessionId);

            return messages.Select(m => new
            {
                m.Id,
                m.Content,
                m.SentAt,
                SenderName = m.SenderName
                    ?? m.Sender?.FullName
                    ?? "Khách hàng",
                SenderId = m.SenderId ?? string.Empty
            });
        }

        public async Task<ChatSession?> GetCurrentSessionAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            return await _chatRepo.GetActiveSessionByUserIdAsync(userId);
        }

        // ── Admin ChatController ─────────────────────────────────────────

        public async Task<List<ChatSession>> GetActiveSessionsAsync()
        {
            return await _chatRepo.GetActiveSessionsAsync();
        }

        public async Task<int> GetClosedCountAsync(int days = 7)
        {
            return await _chatRepo.CountClosedRecentAsync(days);
        }

        public async Task<IEnumerable<object>> GetSessionMessagesAsync(int sessionId)
        {
            var messages = await _chatRepo.GetMessagesBySessionIdAsync(sessionId);

            return messages.Select(m => new
            {
                m.Id,
                m.Content,
                m.SentAt,
                SenderName = m.SenderName
                    ?? m.Sender?.FullName
                    ?? "Khách hàng",
                SenderId = m.SenderId ?? string.Empty
            });
        }

        // ── ChatHub operations ───────────────────────────────────────────

        public async Task<ChatSessionStartResult> StartOrRestoreSessionAsync(
            string? userId, string? guestName, string? guestPhone = null, string? guestEmail = null)
        {
            // ── User đã đăng nhập (Member) ────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var existing = await _chatRepo.GetActiveSessionByUserIdAsync(userId);
                if (existing != null)
                {
                    var history = await _chatRepo.GetMessagesBySessionIdAsync(existing.Id);
                    _logger.LogDebug("Restored Member session {Id} for user {UserId}", existing.Id, userId);
                    return new ChatSessionStartResult(existing.Id, IsNew: false, MapMessages(history), IsMember: true);
                }

                // Tạo session mới cho Member
                var memberSession = new ChatSession
                {
                    CustomerId = userId,
                    StartedAt  = DateTime.Now,
                    Status     = "waiting"
                };
                await _chatRepo.CreateSessionAsync(memberSession);

                _logger.LogInformation("New Member session {Id} for user {UserId}", memberSession.Id, userId);
                return new ChatSessionStartResult(memberSession.Id, IsNew: true, Enumerable.Empty<object>(), IsMember: true);
            }

            // ── Guest (chưa đăng nhập) ────────────────────────────────────────────
            // Ưu tiên check SĐT, fallback sang email — tránh tạo session trùng
            var existingGuest = await _chatRepo.GetActiveGuestSessionAsync(guestPhone, guestEmail);
            if (existingGuest != null)
            {
                // Cập nhật tên nếu guest đổi tên khi quay lại (tùy chọn)
                if (!string.IsNullOrWhiteSpace(guestName) &&
                    !string.Equals(existingGuest.GuestName, guestName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    existingGuest.GuestName = guestName.Trim();
                    await _chatRepo.UpdateSessionAsync(existingGuest);
                }

                var history = await _chatRepo.GetMessagesBySessionIdAsync(existingGuest.Id);
                _logger.LogDebug("Restored Guest session {Id} by phone/email", existingGuest.Id);
                return new ChatSessionStartResult(existingGuest.Id, IsNew: false, MapMessages(history), IsMember: false);
            }

            // Tạo session mới cho Guest
            var guestSession = new ChatSession
            {
                CustomerId = null,
                GuestName  = string.IsNullOrWhiteSpace(guestName) ? "Khách hàng" : guestName.Trim(),
                GuestPhone = string.IsNullOrWhiteSpace(guestPhone) ? null : guestPhone.Trim(),
                GuestEmail = string.IsNullOrWhiteSpace(guestEmail) ? null : guestEmail.Trim().ToLowerInvariant(),
                StartedAt  = DateTime.Now,
                Status     = "waiting"
            };

            await _chatRepo.CreateSessionAsync(guestSession);

            _logger.LogInformation("New Guest session {Id} for {Name} / phone={Phone}",
                guestSession.Id, guestSession.GuestName, guestSession.GuestPhone);

            return new ChatSessionStartResult(guestSession.Id, IsNew: true, Enumerable.Empty<object>(), IsMember: false);
        }

        public async Task<ChatSendMessageResult> AddMessageAsync(
            int sessionId, string? senderId, string senderName, string content, bool senderIsStaff)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Nội dung tin nhắn không được để trống.", nameof(content));

            var session = await _chatRepo.GetSessionByIdAsync(sessionId)
                ?? throw new InvalidOperationException($"Session {sessionId} không tồn tại.");

            var message = new ChatMessage
            {
                SessionId  = sessionId,
                SenderId   = string.IsNullOrWhiteSpace(senderId) ? null : senderId,
                SenderName = senderName.Trim(),
                Content    = content.Trim(),
                SentAt     = DateTime.Now
            };

            await _chatRepo.AddMessageAsync(message);

            // Tự động chuyển waiting → open khi staff gửi tin đầu tiên
            bool statusChanged = false;
            if (senderIsStaff && session.Status == "waiting")
            {
                session.Status  = "open";
                session.StaffId = senderId;
                await _chatRepo.UpdateSessionAsync(session);
                statusChanged = true;

                _logger.LogInformation(
                    "Session {Id} chuyển sang 'open' sau khi staff {StaffId} trả lời",
                    sessionId, senderId);
            }

            return new ChatSendMessageResult(
                MessageId:            message.Id,
                SentAt:               message.SentAt,
                SessionStatusChanged: statusChanged,
                NewStatus:            session.Status!
            );
        }

        public async Task<bool> JoinSessionAsync(int sessionId, string staffId)
        {
            var session = await _chatRepo.GetSessionByIdAsync(sessionId);
            if (session == null)
            {
                _logger.LogWarning("JoinSession: session {Id} không tồn tại", sessionId);
                return false;
            }

            if (session.Status == "waiting")
            {
                session.Status  = "open";
                session.StaffId = staffId;
                await _chatRepo.UpdateSessionAsync(session);
                _logger.LogInformation("Staff {Id} tham gia session {SessionId}", staffId, sessionId);
            }

            return true;
        }

        public async Task<bool> CloseSessionAsync(int sessionId)
        {
            var session = await _chatRepo.GetSessionByIdAsync(sessionId);
            if (session == null)
            {
                _logger.LogWarning("CloseSession: session {Id} không tồn tại", sessionId);
                return false;
            }

            session.Status  = "closed";
            session.EndedAt = DateTime.Now;
            await _chatRepo.UpdateSessionAsync(session);

            _logger.LogInformation("Session {Id} đã được đóng", sessionId);
            return true;
        }

        // ── Private helpers ──────────────────────────────────────────────

        private static IEnumerable<object> MapMessages(IEnumerable<ChatMessage> messages)
        {
            return messages.Select(m => (object)new
            {
                m.Id,
                m.Content,
                m.SentAt,
                SenderName = m.SenderName ?? m.Sender?.FullName ?? "Khách hàng",
                SenderId   = m.SenderId ?? string.Empty
            });
        }
    }
}
