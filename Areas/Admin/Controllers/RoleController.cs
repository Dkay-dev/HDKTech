using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HDKTech.Data;
using HDKTech.Models;
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

        public RoleController(HDKTechContext context, ILogger<RoleController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Display all roles with search and filter
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

                // Apply search filter
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(r => 
                        r.RoleName.Contains(searchTerm) ||
                        r.Description.Contains(searchTerm));
                }

                var totalCount = await query.CountAsync();
                var roles = await query
                    .OrderBy(r => r.RoleName)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.Roles = roles;
                ViewBag.TotalCount = totalCount;
                ViewBag.PageNumber = pageNumber;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                ViewBag.SearchTerm = searchTerm;

                // Summary statistics
                var totalRoles = await _context.Roles.AsNoTracking().CountAsync();
                var totalPermissions = await _context.Permissions.AsNoTracking().CountAsync();
                var activeRoles = await _context.Roles.AsNoTracking().Where(r => r.IsActive).CountAsync();

                ViewBag.TotalRoles = totalRoles;
                ViewBag.TotalPermissions = totalPermissions;
                ViewBag.ActiveRoles = activeRoles;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading roles");
                TempData["Error"] = "Lỗi khi tải danh sách vai trò";
                return View();
            }
        }

        /// <summary>
        /// Display role details with permissions
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
                    .OrderBy(p => p.Module)
                    .ThenBy(p => p.Action)
                    .ToListAsync();

                ViewBag.Role = role;
                ViewBag.AllPermissions = allPermissions;
                ViewBag.RolePermissionIds = role.RolePermissions.Select(rp => rp.PermissionId).ToList();

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
        /// Create new role
        /// GET: /admin/role/create
        /// </summary>
        [HttpGet]
        [Route("create")]
        public async Task<IActionResult> Create()
        {
            try
            {
                var permissions = await _context.Permissions
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Module)
                    .ThenBy(p => p.Action)
                    .ToListAsync();

                ViewBag.Permissions = permissions;
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
        /// Save new role
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
                    var permissions = await _context.Permissions.AsNoTracking().ToListAsync();
                    ViewBag.Permissions = permissions;
                    return View(role);
                }

                _context.Roles.Add(role);
                await _context.SaveChangesAsync();

                // Add permissions to role
                if (selectedPermissions != null && selectedPermissions.Any())
                {
                    var rolePermissions = selectedPermissions.Select(pId => new RolePermission
                    {
                        RoleId = role.RoleId,
                        PermissionId = pId
                    }).ToList();

                    _context.RolePermissions.AddRange(rolePermissions);
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = $"Tạo vai trò '{role.RoleName}' thành công";
                return RedirectToAction("Details", new { id = role.RoleId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role");
                TempData["Error"] = "Lỗi khi tạo vai trò";
                var permissions = await _context.Permissions.AsNoTracking().ToListAsync();
                ViewBag.Permissions = permissions;
                return View(role);
            }
        }

        /// <summary>
        /// Edit role
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

                var permissions = await _context.Permissions
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Module)
                    .ThenBy(p => p.Action)
                    .ToListAsync();

                ViewBag.Permissions = permissions;
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
        /// Update role
        /// POST: /admin/role/edit/1
        /// </summary>
        [HttpPost]
        [Route("edit/{id}")]
        public async Task<IActionResult> Edit(int id, Role role, [FromForm] List<int> selectedPermissions)
        {
            try
            {
                if (id != role.RoleId)
                {
                    return NotFound();
                }

                if (!ModelState.IsValid)
                {
                    var permissions = await _context.Permissions.AsNoTracking().ToListAsync();
                    ViewBag.Permissions = permissions;
                    return View(role);
                }

                // Update role
                _context.Roles.Update(role);
                await _context.SaveChangesAsync();

                // Update permissions
                var existingPermissions = await _context.RolePermissions
                    .Where(rp => rp.RoleId == id)
                    .ToListAsync();

                _context.RolePermissions.RemoveRange(existingPermissions);
                await _context.SaveChangesAsync();

                if (selectedPermissions != null && selectedPermissions.Any())
                {
                    var rolePermissions = selectedPermissions.Select(pId => new RolePermission
                    {
                        RoleId = role.RoleId,
                        PermissionId = pId
                    }).ToList();

                    _context.RolePermissions.AddRange(rolePermissions);
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = $"Cập nhật vai trò '{role.RoleName}' thành công";
                return RedirectToAction("Details", new { id = role.RoleId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role");
                TempData["Error"] = "Lỗi khi cập nhật vai trò";
                var permissions = await _context.Permissions.AsNoTracking().ToListAsync();
                ViewBag.Permissions = permissions;
                return View(role);
            }
        }

        /// <summary>
        /// Delete role
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
                {
                    return Json(new { success = false, message = "Không tìm thấy vai trò" });
                }

                // Remove role permissions first
                var rolePermissions = await _context.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .ToListAsync();

                _context.RolePermissions.RemoveRange(rolePermissions);
                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();

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
