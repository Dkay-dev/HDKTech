using HDKTech.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using HDKTech.Models;

namespace HDKTech.Controllers
{
    /// <summary>
    /// ChatController — REST API cho widget chat phía khách hàng (guest + user đã đăng nhập).
    /// Inject IChatService, không truy cập DbContext trực tiếp.
    /// </summary>
    public class ChatController : Controller
    {
        private readonly IChatService _chatService;
        private readonly UserManager<AppUser> _userManager;

        public ChatController(IChatService chatService, UserManager<AppUser> userManager)
        {
            _chatService = chatService;
            _userManager = userManager;
        }

        /// <summary>Lấy lịch sử tin nhắn của session (AJAX từ widget)</summary>
        [HttpGet]
        public async Task<IActionResult> History(int sessionId)
        {
            var messages = await _chatService.GetHistoryAsync(sessionId);
            return Json(messages);
        }

        /// <summary>Lấy session đang mở của user đã đăng nhập (dùng để restore khi reload trang)</summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> CurrentSession()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Json(null);

            var session = await _chatService.GetCurrentSessionAsync(userId);
            if (session == null) return Json(null);

            return Json(new
            {
                sessionId = session.Id,
                status    = session.Status,
                startedAt = session.StartedAt
            });
        }
    }
}
