using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HDKTech.Models;
using HDKTech.Repositories;

namespace HDKTech.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [Route("admin/khuyenmai")]
    public class KhuyenMaiController : Controller
    {
        private readonly KhuyenMaiRepository _khuyenMaiRepository;
        private readonly ILogger<KhuyenMaiController> _logger;

        public KhuyenMaiController(
            KhuyenMaiRepository khuyenMaiRepository,
            ILogger<KhuyenMaiController> logger)
        {
            _khuyenMaiRepository = khuyenMaiRepository;
            _logger = logger;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var promotions = await _khuyenMaiRepository.GetAllPromotionsAsync();
            return View(promotions);
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KhuyenMai khuyenMai)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _khuyenMaiRepository.CreatePromotionAsync(khuyenMai);
                    _logger.LogInformation($"Promotion '{khuyenMai.TenChienDich}' created successfully");
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating promotion: {ex.Message}");
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo chiến dịch");
            }

            return View(khuyenMai);
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var khuyenMai = await _khuyenMaiRepository.GetPromotionByIdAsync(id);
            if (khuyenMai == null)
                return NotFound();

            return View(khuyenMai);
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, KhuyenMai khuyenMai)
        {
            if (id != khuyenMai.MaKhuyenMai)
                return BadRequest();

            try
            {
                var existingPromotion = await _khuyenMaiRepository.GetPromotionByIdAsync(id);
                if (existingPromotion == null)
                    return NotFound();

                khuyenMai.NgayTao = existingPromotion.NgayTao;
                khuyenMai.SoLuongSuDung = existingPromotion.SoLuongSuDung;

                if (ModelState.IsValid)
                {
                    await _khuyenMaiRepository.UpdatePromotionAsync(khuyenMai);
                    _logger.LogInformation($"Promotion '{khuyenMai.TenChienDich}' updated successfully");
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating promotion: {ex.Message}");
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật chiến dịch");
            }

            return View(khuyenMai);
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var khuyenMai = await _khuyenMaiRepository.GetPromotionByIdAsync(id);
            if (khuyenMai == null)
                return NotFound();

            return View(khuyenMai);
        }

        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var khuyenMai = await _khuyenMaiRepository.GetPromotionByIdAsync(id);
                if (khuyenMai != null)
                {
                    await _khuyenMaiRepository.DeletePromotionAsync(id);
                    _logger.LogInformation($"Promotion '{khuyenMai.TenChienDich}' deleted successfully");
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting promotion: {ex.Message}");
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
