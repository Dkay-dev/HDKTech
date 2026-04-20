using HDKTech.Models;

namespace HDKTech.Repositories.Interfaces
{
    /// <summary>
    /// Repository quản lý ChatSession và ChatMessage.
    /// Không kế thừa GenericRepository vì thao tác trên 2 entity.
    /// </summary>
    public interface IChatRepository
    {
        // ── ChatSession ──────────────────────────────────────────────────

        /// <summary>Lấy session theo id (có include Customer, Messages)</summary>
        Task<ChatSession?> GetSessionByIdAsync(int sessionId);

        /// <summary>Lấy session đang mở của user (status != "closed")</summary>
        Task<ChatSession?> GetActiveSessionByUserIdAsync(string userId);

        /// <summary>
        /// Tìm session guest đang active theo SĐT hoặc email.
        /// Dùng để chống tạo trùng session khi khách quay lại.
        /// </summary>
        Task<ChatSession?> GetActiveGuestSessionAsync(string? phone, string? email);

        /// <summary>Lấy tất cả session chưa đóng, sắp theo thời gian mới nhất</summary>
        Task<List<ChatSession>> GetActiveSessionsAsync();

        /// <summary>Đếm số session đã đóng trong n ngày gần nhất</summary>
        Task<int> CountClosedRecentAsync(int days = 7);

        /// <summary>Tạo session mới, lưu DB và trả về entity đã có Id</summary>
        Task<ChatSession> CreateSessionAsync(ChatSession session);

        /// <summary>Cập nhật session (status, staffId, endedAt)</summary>
        Task UpdateSessionAsync(ChatSession session);

        // ── ChatMessage ──────────────────────────────────────────────────

        /// <summary>Lấy danh sách tin nhắn của session, sắp theo thời gian</summary>
        Task<List<ChatMessage>> GetMessagesBySessionIdAsync(int sessionId);

        /// <summary>Thêm tin nhắn mới, lưu DB và trả về entity đã có Id</summary>
        Task<ChatMessage> AddMessageAsync(ChatMessage message);

        // ── Persistence ──────────────────────────────────────────────────
        Task<bool> SaveAsync();
    }
}
