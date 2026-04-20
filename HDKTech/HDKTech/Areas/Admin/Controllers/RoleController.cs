using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using HDKTech.ChucNangPhanQuyen;
using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Services;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Areas.Admin.Controllers
{
    /// <summary>
    /// View-model mô tả 1 dòng role trong trang danh sách.
    /// </summary>
    public class RoleListItem
    {
        public string Id              { get; set; } = "";
        public string Code            { get; set; } = "";
        public string Name            { get; set; } = "";
        public int    UserCount       { get; set; }
        public int    PermissionCount { get; set; }

        // Backward-compat cho view cũ (*.cshtml dùng RoleId/RoleName).
        public string RoleId   => Id;
        public string RoleName => Name;
        public string RoleCode => Code;
    }

    /// <summary>
    /// RoleController đã refactor sang ASP.NET Identity hoàn toàn.
    /// - Role   = <see cref="IdentityRole"/> (AspNetRoles).
    /// - Quyền  = <see cref="Claim"/> type "permission" gắn vào role (AspNetRoleClaims).
    /// </summary>
    [Area("Admin")]
    [Authorize(Policy = "RequireAdmin")]
    [Route("admin/[controller]")]
    public class RoleController : Controller
    {
        private readonly RoleManager<IdentityRole>  _roleManager;
        private readonly UserManager<AppUser>       _userManager;
        private readonly HDKTechContext             _db;
        private readonly ILogger<RoleController>    _logger;
        private readonly ISystemLogService          _logService;

        /// <summary>
        /// Bộ permission mà UI phân quyền cho phép toggle.
        /// Đồng bộ với <see cref="IdentityRoleSeed.AllPermissions"/>.
        /// </summary>
        public static IReadOnlyList<string> AllSystemPermissions =>
            IdentityRoleSeed.AllPermissions;

        private static readonly HashSet<string> ProtectedRoleNames =
            new(StringComparer.OrdinalIgnoreCase) { "Admin", "Manager", "Staff", "Customer" };

        public RoleController(
            RoleManager<IdentityRole> roleManager,
            UserManager<AppUser>      userManager,
            HDKTechContext            db,
            ILogger<RoleController>   logger,
            ISystemLogService         logService)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _db          = db;
            _logger      = logger;
            _logService  = logService;
        }

        // ─────────────────────────────────────────────────────────────
        // INDEX
        // ─────────────────────────────────────────────────────────────
        [HttpGet]
        [Route("")]
        [Route("index")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var roles = await _roleManager.Roles
                    .AsNoTracking()
                    .OrderBy(r => r.Name)
                    .ToListAsync();

                var list = new List<RoleListItem>();
                foreach (var r in roles)
                {
                    var usersInRole = await _userManager.GetUsersInRoleAsync(r.Name!);
                    var claims      = await _roleManager.GetClaimsAsync(r);

                    list.Add(new RoleListItem
                    {
                        Id              = r.Id,
                        Code            = r.Name ?? "",
                        Name            = r.Name ?? "",
                        UserCount       = usersInRole.Count,
                        PermissionCount = claims.Count(c => c.Type == PermissionHandler.PermissionClaimType)
                    });
                }

                return View(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading roles");
                TempData["Error"] = "Lỗi khi tải danh sách vai trò";
                return View(new List<RoleListItem>());
            }
        }

        // ─────────────────────────────────────────────────────────────
        // CREATE
        // ─────────────────────────────────────────────────────────────
        [HttpGet]
        [Route("create")]
        public IActionResult Create() => View();

        [HttpPost]
        [Route("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [FromForm] string roleName,
            [FromForm] string? roleCode = null)
        {
            roleName = roleName?.Trim() ?? "";
            // Với Identity, tên role = duy nhất, bỏ qua roleCode (giữ param để không phá form hiện có).

            if (string.IsNullOrWhiteSpace(roleName))
            {
                TempData["Error"] = "Tên vai trò không được để trống.";
                return View();
            }

            if (await _roleManager.RoleExistsAsync(roleName))
            {
                TempData["Error"] = $"Vai trò '{roleName}' đã tồn tại.";
                return View();
            }

            var role = new IdentityRole(roleName)
            {
                NormalizedName = roleName.ToUpperInvariant()
            };

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Không tạo được role: " +
                    string.Join("; ", result.Errors.Select(e => e.Description));
                return View();
            }

            await _logService.LogActionAsync(
                username   : User.Identity?.Name ?? "Admin",
                actionType : "Create",
                module     : "PhanQuyen",
                description: $"Tạo Role: '{roleName}'",
                entityId   : role.Id,
                entityName : roleName,
                userRole   : "Admin");

            TempData["Success"] = $"Tạo vai trò '{roleName}' thành công.";
            return RedirectToAction(nameof(ManagePermissions), new { roleCode = role.Name });
        }

        // ─────────────────────────────────────────────────────────────
        // DELETE
        // ─────────────────────────────────────────────────────────────
        [HttpPost]
        [Route("delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] string roleId)
        {
            if (string.IsNullOrWhiteSpace(roleId))
                return Json(new { success = false, message = "Thiếu RoleId" });

            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
                return Json(new { success = false, message = "Không tìm thấy vai trò" });

            if (ProtectedRoleNames.Contains(role.Name ?? ""))
                return Json(new { success = false, message = $"Không thể xoá role hệ thống '{role.Name}'" });

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            if (usersInRole.Count > 0)
                return Json(new
                {
                    success = false,
                    message = $"Role '{role.Name}' vẫn còn {usersInRole.Count} user đang sử dụng."
                });

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
                return Json(new
                {
                    success = false,
                    message = "Không xoá được: " +
                        string.Join("; ", result.Errors.Select(e => e.Description))
                });

            await _logService.LogActionAsync(
                username   : User.Identity?.Name ?? "Admin",
                actionType : "Delete",
                module     : "PhanQuyen",
                description: $"Xoá Role: '{role.Name}' (Id: {role.Id})",
                entityId   : role.Id,
                entityName : role.Name ?? "",
                userRole   : "Admin");

            return Json(new { success = true, message = $"Xóa vai trò '{role.Name}' thành công" });
        }

        // ─────────────────────────────────────────────────────────────
        // MANAGE PERMISSIONS
        // ─────────────────────────────────────────────────────────────
        [HttpGet]
        [Route("manage-permissions/{roleCode}")]
        public async Task<IActionResult> ManagePermissions(string roleCode)
        {
            var role = await _roleManager.FindByNameAsync(roleCode);
            if (role == null)
            {
                TempData["Error"] = $"Không tìm thấy Role '{roleCode}'";
                return RedirectToAction("Index");
            }

            var currentPerms = (await _roleManager.GetClaimsAsync(role))
                .Where(c => c.Type == PermissionHandler.PermissionClaimType)
                .Select(c => c.Value)
                .ToList();

            var grouped = AllSystemPermissions
                .GroupBy(p => p.Split('.')[0])
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.ToList());

            ViewBag.RoleCode           = role.Name;
            ViewBag.RoleName           = role.Name;
            ViewBag.RoleId             = role.Id;
            ViewBag.CurrentPermissions = currentPerms.ToHashSet(StringComparer.OrdinalIgnoreCase);
            ViewBag.GroupedPermissions = grouped;

            return View();
        }

        // ─────────────────────────────────────────────────────────────
        // SAVE PERMISSIONS
        // ─────────────────────────────────────────────────────────────
        [HttpPost]
        [Route("save-permissions/{roleCode}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePermissions(
            string roleCode,
            [FromForm] List<string> permissions)
        {
            var role = await _roleManager.FindByNameAsync(roleCode);
            if (role == null)
            {
                TempData["Error"] = $"Không tìm thấy Role '{roleCode}'";
                return RedirectToAction("Index");
            }

            var whitelist = (permissions ?? new List<string>())
                .Where(AllSystemPermissions.Contains)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var existing = (await _roleManager.GetClaimsAsync(role))
                .Where(c => c.Type == PermissionHandler.PermissionClaimType)
                .ToList();

            var desired = new HashSet<string>(whitelist, StringComparer.OrdinalIgnoreCase);

            // Xoá claim thừa
            foreach (var c in existing)
            {
                if (!desired.Contains(c.Value))
                    await _roleManager.RemoveClaimAsync(role, c);
            }

            var already = new HashSet<string>(
                existing.Select(c => c.Value),
                StringComparer.OrdinalIgnoreCase);

            // Thêm claim còn thiếu
            foreach (var perm in whitelist)
            {
                if (already.Contains(perm)) continue;
                await _roleManager.AddClaimAsync(role,
                    new Claim(PermissionHandler.PermissionClaimType, perm));
            }

            await _logService.LogActionAsync(
                username   : User.Identity?.Name ?? "Admin",
                actionType : "Update",
                module     : "PhanQuyen",
                description: $"Cập nhật Permissions cho Role '{role.Name}': {whitelist.Count} quyền.",
                entityId   : role.Id,
                entityName : role.Name ?? "",
                userRole   : "Admin");

            TempData["Success"] = $"Đã lưu {whitelist.Count} quyền cho role '{role.Name}'.";
            return RedirectToAction(nameof(ManagePermissions), new { roleCode = role.Name });
        }

        // ─────────────────────────────────────────────────────────────
        // GET PERMISSIONS (AJAX)
        // ─────────────────────────────────────────────────────────────
        [HttpGet]
        [Route("get-permissions/{roleCode}")]
        public async Task<IActionResult> GetPermissions(string roleCode)
        {
            var role = await _roleManager.FindByNameAsync(roleCode);
            if (role == null) return Json(new { success = false, message = "Role not found" });

            var perms = (await _roleManager.GetClaimsAsync(role))
                .Where(c => c.Type == PermissionHandler.PermissionClaimType)
                .Select(c => c.Value)
                .ToList();

            return Json(new { success = true, roleCode = role.Name, roleName = role.Name, permissions = perms });
        }
    }
}
