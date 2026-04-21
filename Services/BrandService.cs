using HDKTech.Models;
using HDKTech.Repositories.Interfaces;
using HDKTech.Services.Interfaces;

namespace HDKTech.Services
{
    public class BrandService : IBrandService
    {
        private readonly IBrandRepository _brandRepo;

        public BrandService(IBrandRepository brandRepo)
        {
            _brandRepo = brandRepo;
        }

        public async Task<(Brand? Brand, List<Product> Products)> GetBrandPageAsync(string slug)
        {
            var brand = await _brandRepo.GetByNameWithProductsAsync(slug);
            if (brand == null) return (null, new List<Product>());

            var products = brand.Products?
                .OrderByDescending(p => p.Id)
                .ToList() ?? new List<Product>();

            return (brand, products);
        }
    }
}
