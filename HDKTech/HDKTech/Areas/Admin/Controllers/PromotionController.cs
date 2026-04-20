using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HDKTech.Models;
using HDKTech.Areas.Admin.Models;
using HDKTech.Areas.Admin.Repositories;

namespace HDKTech.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireAdmin")]
    [Route("admin/promotion")]
    public class PromotionController : Controller
    {
        private readonly PromotionRepository _promotionRepository;
        private readonly ILogger<PromotionController> _logger;

        public PromotionController(
            PromotionRepository promotionRepository,
            ILogger<PromotionController> logger)
        {
            _promotionRepository = promotionRepository;
            _logger = logger;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var promotions = await _promotionRepository.GetAllPromotionsAsync();
            return View(promotions);
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Promotion promotion)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _promotionRepository.CreatePromotionAsync(promotion);
                    _logger.LogInformation($"Promotion '{promotion.CampaignName}' created successfully");
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating promotion: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while creating the promotion");
            }

            return View(promotion);
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var promotion = await _promotionRepository.GetPromotionByIdAsync(id);
            if (promotion == null)
                return NotFound();

            return View(promotion);
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Promotion promotion)
        {
            if (id != promotion.Id)
                return BadRequest();

            try
            {
                var existingPromotion = await _promotionRepository.GetPromotionByIdAsync(id);
                if (existingPromotion == null)
                    return NotFound();

                promotion.CreatedAt = existingPromotion.CreatedAt;
                promotion.UsageCount = existingPromotion.UsageCount;

                if (ModelState.IsValid)
                {
                    await _promotionRepository.UpdatePromotionAsync(promotion);
                    _logger.LogInformation($"Promotion '{promotion.CampaignName}' updated successfully");
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating promotion: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while updating the promotion");
            }

            return View(promotion);
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var promotion = await _promotionRepository.GetPromotionByIdAsync(id);
            if (promotion == null)
                return NotFound();

            return View(promotion);
        }

        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var promotion = await _promotionRepository.GetPromotionByIdAsync(id);
                if (promotion != null)
                {
                    await _promotionRepository.DeletePromotionAsync(id);
                    _logger.LogInformation($"Promotion '{promotion.CampaignName}' deleted successfully");
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
