using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HDKTech.Models;
using HDKTech.Repositories;

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

        public BannerController(
            BannerRepository bannerRepository,
            IWebHostEnvironment webHostEnvironment,
            ILogger<BannerController> logger)
        {
            _bannerRepository = bannerRepository;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var banners = await _bannerRepository.GetAllBannersAsync();
            return View(banners);
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
                    string fileName = $"banner_{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(imageFile.FileName)}";
                    string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "banners");
                    Directory.CreateDirectory(uploadPath);

                    string filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    banner.ImageUrl = $"/uploads/banners/{fileName}";
                }

                if (ModelState.IsValid)
                {
                    await _bannerRepository.CreateBannerAsync(banner);
                    _logger.LogInformation($"Banner '{banner.TenBanner}' created successfully");
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating banner: {ex.Message}");
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo banner");
            }

            return View(banner);
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var banner = await _bannerRepository.GetBannerByIdAsync(id);
            if (banner == null)
                return NotFound();

            return View(banner);
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Banner banner, IFormFile? imageFile)
        {
            if (id != banner.MaBanner)
                return BadRequest();

            try
            {
                var existingBanner = await _bannerRepository.GetBannerByIdAsync(id);
                if (existingBanner == null)
                    return NotFound();

                if (imageFile != null && imageFile.Length > 0)
                {
                    string fileName = $"banner_{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(imageFile.FileName)}";
                    string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "banners");
                    Directory.CreateDirectory(uploadPath);

                    string filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    banner.ImageUrl = $"/uploads/banners/{fileName}";
                }
                else
                {
                    banner.ImageUrl = existingBanner.ImageUrl;
                }

                banner.NgayTao = existingBanner.NgayTao;

                if (ModelState.IsValid)
                {
                    await _bannerRepository.UpdateBannerAsync(banner);
                    _logger.LogInformation($"Banner '{banner.TenBanner}' updated successfully");
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating banner: {ex.Message}");
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật banner");
            }

            return View(banner);
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var banner = await _bannerRepository.GetBannerByIdAsync(id);
            if (banner == null)
                return NotFound();

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
                    _logger.LogInformation($"Banner '{banner.TenBanner}' deleted successfully");
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting banner: {ex.Message}");
                return RedirectToAction(nameof(Index));
            }
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
                _logger.LogError($"Error updating banner order: {ex.Message}");
                return BadRequest();
            }
        }

        [HttpPost("ToggleActive")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive([FromBody] dynamic request)
        {
            try
            {
                int id = request.id;
                bool isActive = request.isActive;

                var banner = await _bannerRepository.GetBannerByIdAsync(id);
                if (banner == null)
                    return NotFound();

                banner.IsActive = isActive;
                banner.NgayCapNhat = DateTime.Now;
                await _bannerRepository.UpdateBannerAsync(banner);

                _logger.LogInformation($"Banner '{banner.TenBanner}' toggled to {(isActive ? "active" : "inactive")}");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error toggling banner: {ex.Message}");
                return BadRequest();
            }
        }    }
}
