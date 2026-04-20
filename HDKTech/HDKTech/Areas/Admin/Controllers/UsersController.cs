using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Services;

namespace HDKTech.Areas.Admin.Controllers
{
    /// <summary>
    /// UsersController refactor — dùng hoàn toàn ASP.NET Identity.
    ///  - Role lấy từ AspNetUserRoles qua <see cref="UserManager{TUser}.GetRolesAsync"/>.
    ///  - Đổi role dùng AddToRole / RemoveFromRoles (không đụng DB trực tiếp).
    /// </summary>
    [Area("Admin")]
    [Authorize(Policy = "RequireManager")]
    [Route("admin/[controller]")]
    public class UsersController : Controller
    {
        private readonly UserManager<AppUser>       _userManager;
        private readonly RoleManager<IdentityRole>  _roleManager;
        private readonly HDKTechContext             _db;
        private readonly ISystemLogService          _logService;
        private readonly ILogger<UsersController>   _logger;

        private const decimal VipThreshold = 5_000_000m;

        public UsersController(
            UserManager<AppUser>       userManager,
            RoleManager<IdentityRole>  roleManager,
            HDKTechContext             db,
            ISystemLogService          logService,
            ILogger<UsersController>   logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db          = db;
            _logService  = logService;
            _logger      = logger;
        }

        // ── GET /admin/users ──────────────────────────────────────────
        [HttpGet("")]
        [HttpGet("index")]
        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 20)
        {
            var q = _db.Users
                .AsNoTracking()
                .Include(u => u.Orders)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(u =>
                    u.FullName.ToLower().Contains(s) ||
                    (u.Email != null && u.Email.ToLower().Contains(s)));
            }

            var allUsers = await q.OrderBy(u => u.FullName).ToListAsync();

            // Lấy role name cho từng user (AspNetUserRoles join AspNetRoles).
            // Dùng IQueryable để khỏi N+1 khi danh sách dài.
            var userIds = allUsers.Select(u => u.Id).ToList();
            var userRoleMap = await (
                from ur in _db.UserRoles.AsNoTracking()
                join r  in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
                where userIds.Contains(ur.UserId)
                select new { ur.UserId, r.Name }
            ).ToListAsync();

            var roleByUser = userRoleMap
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Name ?? "").First());

            var now = DateTime.UtcNow;
            var viewModels = allUsers.Select(u =>
            {
                var totalSpent = u.Orders?
                    .Where(o => o.Status == OrderStatus.Delivered)
                    .Sum(o => o.TotalAmount) ?? 0m;

                return new UserListViewModel
                {
                    Id         = u.Id,
                    FullName   = u.FullName,
                    Email      = u.Email ?? "",
                    Role       = roleByUser.TryGetValue(u.Id, out var rn) ? rn : "—",
                    IsLocked   = u.LockoutEnd.HasValue && u.LockoutEnd.Value > now,
                    TotalSpent = totalSpent,
                    IsVip      = totalSpent >= VipThreshold,
                    CreatedAt  = u.CreatedAt
                };
            }).ToList();

            var totalCount = viewModels.Count;
            var paged      = viewModels.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Search    = search;
            ViewBag.Page      = page;
            ViewBag.PageSize  = pageSize;
            ViewBag.TotalCount= totalCount;
            ViewBag.TotalPages= (int)Math.Ceiling((double)totalCount / pageSize);

            // AllRoles: tất cả role trong AspNetRoles.
            ViewBag.AllRoles = await _roleManager.Roles
                .AsNoTracking()
                .OrderBy(r => r.Name)
                .Select(r => r.Name ?? "")
                .ToListAsync();

            return View(paged);
        }

        // ── POST /admin/users/update-role ─────────────────────────────
        [HttpPost("update-role")]
        [Authorize(Policy = "RequireAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRole(string userId, string newRole)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(newRole))
                return Json(new { success = false, message = "Thiếu tham số." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy người dùng." });

            if (!await _roleManager.RoleExistsAsync(newRole))
                return Json(new { success = false, message = $"Role '{newRole}' không tồn tại." });

            var currentRoles = await _userManager.GetRolesAsync(user);
            var oldRoleName  = currentRoles.FirstOrDefault() ?? "—";

            // Idempotent — nếu role đã đúng, bỏ qua việc đụng DB.
            if (currentRoles.Count == 1 &&
                string.Equals(currentRoles[0], newRole, StringComparison.OrdinalIgnoreCase))
            {
                return Json(new
                {
                    success = true,
                    message = $"Người dùng đã ở role '{newRole}'."
                });
            }

            if (currentRoles.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                    return Json(new
                    {
                        success = false,
                        message = "Không gỡ được role cũ: " +
                            string.Join("; ", removeResult.Errors.Select(e => e.Description))
                    });
            }

            var addResult = await _userManager.AddToRoleAsync(user, newRole);
            if (!addResult.Succeeded)
                return Json(new
                {
                    success = false,
                    message = "Không gán được role mới: " +
                        string.Join("; ", addResult.Errors.Select(e => e.Description))
                });

            // Ép refresh security stamp → cookie cũ bị vô hiệu
            await _userManager.UpdateSecurityStampAsync(user);

            var actor = User.Identity?.Name ?? "System";
            await _logService.LogActionAsync(
                username   : actor,
                actionType : "Update",
                module     : "User",
                description: $"Cập nhật role của '{user.FullName}' từ '{oldRoleName}' → '{newRole}'",
                entityId   : user.Id,
                entityName : user.FullName,
                oldValue   : oldRoleName,
                newValue   : newRole,
                userRole   : User.IsInRole("Admin") ? "Admin" : "Manager",
                userId     : user.Id);

            return Json(new
            {
                success = true,
                message = $"Đã cập nhật role thành '{newRole}'. Người dùng sẽ phải đăng nhập lại."
            });
        }

        // ── POST /admin/users/lock ────────────────────────────────────
        [HttpPost("lock")]
        [Authorize(Policy = "RequireAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockAccount(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng." });

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

        // ── POST /admin/users/unlock ──────────────────────────────────
        [HttpPost("unlock")]
        [Authorize(Policy = "RequireAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockAccount(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng." });

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);

            var actor = User.Identity?.Name ?? "System";
            await _logService.LogActionAsync(
                username   : actor,
                actionType : "Unlock",
                module     : "User",
                description: $"Mở khoá tài khoản của '{user.FullName}' ({user.Email}).",
                entityId   : user.Id,
                entityName : user.FullName,
                oldValue   : "Locked",
                newValue   : "Active",
                userRole   : "Admin",
                userId     : user.Id);

            return Json(new { success = true, message = $"Đã mở khoá tài khoản '{user.FullName}'." });
        }
    }

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
