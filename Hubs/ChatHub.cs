using HDKTech.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using HDKTech.Models;

namespace HDKTech.Hubs
{
    /// <summary>
    /// ChatHub — SignalR hub xử lý realtime giữa khách hàng (user đã đăng nhập + guest) và staff/admin.
    ///
    /// Groups:
    ///   - "session_{id}"  : cả khách + staff đang trong session đó
    ///   - "staff_room"    : tất cả staff/admin/manager đang online
    /// </summary>
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<ChatHub> _logger;

        // ConnectionId → UserId (cho user đã đăng nhập)
        private static readonly Dictionary<string, string> _connections = new();

        // ConnectionId → (GuestName, GuestPhone) (cho khách vãng lai chưa đăng nhập)
        private static readonly Dictionary<string, (string GuestName, string? GuestPhone)> _guestConnections = new();

        private static readonly object _lock = new();

        public ChatHub(
            IChatService chatService,
            UserManager<AppUser> userManager,
            ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _userManager = userManager;
            _logger = logger;
        }

        // ── Kết nối / Ngắt kết nối ───────────────────────────────────────

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                lock (_lock) { _connections[Context.ConnectionId] = userId; }

                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Any(r => r is "Admin" or "Manager" or "Staff"))
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, "staff_room");
                        _logger.LogInformation("Staff {Name} connected to chat hub", user.FullName);
                    }
                }
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            lock (_lock)
            {
                _connections.Remove(Context.ConnectionId);
                _guestConnections.Remove(Context.ConnectionId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        // ── Khách hàng: Bắt đầu hoặc khôi phục session ──────────────────

        /// <param name="guestName">Tên khách (dùng khi chưa đăng nhập)</param>
        /// <param name="guestPhone">SĐT khách (bắt buộc với guest để nhận diện lại)</param>
        /// <param name="guestEmail">Email khách (tùy chọn, fallback nếu không có SĐT)</param>
        public async Task<object> StartSession(
            string guestName,
            string? guestPhone = null,
            string? guestEmail = null)
        {
            var userId = Context.UserIdentifier;

            // Nếu là guest → đăng ký tên vào memory để dùng cho SendMessage / Typing
            if (string.IsNullOrEmpty(userId))
            {
                var name = string.IsNullOrWhiteSpace(guestName) ? "Khách hàng" : guestName.Trim();
                lock (_lock)
                {
                    _guestConnections[Context.ConnectionId] = (name, guestPhone?.Trim());
                }
            }

            var result = await _chatService.StartOrRestoreSessionAsync(
                userId, guestName, guestPhone, guestEmail);

            await Groups.AddToGroupAsync(Context.ConnectionId, $"session_{result.SessionId}");

            // Nếu là session mới → thông báo toàn bộ staff đang online
            if (result.IsNew)
            {
                string displayName;
                if (result.IsMember && userId != null)
                {
                    var user = await _userManager.FindByIdAsync(userId);
                    displayName = user?.FullName ?? user?.UserName ?? "Thành viên";
                }
                else
                {
                    displayName = string.IsNullOrWhiteSpace(guestName) ? "Khách hàng" : guestName.Trim();
                }

                await Clients.Group("staff_room").SendAsync("NewSession", new
                {
                    sessionId  = result.SessionId,
                    guestName  = displayName,
                    guestPhone = guestPhone,
                    guestEmail = guestEmail,
                    isMember   = result.IsMember,
                    startedAt  = DateTime.Now,
                    status     = "waiting"
                });
            }

            return new
            {
                sessionId = result.SessionId,
                messages  = result.Messages,
                isNew     = result.IsNew,
                isMember  = result.IsMember
            };
        }

        // ── Gửi tin nhắn ─────────────────────────────────────────────────

        public async Task SendMessage(int sessionId, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return;

            var userId = Context.UserIdentifier;
            string? senderId;
            string senderName;
            bool senderIsStaff;

            if (!string.IsNullOrEmpty(userId))
            {
                // User đã đăng nhập
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return;

                var roles = await _userManager.GetRolesAsync(user);
                senderIsStaff = roles.Any(r => r is "Admin" or "Manager" or "Staff");
                senderId      = userId;
                senderName    = user.FullName ?? user.UserName ?? "Người dùng";
            }
            else
            {
                // Khách vãng lai — lấy tên từ memory
                (string GuestName, string? GuestPhone) guestInfo;
                lock (_lock)
                {
                    if (!_guestConnections.TryGetValue(Context.ConnectionId, out guestInfo))
                    {
                        _logger.LogWarning("SendMessage từ guest chưa đăng ký (connectionId={Id})", Context.ConnectionId);
                        return;
                    }
                }
                senderId      = null;
                senderName    = guestInfo.GuestName;
                senderIsStaff = false;
            }

            var msgResult = await _chatService.AddMessageAsync(
                sessionId, senderId, senderName, content.Trim(), senderIsStaff);

            // Broadcast tin nhắn tới session group
            await Clients.Group($"session_{sessionId}").SendAsync("ReceiveMessage", new
            {
                id         = msgResult.MessageId,
                sessionId,
                content    = content.Trim(),
                sentAt     = msgResult.SentAt,
                senderId   = senderId ?? string.Empty,
                senderName,
                isStaff    = senderIsStaff
            });

            // Cập nhật danh sách session cho staff (preview tin nhắn mới nhất)
            var preview = content.Length > 50 ? content[..50] + "..." : content;
            await Clients.Group("staff_room").SendAsync("SessionUpdated", new
            {
                sessionId,
                lastMessage   = preview,
                lastMessageAt = msgResult.SentAt,
                status        = msgResult.NewStatus
            });
        }

        // ── Staff: Tham gia session ───────────────────────────────────────

        public async Task JoinSession(int sessionId)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId)) return;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            var roles = await _userManager.GetRolesAsync(user);

            // Cho phép cả khách hàng đã đăng nhập join lại session của họ
            bool isStaff = roles.Any(r => r is "Admin" or "Manager" or "Staff");
            if (!isStaff)
            {
                // Chỉ join group, không thay đổi status
                await Groups.AddToGroupAsync(Context.ConnectionId, $"session_{sessionId}");
                return;
            }

            var joined = await _chatService.JoinSessionAsync(sessionId, userId);
            if (!joined) return;

            await Groups.AddToGroupAsync(Context.ConnectionId, $"session_{sessionId}");

            // Tin hệ thống thông báo staff đã vào
            await Clients.Group($"session_{sessionId}").SendAsync("SystemMessage", new
            {
                content = $"{user.FullName} đã tham gia hỗ trợ",
                sentAt  = DateTime.Now
            });

            // Cập nhật staff room
            await Clients.Group("staff_room").SendAsync("SessionUpdated", new
            {
                sessionId,
                status    = "open",
                staffName = user.FullName
            });
        }

        // ── Đóng session ─────────────────────────────────────────────────

        public async Task CloseSession(int sessionId)
        {
            var closed = await _chatService.CloseSessionAsync(sessionId);
            if (!closed) return;

            await Clients.Group($"session_{sessionId}").SendAsync("SessionClosed", new
            {
                sessionId,
                message = "Cuộc hội thoại đã kết thúc. Cảm ơn bạn đã liên hệ!"
            });

            await Clients.Group("staff_room").SendAsync("SessionClosed", new { sessionId });
        }

        // ── Typing indicator ─────────────────────────────────────────────

        public async Task Typing(int sessionId, bool isTyping)
        {
            var userId = Context.UserIdentifier;
            string typingUserId;
            string typingUserName;

            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return;
                typingUserId   = userId;
                typingUserName = user.FullName ?? "Người dùng";
            }
            else
            {
                (string GuestName, string? GuestPhone) guestInfo;
                lock (_lock)
                {
                    if (!_guestConnections.TryGetValue(Context.ConnectionId, out guestInfo))
                        return;
                }
                typingUserId   = Context.ConnectionId; // dùng connectionId làm ID tạm
                typingUserName = guestInfo.GuestName;
            }

            await Clients.OthersInGroup($"session_{sessionId}").SendAsync("UserTyping", new
            {
                sessionId,
                userId   = typingUserId,
                userName = typingUserName,
                isTyping
            });
        }
    }
}
