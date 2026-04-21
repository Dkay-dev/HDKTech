using HDKTech.Models;

namespace HDKTech.Services.Interfaces
{
    public interface IBrandService
    {
        Task<(Brand? Brand, List<Product> Products)> GetBrandPageAsync(string slug);
    }
}
