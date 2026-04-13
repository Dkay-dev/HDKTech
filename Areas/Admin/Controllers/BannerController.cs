using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HDKTech.Areas.Admin.Models;
using HDKTech.Areas.Admin.Repositories;
using HDKTech.Areas.Admin.ViewModels;
using HDKTech.Services;

namespace HDKTech.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [Route("admin/banner")]
    public class BannerController : Controller
    {
        private readonly BannerRepository _bannerRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<BannerController> _logger;
        private readonly ISystemLogService _logService;

        public BannerController(
            BannerRepository bannerRepository,
            IWebHostEnvironment webHostEnvironment,
            ILogger<BannerController> logger,
            ISystemLogService logService)
        {
            _bannerRepository = bannerRepository;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _logService = logService;
        }

        /// <summary>
        /// Danh sách Banner với thống kê.
        /// GET: /admin/banner
        /// </summary>
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var banners = (await _bannerRepository.GetAllBannersAsync()).ToList();

            var vm = new BannerIndexViewModel
            {
                Banners         = banners,
                TotalBanners    = banners.Count(),
                ActiveBanners   = banners.Count(b => b.IsActive),
                InactiveBanners = banners.Count(b => !b.IsActive),
                MainBanners     = banners.Count(b => b.BannerType == "Main"),
                SideBanners     = banners.Count(b => b.BannerType == "Side")
            };

            return View(vm);
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Banner banner, IFormFile? imageFile)
        {
            try
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    string fileName   = $"banner_{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(imageFile.FileName)}";
                    string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "banners");
                    Directory.CreateDirectory(uploadPath);

                    string filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await imageFile.CopyToAsync(stream);

                    banner.ImageUrl = $"/uploads/banners/{fileName}";
                }

                if (ModelState.IsValid)
                {
                    await _bannerRepository.CreateBannerAsync(banner);
                    _logger.LogInformation("Banner '{Title}' created", banner.Title);

                    // ── AUTO AUDIT LOG ─────────────────────────────────────────────
                    await _logService.LogActionAsync(
                        username:    User.Identity?.Name ?? "Admin",
                        actionType:  "Create",
                        module:      "Banner",
                        description: $"Tạo mới Banner: '{banner.Title}' (Type: {banner.BannerType})",
                        entityId:    banner.Id.ToString(),
                        entityName:  banner.Title,
                        newValue:    $"ImageUrl={banner.ImageUrl}, IsActive={banner.IsActive}",
                        userRole:    User.IsInRole("Admin") ? "Admin" : "Manager"
                    );

                    TempData["Success"] = $"Tạo banner '{banner.Title}' thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating banner");
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo banner");
            }

            return View(banner);
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var banner = await _bannerRepository.GetBannerByIdAsync(id);
            if (banner == null) return NotFound();
            return View(banner);
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Banner banner, IFormFile? imageFile)
        {
            if (id != banner.Id) return BadRequest();

            try
            {
                var existingBanner = await _bannerRepository.GetBannerByIdAsync(id);
                if (existingBanner == null) return NotFound();

                string oldValue = $"Title={existingBanner.Title}, IsActive={existingBanner.IsActive}";

                if (imageFile != null && imageFile.Length > 0)
                {
                    string fileName   = $"banner_{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(imageFile.FileName)}";
                    string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "banners");
                    Directory.CreateDirectory(uploadPath);

                    string filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await imageFile.CopyToAsync(stream);

                    banner.ImageUrl = $"/uploads/banners/{fileName}";
                }
                else
                {
                    banner.ImageUrl = existingBanner.ImageUrl;
                }

                banner.CreatedAt = existingBanner.CreatedAt;
                banner.UpdatedAt = DateTime.Now;

                if (ModelState.IsValid)
                {
                    await _bannerRepository.UpdateBannerAsync(banner);
                    _logger.LogInformation("Banner '{Title}' updated", banner.Title);

                    // ── AUTO AUDIT LOG ─────────────────────────────────────────────
                    await _logService.LogActionAsync(
                        username:    User.Identity?.Name ?? "Admin",
                        actionType:  "Update",
                        module:      "Banner",
                        description: $"Cập nhật Banner: '{banner.Title}'",
                        entityId:    id.ToString(),
                        entityName:  banner.Title,
                        oldValue:    oldValue,
                        newValue:    $"Title={banner.Title}, IsActive={banner.IsActive}",
                        userRole:    User.IsInRole("Admin") ? "Admin" : "Manager"
                    );

                    TempData["Success"] = $"Cập nhật banner '{banner.Title}' thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating banner");
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật banner");
            }

            return View(banner);
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var banner = await _bannerRepository.GetBannerByIdAsync(id);
            if (banner == null) return NotFound();
            return View(banner);
        }

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
                    _logger.LogInformation("Banner '{Title}' deleted", banner.Title);

                    // ── AUTO AUDIT LOG ─────────────────────────────────────────────
                    await _logService.LogActionAsync(
                        username:    User.Identity?.Name ?? "Admin",
                        actionType:  "Delete",
                        module:      "Banner",
                        description: $"Xoá Banner: '{banner.Title}' (ID: {id})",
                        entityId:    id.ToString(),
                        entityName:  banner.Title,
                        oldValue:    $"ImageUrl={banner.ImageUrl}, IsActive={banner.IsActive}",
                        userRole:    User.IsInRole("Admin") ? "Admin" : "Manager"
                    );

                    TempData["Success"] = $"Xoá banner '{banner.Title}' thành công!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting banner");
                TempData["Error"] = "Lỗi khi xoá banner";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("UpdateOrder")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrder([FromBody] List<(int BannerId, int Order)> orders)
        {
            try
            {
                await _bannerRepository.UpdateBannerOrderAsync(orders);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating banner order");
                return BadRequest();
            }
        }

        [HttpPost("ToggleActive")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive([FromBody] dynamic request)
        {
            try
            {
                int id = (int)request.id;
                bool isActive = (bool)request.IsActive;

                var banner = await _bannerRepository.GetBannerByIdAsync(id);
                if (banner == null) return NotFound();

                string oldStatus = banner.IsActive ? "Active" : "Inactive";
                banner.IsActive  = isActive;
                banner.UpdatedAt = DateTime.Now;
                await _bannerRepository.UpdateBannerAsync(banner);

                // ── AUTO AUDIT LOG ─────────────────────────────────────────────
                await _logService.LogActionAsync(
                    username:    User.Identity?.Name ?? "Admin",
                    actionType:  "Update",
                    module:      "Banner",
                    description: $"Đổi trạng thái Banner '{banner.Title}': {oldStatus} → {(isActive ? "Active" : "Inactive")}",
                    entityId:    id.ToString(),
                    entityName:  banner.Title,
                    oldValue:    oldStatus,
                    newValue:    isActive ? "Active" : "Inactive",
                    userRole:    User.IsInRole("Admin") ? "Admin" : "Manager"
                );

                _logger.LogInformation("Banner '{Title}' toggled to {Status}", banner.Title, isActive ? "active" : "inactive");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling banner");
                return BadRequest();
            }
        }
    }
}
