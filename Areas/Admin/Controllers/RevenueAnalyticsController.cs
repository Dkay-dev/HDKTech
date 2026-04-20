using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HDKTech.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireAdminArea")]
    [Route("admin/[controller]")]
    public class RevenueAnalyticsController : Controller
    {
        [HttpGet("")]
        [HttpGet("index")]
        [Authorize(Policy = "RevenueAnalytics.Read")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
