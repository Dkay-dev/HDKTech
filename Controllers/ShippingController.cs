using Microsoft.AspNetCore.Mvc;
using HDKTech.Models;

namespace HDKTech.Controllers
{
    public class ShippingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// API endpoint tính phí vận chuyển dựa theo tỉnh/thành phố.
        /// Gọi từ AJAX trên trang checkout khi người dùng chọn địa điểm.
        /// </summary>
        [HttpPost]
        public IActionResult CalculateFee([FromBody] ShippingFeeRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.CityName))
            {
                return Json(new ShippingFeeResponse
                {
                    Success = false,
                    Message = "Vui lòng chọn tỉnh/thành phố"
                });
            }

            var (fee, zone) = GetShippingFee(request.CityName);

            return Json(new ShippingFeeResponse
            {
                Success = true,
                Fee = fee,
                FeeFormatted = fee == 0 ? "Miễn phí" : fee.ToString("N0") + "₫",
                Zone = zone,
                Message = $"Phí vận chuyển đến {request.CityName}: " + (fee == 0 ? "Miễn phí" : fee.ToString("N0") + "₫")
            });
        }

        /// <summary>
        /// Tính phí ship dựa theo tên tỉnh/thành phố.
        /// Zone A (Miễn phí): Đà Nẵng — kho hàng chính.
        /// Zone B (20.000₫): Các tỉnh miền Trung lân cận.
        /// Zone C (30.000₫): Miền Nam.
        /// Zone D (35.000₫): Miền Bắc.
        /// </summary>
        private (decimal fee, string zone) GetShippingFee(string cityName)
        {
            if (string.IsNullOrEmpty(cityName))
                return (35000, "D");

            var name = cityName.ToLower()
                               .Replace("thành phố ", "")
                               .Replace("tỉnh ", "")
                               .Trim();

            // Zone A — Miễn phí (Đà Nẵng & khu vực kho)
            var zoneA = new[] { "đà nẵng", "da nang" };

            // Zone B — 20.000₫ (Miền Trung)
            var zoneB = new[]
            {
                "thừa thiên huế", "thừa thiên - huế", "huế",
                "quảng nam", "quảng ngãi", "bình định",
                "phú yên", "khánh hòa", "kon tum", "gia lai",
                "đắk lắk", "đắk nông", "quảng trị", "quảng bình",
                "hà tĩnh", "nghệ an"
            };

            // Zone C — 30.000₫ (Miền Nam)
            var zoneC = new[]
            {
                "hồ chí minh", "ho chi minh", "bình dương", "đồng nai",
                "bà rịa - vũng tàu", "bà rịa vũng tàu", "long an",
                "tiền giang", "bến tre", "trà vinh", "vĩnh long",
                "đồng tháp", "an giang", "kiên giang", "cần thơ",
                "hậu giang", "sóc trăng", "bạc liêu", "cà mau",
                "tây ninh", "bình phước", "ninh thuận", "bình thuận",
                "lâm đồng", "đà lạt"
            };

            foreach (var c in zoneA)
                if (name.Contains(c)) return (0, "A");

            foreach (var c in zoneB)
                if (name.Contains(c)) return (20000, "B");

            foreach (var c in zoneC)
                if (name.Contains(c)) return (30000, "C");

            // Zone D — 35.000₫ (Miền Bắc & các tỉnh còn lại)
            return (35000, "D");
        }
    }
}