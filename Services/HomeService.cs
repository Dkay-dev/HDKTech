using HDKTech.Areas.Admin.Repositories;
using HDKTech.Models;
using HDKTech.Repositories.Interfaces;
using HDKTech.Services.Interfaces;
using HDKTech.ViewModels;

namespace HDKTech.Services
{
    public class HomeService : IHomeService
    {
        private readonly IProductRepository _productRepo;
        private readonly ICategoryRepository _categoryRepo;
        private readonly BannerRepository _bannerRepo;
        private readonly IProductService _productService;

        public HomeService(
            IProductRepository productRepo,
            ICategoryRepository categoryRepo,
            BannerRepository bannerRepo,
            IProductService productService)
        {
            _productRepo    = productRepo;
            _categoryRepo   = categoryRepo;
            _bannerRepo     = bannerRepo;
            _productService = productService;
        }

        public async Task<HomeIndexViewModel> GetHomePageDataAsync()
        {
            var categories    = await _categoryRepo.GetAllAsync();
            var activeBanners = (await _bannerRepo.GetActiveBannersAsync()).ToList();

            var flashSaleProducts = await _productRepo.GetFlashSaleProductsAsync(limit: 5);
            var topSellerProducts = await _productRepo.GetTopSellerProductsAsync(limit: 8);
            var newProducts       = await _productRepo.GetNewProductsAsync(limit: 6);

            // Performance fix: GetPagedAsync(12) thay vì GetAllWithImagesAsync()
            var (pagedProducts, _) = await _productRepo.GetPagedAsync(page: 1, pageSize: 12);

            var flashSaleEndTime = await _productService.GetFlashSaleEndTimeAsync();

            return new HomeIndexViewModel
            {
                FlashSaleProducts = flashSaleProducts,
                TopSellerProducts = topSellerProducts,
                NewProducts       = newProducts,
                AllProducts       = pagedProducts,
                FlashSaleEndTime  = flashSaleEndTime,

                Categories = categories
                    .Where(c => c.ParentCategoryId == null)
                    .OrderBy(c => c.Id)
                    .ToList(),

                MainBanners = activeBanners
                    .Where(b => b.BannerType == "Main")
                    .OrderBy(b => b.DisplayOrder)
                    .ToList(),

                SideBanners = activeBanners
                    .Where(b => b.BannerType == "Side")
                    .OrderBy(b => b.DisplayOrder)
                    .ToList(),

                BottomBanners = activeBanners
                    .Where(b => b.BannerType == "Bottom")
                    .OrderBy(b => b.DisplayOrder)
                    .ToList()
            };
        }

        public async Task<(IEnumerable<Category> Categories, IEnumerable<Product> Products)> GetDiagnosticDataAsync()
        {
            var categories = await _categoryRepo.GetAllAsync();
            var products   = await _productRepo.GetAllWithImagesAsync();
            return (categories, products);
        }
    }
}
