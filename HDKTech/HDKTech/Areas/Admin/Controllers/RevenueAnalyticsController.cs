using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HDKTech.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireManager")]
    [Route("admin/[controller]")]
    public class RevenueAnalyticsController : Controller
    {
        [HttpGet("")]
        [HttpGet("index")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
