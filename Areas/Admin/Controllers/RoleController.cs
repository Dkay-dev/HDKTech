using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Areas.Admin.ViewModels;
using HDKTech.Services;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [Route("admin/[controller]")]
    public class RoleController : Controller
    {
        private readonly HDKTechContext _context;
        private readonly ILogger<RoleController> _logger;
        private readonly ISystemLogService _logService;

        public RoleController(HDKTechContext context, ILogger<RoleController> logger, ISystemLogService logService)
        {
            _context    = context;
            _logger     = logger;
            _logService = logService;
        }

        /// <summary>
        /// Danh sách vai trò có phân trang, tìm kiếm.
        /// GET: /admin/role
        /// </summary>
        [HttpGet]
        [Route("")]
        [Route("index")]
        public async Task<IActionResult> Index(string searchTerm = "", int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                IQueryable<Role> query = _context.Roles
                    .AsNoTracking()
                    .Include(r => r.RolePermissions);

                if (!string.IsNullOrEmpty(searchTerm))
                    query = query.Where(r =>
                        r.RoleName.Contains(searchTerm) ||
                        r.Description.Contains(searchTerm));

                var totalCount = await query.CountAsync();
                var roles      = await query
                    .OrderBy(r => r.RoleName)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var vm = new RoleIndexViewModel
                {
                    Roles            = roles,
                    TotalRoles       = await _context.Roles.AsNoTracking().CountAsync(),
                    ActiveRoles      = await _context.Roles.AsNoTracking().CountAsync(r => r.IsActive),
                    TotalPermissions = await _context.Permissions.AsNoTracking().CountAsync(),
                    CurrentPage      = pageNumber,
                    PageSize         = pageSize,
                    TotalPages       = (int)Math.Ceiling((double)totalCount / pageSize),
                    SearchTerm       = searchTerm
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading roles");
                TempData["Error"] = "Lỗi khi tải danh sách vai trò";
                return View(new RoleIndexViewModel());
            }
        }

        /// <summary>
        /// Chi tiết vai trò + quyền hạn.
        /// GET: /admin/role/details/1
        /// </summary>
        [HttpGet]
        [Route("details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var role = await _context.Roles
                    .Include(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(r => r.RoleId == id);

                if (role == null)
                {
                    TempData["Error"] = "Không tìm thấy vai trò";
                    return RedirectToAction("Index");
                }

                var allPermissions = await _context.Permissions
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Module).ThenBy(p => p.Action)
                    .ToListAsync();

                ViewBag.Role                = role;
                ViewBag.AllPermissions      = allPermissions;
                ViewBag.RolePermissionIds   = role.RolePermissions.Select(rp => rp.PermissionId).ToList();
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading role details");
                TempData["Error"] = "Lỗi khi tải chi tiết vai trò";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Tạo vai trò mới.
        /// GET: /admin/role/create
        /// </summary>
        [HttpGet]
        [Route("create")]
        public async Task<IActionResult> Create()
        {
            try
            {
                ViewBag.Permissions = await _context.Permissions
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Module).ThenBy(p => p.Action)
                    .ToListAsync();
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create page");
                TempData["Error"] = "Lỗi khi tải trang tạo vai trò";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Lưu vai trò mới + ghi AuditLog.
        /// POST: /admin/role/create
        /// </summary>
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create(Role role, [FromForm] List<int> selectedPermissions)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Permissions = await _context.Permissions.AsNoTracking().ToListAsync();
                    return View(role);
                }

                _context.Roles.Add(role);
                await _context.SaveChangesAsync();

                if (selectedPermissions?.Any() == true)
                {
                    var rolePermissions = selectedPermissions.Select(pId => new RolePermission
                    {
                        RoleId = role.RoleId, PermissionId = pId
                    }).ToList();
                    _context.RolePermissions.AddRange(rolePermissions);
                    await _context.SaveChangesAsync();
                }

                // ── AUTO AUDIT LOG ─────────────────────────────────────────────
                await _logService.LogActionAsync(
                    username:    User.Identity?.Name ?? "Admin",
                    actionType:  "Create",
                    module:      "PhanQuyen",
                    description: $"Tạo mới Vai trò: '{role.RoleName}'",
                    entityId:    role.RoleId.ToString(),
                    entityName:  role.RoleName,
                    newValue:    $"Permissions: {selectedPermissions?.Count ?? 0}",
                    userRole:    "Admin"
                );

                TempData["Success"] = $"Tạo vai trò '{role.RoleName}' thành công";
                return RedirectToAction("Details", new { id = role.RoleId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role");
                TempData["Error"] = "Lỗi khi tạo vai trò";
                ViewBag.Permissions = await _context.Permissions.AsNoTracking().ToListAsync();
                return View(role);
            }
        }

        /// <summary>
        /// Chỉnh sửa vai trò.
        /// GET: /admin/role/edit/1
        /// </summary>
        [HttpGet]
        [Route("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var role = await _context.Roles
                    .Include(r => r.RolePermissions)
                    .FirstOrDefaultAsync(r => r.RoleId == id);

                if (role == null)
                {
                    TempData["Error"] = "Không tìm thấy vai trò";
                    return RedirectToAction("Index");
                }

                ViewBag.Permissions          = await _context.Permissions.AsNoTracking()
                    .Where(p => p.IsActive).OrderBy(p => p.Module).ThenBy(p => p.Action).ToListAsync();
                ViewBag.SelectedPermissionIds = role.RolePermissions.Select(rp => rp.PermissionId).ToList();
                return View(role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit page");
                TempData["Error"] = "Lỗi khi tải trang chỉnh sửa";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Cập nhật vai trò + ghi AuditLog.
        /// POST: /admin/role/edit/1
        /// </summary>
        [HttpPost]
        [Route("edit/{id}")]
        public async Task<IActionResult> Edit(int id, Role role, [FromForm] List<int> selectedPermissions)
        {
            try
            {
                if (id != role.RoleId) return NotFound();

                if (!ModelState.IsValid)
                {
                    ViewBag.Permissions = await _context.Permissions.AsNoTracking().ToListAsync();
                    return View(role);
                }

                // Lấy dữ liệu cũ để ghi diff
                var oldRole = await _context.Roles.AsNoTracking()
                    .Include(r => r.RolePermissions)
                    .FirstOrDefaultAsync(r => r.RoleId == id);
                string oldValue = oldRole != null ? $"RoleName={oldRole.RoleName}, Permissions={oldRole.RolePermissions.Count}" : "N/A";

                _context.Roles.Update(role);
                await _context.SaveChangesAsync();

                var existingPerms = await _context.RolePermissions.Where(rp => rp.RoleId == id).ToListAsync();
                _context.RolePermissions.RemoveRange(existingPerms);
                await _context.SaveChangesAsync();

                if (selectedPermissions?.Any() == true)
                {
                    _context.RolePermissions.AddRange(selectedPermissions.Select(pId => new RolePermission
                    {
                        RoleId = role.RoleId, PermissionId = pId
                    }));
                    await _context.SaveChangesAsync();
                }

                // ── AUTO AUDIT LOG ─────────────────────────────────────────────
                await _logService.LogActionAsync(
                    username:    User.Identity?.Name ?? "Admin",
                    actionType:  "Update",
                    module:      "PhanQuyen",
                    description: $"Cập nhật Vai trò: '{role.RoleName}'",
                    entityId:    id.ToString(),
                    entityName:  role.RoleName,
                    oldValue:    oldValue,
                    newValue:    $"RoleName={role.RoleName}, Permissions={selectedPermissions?.Count ?? 0}",
                    userRole:    "Admin"
                );

                TempData["Success"] = $"Cập nhật vai trò '{role.RoleName}' thành công";
                return RedirectToAction("Details", new { id = role.RoleId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role");
                TempData["Error"] = "Lỗi khi cập nhật vai trò";
                ViewBag.Permissions = await _context.Permissions.AsNoTracking().ToListAsync();
                return View(role);
            }
        }

        /// <summary>
        /// Xoá vai trò + ghi AuditLog.
        /// POST: /admin/role/delete
        /// </summary>
        [HttpPost]
        [Route("delete")]
        public async Task<IActionResult> Delete(int roleId)
        {
            try
            {
                var role = await _context.Roles.FindAsync(roleId);
                if (role == null)
                    return Json(new { success = false, message = "Không tìm thấy vai trò" });

                var rolePerms = await _context.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
                _context.RolePermissions.RemoveRange(rolePerms);
                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();

                // ── AUTO AUDIT LOG ─────────────────────────────────────────────
                await _logService.LogActionAsync(
                    username:    User.Identity?.Name ?? "Admin",
                    actionType:  "Delete",
                    module:      "PhanQuyen",
                    description: $"Xoá Vai trò: '{role.RoleName}' (ID: {roleId})",
                    entityId:    roleId.ToString(),
                    entityName:  role.RoleName,
                    oldValue:    $"Permissions={rolePerms.Count}",
                    userRole:    "Admin"
                );

                return Json(new { success = true, message = $"Xóa vai trò '{role.RoleName}' thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role");
                return Json(new { success = false, message = "Lỗi khi xóa vai trò" });
            }
        }
    }
}
