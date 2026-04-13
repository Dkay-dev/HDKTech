using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using HDKTech.Models;
using HDKTech.Services;
using System.Security.Claims;

using HDKTech.Areas.Admin.Repositories;

namespace HDKTech.Areas.Admin.Controllers
{
    // ── ViewModel inline: dùng cho Index view ──────────────────────────────────
    public class IdentityRoleListItem
    {
        public string Id              { get; set; } = "";
        public string Name            { get; set; } = "";
        public int    UserCount       { get; set; }
        public int    PermissionCount { get; set; }
    }

    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [Route("admin/[controller]")]
    public class RoleController : Controller
    {
        private readonly ILogger<RoleController>    _logger;
        private readonly ISystemLogService          _logService;
        private readonly RoleManager<IdentityRole>  _roleManager;
        private readonly UserManager<AppUser>       _userManager;

        /// <summary>
        /// Danh sách toàn bộ permissions hệ thống — render UI + whitelist validate.
        /// Format: "Module.Action" — khớp với PermissionRequirement(Module, Action).
        /// </summary>
        public static readonly IReadOnlyList<string> AllSystemPermissions = new[]
        {
            "Product.Read",    "Product.Create",    "Product.Update",    "Product.Delete",
            "Inventory.Read",  "Inventory.Update",
            "Order.Read",      "Order.Update",      "Order.Delete",
            "Category.Read",   "Category.Create",   "Category.Update",   "Category.Delete",
            "Brand.Read",      "Brand.Create",      "Brand.Update",      "Brand.Delete",
            "Banner.Read",     "Banner.Create",     "Banner.Update",     "Banner.Delete",
            "Promotion.Read",  "Promotion.Create",  "Promotion.Update",  "Promotion.Delete",
            "Role.Read",       "Role.Update",
            "Report.Export",
            "SystemLog.Read",
        };

        // System-protected roles — cannot be deleted
        private static readonly HashSet<string> ProtectedRoles =
            new(StringComparer.OrdinalIgnoreCase) { "Admin", "Manager", "User", "WarehouseStaff" };

