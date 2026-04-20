using HDKTech.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using HDKTech.Models;

namespace HDKTech.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireAdminArea")]
    [Route("admin/[controller]")]
    public class ChatController : Controller
    {
        private readonly IChatService _chatService;
        private readonly UserManager<AppUser> _userManager;

        public ChatController(IChatService chatService, UserManager<AppUser> userManager)
        {
            _chatService = chatService;
            _userManager = userManager;
        }

        /// <summary>Trang quản lý chat sessions</summary>
        [HttpGet("")]
        [HttpGet("index")]
        public async Task<IActionResult> Index()
        {
            var sessions     = await _chatService.GetActiveSessionsAsync();
            var closedCount  = await _chatService.GetClosedCountAsync(days: 7);

            ViewBag.ClosedCount = closedCount;
            return View(sessions);
        }

        /// <summary>Lấy danh sách sessions dạng JSON (AJAX polling fallback)</summary>
        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions()
        {
            var sessions = await _chatService.GetActiveSessionsAsync();

            var result = sessions.Select(s => new
            {
                s.Id,
                s.Status,
                s.StartedAt,
                CustomerName   = s.DisplayName,
                MessageCount   = s.Messages?.Count ?? 0,
                LastMessage    = s.Messages?
                    .OrderByDescending(m => m.SentAt)
                    .Select(m => m.Content)
                    .FirstOrDefault() ?? "",
                LastMessageAt  = s.Messages?
                    .OrderByDescending(m => m.SentAt)
                    .Select(m => (DateTime?)m.SentAt)
                    .FirstOrDefault()
            });

            return Json(result);
        }

        /// <summary>Lấy lịch sử tin nhắn của session (AJAX từ admin panel)</summary>
        [HttpGet("session/{sessionId}/messages")]
        public async Task<IActionResult> GetMessages(int sessionId)
        {
            var messages = await _chatService.GetSessionMessagesAsync(sessionId);
            return Json(messages);
        }
    }
}
