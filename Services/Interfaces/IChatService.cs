using HDKTech.Models;

namespace HDKTech.Services.Interfaces
{
    /// <summary>
    /// Service xử lý business logic cho hệ thống chat realtime.
    /// Controllers và ChatHub chỉ inject interface này, không trực tiếp dùng repository hay DbContext.
    /// </summary>
    public interface IChatService
    {
        // ── Dành cho Guest/User ChatController ───────────────────────────

        /// <summary>
        /// Lấy lịch sử tin nhắn của một session.
        /// </summary>
        Task<IEnumerable<object>> GetHistoryAsync(int sessionId);

        /// <summary>
        /// Lấy session đang mở của user đã đăng nhập (status != "closed").
        /// Trả về null nếu không có session nào.
        /// </summary>
        Task<ChatSession?> GetCurrentSessionAsync(string userId);

        // ── Dành cho Admin ChatController ────────────────────────────────

        /// <summary>Lấy tất cả session chưa đóng (admin view)</summary>
        Task<List<ChatSession>> GetActiveSessionsAsync();

        /// <summary>Đếm session đã đóng trong n ngày gần nhất</summary>
        Task<int> GetClosedCountAsync(int days = 7);

        /// <summary>Lấy danh sách tin nhắn của session (admin panel)</summary>
        Task<IEnumerable<object>> GetSessionMessagesAsync(int sessionId);

        // ── Dành cho ChatHub ─────────────────────────────────────────────

        /// <summary>
        /// Khởi tạo hoặc khôi phục session.
        /// userId = null → khách vãng lai (guest).
        /// Với guest: kiểm tra session active theo phone/email trước khi tạo mới.
        /// </summary>
        Task<ChatSessionStartResult> StartOrRestoreSessionAsync(
            string? userId, string? guestName, string? guestPhone = null, string? guestEmail = null);

        /// <summary>
        /// Xử lý gửi tin nhắn: lưu DB, tự động chuyển status waiting→open nếu staff gửi.
        /// senderId = null → guest. senderName phải truyền rõ khi senderId null.
        /// </summary>
        Task<ChatSendMessageResult> AddMessageAsync(
            int sessionId, string? senderId, string senderName, string content, bool senderIsStaff);

        /// <summary>Staff tham gia session.</summary>
        Task<bool> JoinSessionAsync(int sessionId, string staffId);

        /// <summary>Đóng session.</summary>
        Task<bool> CloseSessionAsync(int sessionId);
    }

    // ── Result DTOs ─────────────────────────────────────────────────────────

    public record ChatSessionStartResult(
        int SessionId,
        bool IsNew,
        IEnumerable<object> Messages,
        bool IsMember = false   // true nếu user đã đăng nhập qua Identity
    );

    public record ChatSendMessageResult(
        int MessageId,
        DateTime SentAt,
        bool SessionStatusChanged,
        string NewStatus
    );
}