        public RoleController(
            ILogger<RoleController>   logger,
            ISystemLogService         logService,
            RoleManager<IdentityRole> roleManager,
            UserManager<AppUser>      userManager)
        {
            _logger      = logger;
            _logService  = logService;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        // =====================================================================
        // INDEX — danh sách Identity Roles
        // =====================================================================

        /// <summary>
        /// Danh sách tất cả vai trò từ bảng AspNetRoles.
        /// GET: /admin/role
        /// </summary>
        [HttpGet]
        [Route("")]
        [Route("index")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var identityRoles = _roleManager.Roles.OrderBy(r => r.Name).ToList();

                var list = new List<IdentityRoleListItem>();
                foreach (var role in identityRoles)
                {
                    var users  = await _userManager.GetUsersInRoleAsync(role.Name!);
                    var claims = await _roleManager.GetClaimsAsync(role);
                    var permCount = claims.Count(c => c.Type == "Permission");

                    list.Add(new IdentityRoleListItem
                    {
                        Id              = role.Id,
                        Name            = role.Name!,
                        UserCount       = users.Count,
                        PermissionCount = permCount
                    });
                }

                return View(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading roles");
                TempData["Error"] = "Lỗi khi tải danh sách vai trò";
                return View(new List<IdentityRoleListItem>());
            }
        }

        // =====================================================================
        // CREATE
        // =====================================================================

        /// <summary>
        /// Form tạo vai trò mới.
        /// GET: /admin/role/create
        /// </summary>
        [HttpGet]
        [Route("create")]
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Lưu vai trò mới vào bảng AspNetRoles + ghi AuditLog.
        /// POST: /admin/role/create
        /// </summary>
        [HttpPost]
        [Route("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] string roleName)
        {
            try
            {
                roleName = roleName?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(roleName))
                {
                    ModelState.AddModelError("roleName", "Tên vai trò không được để trống.");
                    TempData["Error"] = "Tên vai trò không được để trống.";
                    return View();
                }

                // Kiểm tra trùng tên
                if (await _roleManager.RoleExistsAsync(roleName))
                {
                    ModelState.AddModelError("roleName", $"Vai trò '{roleName}' đã tồn tại.");
                    TempData["Error"] = $"Vai trò '{roleName}' đã tồn tại.";
                    return View();
                }

                var result = await _roleManager.CreateAsync(new IdentityRole(roleName));

                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    TempData["Error"] = $"Lỗi khi tạo vai trò: {errors}";
                    return View();
                }

                // AuditLog
                await _logService.LogActionAsync(
                    username:    User.Identity?.Name ?? "Admin",
                    actionType:  "Create",
                    module:      "PhanQuyen",
                    description: $"Tạo mới Vai trò Identity: '{roleName}'",
                    entityName:  roleName,
                    userRole:    "Admin"
                );

                TempData["Success"] = $"Tạo vai trò '{roleName}' thành công. Hãy gán quyền cho vai trò này.";
                return RedirectToAction(nameof(ManagePermissions), new { roleName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role");
                TempData["Error"] = "Lỗi khi tạo vai trò";
                return View();
            }
        }

        // =====================================================================
        // DELETE
        // =====================================================================

        /// <summary>
        /// Xoá vai trò khỏi AspNetRoles + ghi AuditLog.
        /// POST: /admin/role/delete
        /// </summary>
        [HttpPost]
        [Route("delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] string roleId)
        {
            try
            {
                var role = await _roleManager.FindByIdAsync(roleId);
                if (role == null)
                    return Json(new { success = false, message = "Không tìm thấy vai trò" });

                // Bảo vệ các role hệ thống
                if (ProtectedRoles.Contains(role.Name ?? ""))
                    return Json(new { success = false, message = $"Không thể xoá vai trò hệ thống '{role.Name}'" });

                var roleName = role.Name!;
                var result   = await _roleManager.DeleteAsync(role);

                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    return Json(new { success = false, message = $"Lỗi khi xoá: {errors}" });
                }

                // AuditLog
                await _logService.LogActionAsync(
                    username:    User.Identity?.Name ?? "Admin",
                    actionType:  "Delete",
                    module:      "PhanQuyen",
                    description: $"Xoá Vai trò: '{roleName}' (ID: {roleId})",
                    entityId:    roleId,
                    entityName:  roleName,
                    userRole:    "Admin"
                );

                return Json(new { success = true, message = $"Xóa vai trò '{roleName}' thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role");
                return Json(new { success = false, message = "Lỗi khi xóa vai trò" });
            }
        }

        // =====================================================================
        // POLICY-BASED PERMISSION MANAGEMENT (AspNetRoleClaims)
        // =====================================================================

        /// <summary>
        /// Trang quản lý Permission Claims cho một Identity Role.
        /// GET: /admin/role/manage-permissions/{roleName}
        /// </summary>
        [HttpGet]
        [Route("manage-permissions/{roleName}")]
        public async Task<IActionResult> ManagePermissions(string roleName)
        {
            try
            {
                var identityRole = await _roleManager.FindByNameAsync(roleName);
                if (identityRole == null)
                {
                    TempData["Error"] = $"Không tìm thấy Identity Role '{roleName}'";
                    return RedirectToAction("Index");
                }

                // Lấy Permission claims hiện có từ AspNetRoleClaims
                var existingClaims = await _roleManager.GetClaimsAsync(identityRole);
                var currentPerms   = existingClaims
                    .Where(c => c.Type == "Permission")
                    .Select(c => c.Value)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                // Nhóm theo Module để render bảng checkbox
                var grouped = AllSystemPermissions
                    .GroupBy(p => p.Split('.')[0])
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key, g => g.ToList());

                ViewBag.RoleName           = roleName;
                ViewBag.IdentityRoleId     = identityRole.Id;
                ViewBag.CurrentPermissions = currentPerms;
                ViewBag.GroupedPermissions = grouped;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ManagePermissions for {RoleName}", roleName);
                TempData["Error"] = "Lỗi khi tải trang quản lý quyền";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Lưu danh sách Permission Claims vào bảng AspNetRoleClaims.
        /// Xóa toàn bộ "Permission" claims cũ, sau đó INSERT lại danh sách mới.
        /// POST: /admin/role/save-permissions/{roleName}
        /// </summary>
        [HttpPost]
        [Route("save-permissions/{roleName}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePermissions(
            string roleName,
            [FromForm] List<string> permissions)
        {
            try
            {
                var identityRole = await _roleManager.FindByNameAsync(roleName);
                if (identityRole == null)
                {
                    TempData["Error"] = $"Không tìm thấy Identity Role '{roleName}'";
                    return RedirectToAction("Index");
                }

                // Bước 1 — Xóa toàn bộ "Permission" claims cũ khỏi AspNetRoleClaims
                var existingClaims   = await _roleManager.GetClaimsAsync(identityRole);
                var permissionClaims = existingClaims.Where(c => c.Type == "Permission").ToList();
                foreach (var claim in permissionClaims)
                    await _roleManager.RemoveClaimAsync(identityRole, claim);

                // Bước 2 — Thêm Permission claims mới (chỉ chấp nhận value trong whitelist)
                var validPerms = (permissions ?? new List<string>())
                    .Where(p => AllSystemPermissions.Contains(p))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var perm in validPerms)
                    await _roleManager.AddClaimAsync(identityRole, new Claim("Permission", perm));

                // ✅ Bước 3 — UpdateSecurityStampAsync cho TẤT CẢ user thuộc role này
                // Mục đích: buộc user đang đăng nhập phải refresh token → nhận quyền mới ngay lập tức
                // Không cần user phải đăng xuất rồi đăng nhập lại thủ công
                var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
                int stampUpdated = 0;
                foreach (var roleUser in usersInRole)
                {
                    var stampResult = await _userManager.UpdateSecurityStampAsync(roleUser);
                    if (stampResult.Succeeded) stampUpdated++;
                    else _logger.LogWarning("Không thể cập nhật SecurityStamp cho user {UserId}", roleUser.Id);
                }
                _logger.LogInformation(
                    "SavePermissions: Cập nhật SecurityStamp cho {Count}/{Total} user trong role '{Role}'",
                    stampUpdated, usersInRole.Count, roleName);

                // Bước 4 — Ghi AuditLog
                await _logService.LogActionAsync(
                    username:    User.Identity?.Name ?? "Admin",
                    actionType:  "Update",
                    module:      "PhanQuyen",
                    description: $"Cập nhật Policy Claims cho Role '{roleName}': {validPerms.Count} quyền. SecurityStamp đã reset cho {stampUpdated}/{usersInRole.Count} user.",
                    entityId:    identityRole.Id,
                    entityName:  roleName,
                    oldValue:    $"{permissionClaims.Count} claims cũ",
                    newValue:    string.Join(", ", validPerms),
                    userRole:    "Admin"
                );

                _logger.LogInformation(
                    "SavePermissions: Role '{Role}' — {Count} permissions saved.", roleName, validPerms.Count);

                TempData["Success"] = $"Đã lưu {validPerms.Count} quyền cho role '{roleName}'. Phiên đăng nhập của {stampUpdated} người dùng sẽ được cập nhật ngay.";
                return RedirectToAction(nameof(ManagePermissions), new { roleName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving permissions for role {RoleName}", roleName);
                TempData["Error"] = "Lỗi khi lưu quyền, vui lòng thử lại";
                return RedirectToAction(nameof(ManagePermissions), new { roleName });
            }
        }

        /// <summary>
        /// API JSON: Trả về danh sách Permission claims hiện tại của một role.
        /// GET: /admin/role/get-permissions/{roleName}
        /// </summary>
        [HttpGet]
        [Route("get-permissions/{roleName}")]
        public async Task<IActionResult> GetPermissions(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null) return Json(new { success = false, message = "Role not found" });

            var claims = await _roleManager.GetClaimsAsync(role);
            var perms  = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList();
            return Json(new { success = true, roleName, permissions = perms });
        }
    }
}

