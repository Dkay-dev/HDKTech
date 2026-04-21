using HDKTech.Models;
using HDKTech.ViewModels;

namespace HDKTech.Services.Interfaces
{
    public interface IHomeService
    {
        Task<HomeIndexViewModel> GetHomePageDataAsync();
        Task<(IEnumerable<Category> Categories, IEnumerable<Product> Products)> GetDiagnosticDataAsync();
    }
}
