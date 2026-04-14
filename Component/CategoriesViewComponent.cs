using HDKTech.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Repositories.Component
{
    public class CategoriesViewComponent : ViewComponent
    {
        private readonly HDKTechContext _DbContext;

        public CategoriesViewComponent(HDKTechContext DbContext)
        {
            _DbContext = DbContext;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Lấy TOÀN BỘ danh mục về máy khách (Client-side)
            // Việc lọc Cha/Con/Cháu sẽ để cho file View (.cshtml) xử lý bằng Model.Where
            var allCategories = await _DbContext.Categories.ToListAsync();

            return View(allCategories);
        }
    }
}
