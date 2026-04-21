using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using HDKTech.Areas.Admin.Models;
using HDKTech.Areas.Admin.Repositories;
using HDKTech.Areas.Admin.ViewModels;
using HDKTech.Repositories.Interfaces;
using HDKTech.Services;

namespace HDKTech.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireAdmin")]
    [Route("admin/banner")]
    public class BannerController : Controller
    {
        private readonly BannerRepository _bannerRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<BannerController> _logger;
        private readonly ISystemLogService _logService;

        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        private const long MaxFileSizeBytes = 10 * 1024 * 1024;
        private const string BannerImageFolder = "images/banners";

        public BannerController(
            BannerRepository bannerRepository,
            ICategoryRepository categoryRepository,
            IWebHostEnvironment webHostEnvironment,
            ILogger<BannerController> logger,
            ISystemLogService logService)
        {
            _bannerRepository = bannerRepository;
            _categoryRepository = categoryRepository;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _logService = logService;
        }

        /// <summary>Đổ danh sách danh mục cha vào ViewBag để dùng trong dropdown.</summary>
        private async Task PopulateCategoriesViewBagAsync(int? selectedId = null)
        {
            var categories = await _categoryRepository.GetParentCategoriesAsync();
            ViewBag.Categories = new SelectList(
                categories.OrderBy(c => c.Name),
                "Id", "Name", selectedId);
        }

        // ============================================================
        // INDEX
        // ============================================================
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var banners = (await _bannerRepository.GetAllBannersAsync()).ToList();

            var vm = new BannerIndexViewModel
            {
                Banners = banners,
                TotalBanners = banners.Count,
                ActiveBanners = banners.Count(b => b.IsActive),
                InactiveBanners = banners.Count(b => !b.IsActive),
                MainBanners = banners.Count(b => b.BannerType == "Main"),
                SideBanners = banners.Count(b => b.BannerType == "Side")
            };

            return View(vm);
        }

        // ============================================================
        // CREATE
        // ============================================================
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            await PopulateCategoriesViewBagAsync();
            return View(new Banner { IsActive = true, DisplayOrder = 0 });
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Banner banner, IFormFile? imageFile)
        {
            // Xóa lỗi validation mặc định của các navigation property
            ModelState.Remove(nameof(banner.ImageUrl));
            ModelState.Remove(nameof(banner.Category));

            // [1] Upload file → ưu tiên hơn URL ─────────────────────
            if (imageFile is { Length: > 0 })
            {
                var (ok, msg, url) = await SaveBannerImageAsync(imageFile);
                if (!ok)
                {
                    ModelState.AddModelError("ImageUrl", msg);
                    return View(banner);
                }
                banner.ImageUrl = url;
            }

            // [2] Không có file → kiểm tra URL field ─────────────────
            if (string.IsNullOrWhiteSpace(banner.ImageUrl))
            {
                ModelState.AddModelError("ImageUrl",
                    "Vui lòng tải ảnh lên hoặc nhập URL hình ảnh.");
                return View(banner);
            }

            // [3] Chuẩn hóa LinkUrl ───────────────────────────────────
            banner.LinkUrl = NormalizeToRelativePath(banner.LinkUrl);

            if (!ModelState.IsValid)
            {
                await PopulateCategoriesViewBagAsync(banner.CategoryId);
                return View(banner);
            }

            try
            {
                // CategoryId chỉ có ý nghĩa với Side banner
                if (banner.BannerType != "Side") banner.CategoryId = null;
                banner.CreatedAt = DateTime.Now;
                await _bannerRepository.CreateBannerAsync(banner);
                _logger.LogInformation("Banner '{Title}' created", banner.Title);

                await _logService.LogActionAsync(
                    username: User.Identity?.Name ?? "Admin",
                    actionType: "Create",
                    module: "Banner",
                    description: $"Tạo mới Banner: '{banner.Title}' (Type: {banner.BannerType})",
                    entityId: banner.Id.ToString(),
                    entityName: banner.Title,
                    newValue: $"ImageUrl={banner.ImageUrl}, IsActive={banner.IsActive}",
                    userRole: "Admin");

                TempData["Success"] = $"Tạo banner '{banner.Title}' thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating banner");
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo banner. Vui lòng thử lại.");
                return View(banner);
            }
        }

        // ============================================================
        // EDIT
        // ============================================================
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var banner = await _bannerRepository.GetBannerByIdAsync(id);
            if (banner == null) return NotFound();
            await PopulateCategoriesViewBagAsync(banner.CategoryId);
            return View(banner);
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Banner banner, IFormFile? imageFile)
        {
            if (id != banner.Id) return BadRequest();

            // Xóa lỗi validation mặc định trước
            ModelState.Remove(nameof(banner.ImageUrl));
            ModelState.Remove(nameof(banner.Category));

            // [1] Lấy bản ghi hiện tại ────────────────────────────────
            var existing = await _bannerRepository.GetBannerByIdAsync(id);
            if (existing == null) return NotFound();

            string oldSnapshot = $"Title={existing.Title}, ImageUrl={existing.ImageUrl}, IsActive={existing.IsActive}";

            // [2] Quyết định ImageUrl ─────────────────────────────────
            //   - Có file upload → dùng file (bỏ qua text field)
            //   - Không có file, text field có giá trị → dùng text field
            //   - Không có gì → giữ nguyên ảnh cũ
            if (imageFile is { Length: > 0 })
            {
                var (ok, msg, url) = await SaveBannerImageAsync(imageFile);
                if (!ok)
                {
                    ModelState.AddModelError("ImageUrl", msg);
                    return View(banner);
                }
                banner.ImageUrl = url;
            }
            else if (string.IsNullOrWhiteSpace(banner.ImageUrl))
            {
                // Giữ ảnh cũ — không báo lỗi
                banner.ImageUrl = existing.ImageUrl;
            }
            // else: người dùng xóa ảnh cũ và nhập URL mới → dùng URL mới

            // [3] Chuẩn hóa LinkUrl ───────────────────────────────────
            banner.LinkUrl = NormalizeToRelativePath(banner.LinkUrl);

            if (!ModelState.IsValid)
            {
                await PopulateCategoriesViewBagAsync(banner.CategoryId);
                return View(banner);
            }

            try
            {
                // ── FIX EDIT: Copy từng field sang existing thay vì
                //   truyền object mới vào Update (tránh EF tracking conflict)
                existing.Title = banner.Title;
                existing.ImageUrl = banner.ImageUrl;
                existing.LinkUrl = banner.LinkUrl;
                existing.Description = banner.Description;
                existing.BannerType = banner.BannerType;
                existing.DisplayOrder = banner.DisplayOrder;
                existing.IsActive = banner.IsActive;
                existing.StartDate = banner.StartDate;
                existing.EndDate = banner.EndDate;
                // CategoryId chỉ có ý nghĩa với Side banner
                existing.CategoryId = (banner.BannerType == "Side") ? banner.CategoryId : null;
                existing.UpdatedAt = DateTime.Now;

                await _bannerRepository.UpdateBannerAsync(existing);
                _logger.LogInformation("Banner '{Title}' updated", existing.Title);

                await _logService.LogActionAsync(
                    username: User.Identity?.Name ?? "Admin",
                    actionType: "Update",
                    module: "Banner",
                    description: $"Cập nhật Banner: '{existing.Title}'",
                    entityId: id.ToString(),
                    entityName: existing.Title,
                    oldValue: oldSnapshot,
                    newValue: $"Title={existing.Title}, ImageUrl={existing.ImageUrl}, IsActive={existing.IsActive}",
                    userRole: "Admin");

                TempData["Success"] = $"Cập nhật banner '{existing.Title}' thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating banner");
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật banner. Vui lòng thử lại.");
                return View(banner);
            }
        }

        // ============================================================
        // DETAILS
        // ============================================================
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var banner = await _bannerRepository.GetBannerByIdAsync(id);
            if (banner == null) return NotFound();
            return View(banner);
        }

        // ============================================================
        // DELETE
        // ============================================================
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var banner = await _bannerRepository.GetBannerByIdAsync(id);
                if (banner != null)
                {
                    await _bannerRepository.DeleteBannerAsync(id);

                    await _logService.LogActionAsync(
                        username: User.Identity?.Name ?? "Admin",
                        actionType: "Delete",
                        module: "Banner",
                        description: $"Xoá Banner: '{banner.Title}' (ID: {id})",
                        entityId: id.ToString(),
                        entityName: banner.Title,
                        oldValue: $"ImageUrl={banner.ImageUrl}, IsActive={banner.IsActive}",
                        userRole: "Admin");

                    TempData["Success"] = $"Xoá banner '{banner.Title}' thành công!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting banner {Id}", id);
                TempData["Error"] = "Lỗi khi xoá banner. Vui lòng thử lại.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // TOGGLE ACTIVE (AJAX)
        // ============================================================
        [HttpPost("ToggleActive")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive([FromBody] ToggleActiveRequest req)
        {
            try
            {
                var banner = await _bannerRepository.GetBannerByIdAsync(req.Id);
                if (banner == null) return NotFound();

                string oldStatus = banner.IsActive ? "Active" : "Inactive";
                banner.IsActive = req.IsActive;
                banner.UpdatedAt = DateTime.Now;
                await _bannerRepository.UpdateBannerAsync(banner);

                await _logService.LogActionAsync(
                    username: User.Identity?.Name ?? "Admin",
                    actionType: "Update",
                    module: "Banner",
                    description: $"Đổi trạng thái Banner '{banner.Title}': {oldStatus} → {(req.IsActive ? "Active" : "Inactive")}",
                    entityId: req.Id.ToString(),
                    entityName: banner.Title,
                    oldValue: oldStatus,
                    newValue: req.IsActive ? "Active" : "Inactive",
                    userRole: "Admin");

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling banner {Id}", req.Id);
                return BadRequest(new { success = false });
            }
        }

        // ============================================================
        // PRIVATE HELPERS
        // ============================================================

        private async Task<(bool ok, string msg, string url)> SaveBannerImageAsync(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName);

            if (!AllowedExtensions.Contains(ext))
                return (false,
                    $"Định dạng '{ext}' không được hỗ trợ. Chỉ chấp nhận JPG, PNG, WebP, GIF.",
                    null);

            if (file.Length > MaxFileSizeBytes)
                return (false, "Kích thước ảnh vượt quá 10 MB.", null);

            var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, BannerImageFolder);
            Directory.CreateDirectory(uploadPath);

            var safeFile = $"banner_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
            var physicalPath = Path.Combine(uploadPath, safeFile);

            try
            {
                await using var fs = new FileStream(physicalPath, FileMode.Create, FileAccess.Write);
                await file.CopyToAsync(fs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save banner image {Name}", safeFile);
                return (false, "Lỗi hệ thống khi lưu file. Vui lòng thử lại.", null);
            }

            return (true, null, $"/{BannerImageFolder}/{safeFile}");
        }

        private static string NormalizeToRelativePath(string rawLink)
        {
            if (string.IsNullOrWhiteSpace(rawLink)) return null;

            rawLink = rawLink.Trim();

            if (rawLink.StartsWith("/")) return rawLink;

            if (Uri.TryCreate(rawLink, UriKind.Absolute, out var uri))
                return uri.PathAndQuery + uri.Fragment;

            return rawLink;
        }
    }

    public class ToggleActiveRequest
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
    }
}