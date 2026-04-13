using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Services;
using HDKTech.Areas.Admin.Constants;

namespace HDKTech.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    [Route("admin/[controller]")]
    public class UsersController : Controller
    {
        private readonly UserManager<AppUser>       _userManager;
        private readonly RoleManager<IdentityRole>  _roleManager;
        private readonly HDKTechContext             _db;
        private readonly ISystemLogService          _logService;
        private readonly ILogger<UsersController>   _logger;

        // ── VIP Threshold ──────────────────────────────────────────────────────
        private const decimal VipThreshold = 5_000_000m; // 5 triệu VNĐ

        public UsersController(
            UserManager<AppUser>      userManager,
            RoleManager<IdentityRole> roleManager,
            HDKTechContext            db,
            ISystemLogService         logService,
            ILogger<UsersController>  logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db          = db;
            _logService  = logService;
            _logger      = logger;
        }

        // ── GET /admin/users ──────────────────────────────────────────────────
        [HttpGet("")]
        [HttpGet("index")]
        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 20)
        {
            // 1. Lấy tất cả user, eager load Orders để tính TotalSpent
            var usersQuery = _userManager.Users
                .Include(u => u.Orders)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.FullName.ToLower().Contains(s) ||
                    u.Email!.ToLower().Contains(s));
            }

            var allUsers = await usersQuery.OrderBy(u => u.FullName).ToListAsync();

            // 2. Map sang ViewModel (phải async vì GetRolesAsync)
            var now = DateTime.UtcNow;
            var viewModels = new List<UserListViewModel>();

            foreach (var u in allUsers)
            {
                var roles      = await _userManager.GetRolesAsync(u);
                var totalSpent = u.Orders?
                    .Where(o => o.Status == AdminConstants.OrderDelivered)
                    .Sum(o => o.TotalAmount) ?? 0m;

                viewModels.Add(new UserListViewModel
                {
                    Id         = u.Id,
                    FullName   = u.FullName,
                    Email      = u.Email ?? "",
                    Role       = roles.FirstOrDefault() ?? "—",
                    IsLocked   = u.LockoutEnd.HasValue && u.LockoutEnd.Value > now,
                    TotalSpent = totalSpent,
                    IsVip      = totalSpent >= VipThreshold,
                    CreatedAt  = u.CreatedAt,
                });
            }

            // 3. Phân trang
            var totalCount = viewModels.Count;
            var paged      = viewModels.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Search    = search;
            ViewBag.Page      = page;
            ViewBag.PageSize  = pageSize;
            ViewBag.TotalCount= totalCount;
            ViewBag.TotalPages= (int)Math.Ceiling((double)totalCount / pageSize);

            // 4. Danh sách role để dropdown Sửa Role
            ViewBag.AllRoles = _roleManager.Roles.Select(r => r.Name).ToList();

            return View(paged);
        }

        // ── POST /admin/users/update-role ─────────────────────────────────────
        /// <summary>
        /// Cập nhật role cho user. Gọi UpdateSecurityStampAsync để ép user đó logout.
        /// </summary>
        [HttpPost("update-role")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRole(string userId, string newRole)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(newRole))
                return Json(new { success = false, message = "Thiếu tham số." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy người dùng." });

            // Kiểm tra role tồn tại
            if (!await _roleManager.RoleExistsAsync(newRole))
                return Json(new { success = false, message = $"Role '{newRole}' không tồn tại." });

            var currentRoles = await _userManager.GetRolesAsync(user);
            var oldRole      = currentRoles.FirstOrDefault() ?? "—";

            // Xoá toàn bộ role cũ rồi gán role mới
            if (currentRoles.Any())
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

            var addResult = await _userManager.AddToRoleAsync(user, newRole);
            if (!addResult.Succeeded)
            {
                var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                return Json(new { success = false, message = errors });
            }

            // Ép logout — security stamp thay đổi → cookie cũ invalid ngay lập tức
            await _userManager.UpdateSecurityStampAsync(user);

            var actor = User.Identity?.Name ?? "System";
            await _logService.LogActionAsync(
                username   : actor,
                actionType : "Update",
                module     : "User",
                description: $"Cập nhật role của '{user.FullName}' từ '{oldRole}' → '{newRole}'",
                entityId   : user.Id,
                entityName : user.FullName,
                oldValue   : oldRole,
                newValue   : newRole,
                userRole   : User.IsInRole("Admin") ? "Admin" : "Manager",
                userId     : user.Id);

            return Json(new { success = true, message = $"Đã cập nhật role thành '{newRole}'. Người dùng sẽ phải đăng nhập lại." });
        }

        // ── POST /admin/users/lock ─────────────────────────────────────────────
        [HttpPost("lock")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockAccount(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng." });

            // Khoá 100 năm = vĩnh viễn
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            await _userManager.UpdateSecurityStampAsync(user);

            var actor = User.Identity?.Name ?? "System";
            await _logService.LogActionAsync(
                username   : actor,
                actionType : "Lock",
                module     : "User",
                description: $"Khoá tài khoản của '{user.FullName}' ({user.Email})",
                entityId   : user.Id,
                entityName : user.FullName,
                oldValue   : "Active",
                newValue   : "Locked",
                userRole   : "Admin",
                userId     : user.Id);

            return Json(new { success = true, message = $"Đã khoá tài khoản '{user.FullName}'." });
        }

        // ── POST /admin/users/unlock ───────────────────────────────────────────
        [HttpPost("unlock")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockAccount(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng." });

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            var actor = User.Identity?.Name ?? "System";
            await _logService.LogActionAsync(
                username   : actor,
                actionType : "Unlock",
                module     : "User",
                description: $"Mở khoá tài khoản của '{user.FullName}' ({user.Email})",
                entityId   : user.Id,
                entityName : user.FullName,
                oldValue   : "Locked",
                newValue   : "Active",
                userRole   : "Admin",
                userId     : user.Id);

            return Json(new { success = true, message = $"Đã mở khoá tài khoản '{user.FullName}'." });
        }
    }

    // ── ViewModel ─────────────────────────────────────────────────────────────
    public class UserListViewModel
    {
        public string   Id         { get; set; } = "";
        public string   FullName   { get; set; } = "";
        public string   Email      { get; set; } = "";
        public string   Role       { get; set; } = "";
        public bool     IsLocked   { get; set; }
        public decimal  TotalSpent { get; set; }
        public bool     IsVip      { get; set; }
        public DateTime CreatedAt  { get; set; }
    }
}
