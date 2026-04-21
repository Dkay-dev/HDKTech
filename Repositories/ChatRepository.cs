using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Repositories
{
    /// <summary>
    /// Xử lý data access cho ChatSession và ChatMessage.
    /// Không kế thừa GenericRepository vì quản lý đồng thời 2 entity.
    /// </summary>
    public class ChatRepository : IChatRepository
    {
        private readonly HDKTechContext _context;
        private readonly ILogger<ChatRepository> _logger;

        public ChatRepository(HDKTechContext context, ILogger<ChatRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ── ChatSession ──────────────────────────────────────────────────

        public async Task<ChatSession?> GetSessionByIdAsync(int sessionId)
        {
            return await _context.ChatSessions
                .Include(s => s.Customer)
                .Include(s => s.Staff)
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == sessionId);
        }

        public async Task<ChatSession?> GetActiveSessionByUserIdAsync(string userId)
        {
            return await _context.ChatSessions
                .Where(s => s.CustomerId == userId && s.Status != "closed")
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<ChatSession?> GetActiveGuestSessionAsync(string? phone, string? email)
        {
            // Phải là guest (CustomerId == null), chưa đóng, và khớp SĐT hoặc email
            bool hasPhone = !string.IsNullOrWhiteSpace(phone);
            bool hasEmail = !string.IsNullOrWhiteSpace(email);

            if (!hasPhone && !hasEmail) return null;

            return await _context.ChatSessions
                .Where(s => s.CustomerId == null
                    && s.Status != "closed"
                    && (
                        (hasPhone && s.GuestPhone == phone) ||
                        (hasEmail && s.GuestEmail == email)
                    ))
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ChatSession>> GetActiveSessionsAsync()
        {
            return await _context.ChatSessions
                .Include(s => s.Customer)
                .Include(s => s.Messages)
                .Where(s => s.Status != "closed")
                .OrderByDescending(s => s.StartedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<int> CountClosedRecentAsync(int days = 7)
        {
            var cutoff = DateTime.Now.AddDays(-days);
            return await _context.ChatSessions
                .CountAsync(s => s.Status == "closed" && s.EndedAt >= cutoff);
        }

        public async Task<ChatSession> CreateSessionAsync(ChatSession session)
        {
            try
            {
                _context.ChatSessions.Add(session);
                await _context.SaveChangesAsync();
                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tạo ChatSession cho CustomerId: {Id}", session.CustomerId);
                throw;
            }
        }

        public async Task UpdateSessionAsync(ChatSession session)
        {
            try
            {
                _context.ChatSessions.Update(session);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật ChatSession Id: {Id}", session.Id);
                throw;
            }
        }

        // ── ChatMessage ──────────────────────────────────────────────────

        public async Task<List<ChatMessage>> GetMessagesBySessionIdAsync(int sessionId)
        {
            return await _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.SentAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ChatMessage> AddMessageAsync(ChatMessage message)
        {
            try
            {
                _context.ChatMessages.Add(message);
                await _context.SaveChangesAsync();
                return message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lưu ChatMessage cho SessionId: {Id}", message.SessionId);
                throw;
            }
        }

        // ── Persistence ──────────────────────────────────────────────────

        public async Task<bool> SaveAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
