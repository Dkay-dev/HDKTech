using System;
using System.IO;

namespace HDKTech.Utils
{
    /// <summary>
    /// Helper tập trung cho xử lý đường dẫn ảnh sản phẩm
    /// Dùng chung toàn dự án để tránh dup logic
    /// </summary>
    public static class ImageHelper
    {
        /// <summary>
        /// Trả về đường dẫn đầy đủ tới ảnh product trong /images/products.
        /// - Nếu ImageUrl null/empty => "/images/products/no-image.png"
        /// - Nếu đã là đường dẫn tuyệt đối hoặc ImageUrl đầy đủ => chuẩn hoá extension -> .jpg
        /// - Nếu là "folder/file.ext" => "/images/products/folder/file.jpg"
        /// - Nếu chỉ "file.ext" + có categoryFolder => "/images/products/{categoryFolder}/{file}.jpg"
        /// </summary>
        public static string GetImagePath(string? ImageUrl, string? categoryFolder = null)
        {
            const string fallback = "/images/products/no-image.png";

            if (string.IsNullOrWhiteSpace(ImageUrl))
                return fallback;

            ImageUrl = ImageUrl.Trim();

            // ✅ Fix #3: Nếu ImageUrl đã là đường dẫn đầy đủ (tuyệt đối hoặc /images/...)
            // => GIỮ NGUYÊN extension thực tế (.jpg, .png, .webp, .gif)
            // BUG CŨ: luôn đổi extension thành .jpg => ảnh .png/.webp bị broken
            if (ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                ImageUrl.StartsWith("/images/", StringComparison.OrdinalIgnoreCase))
            {
                // Trả về nguyên đường dẫn, không cần chuyển đổi gì thêm
                return ImageUrl;
            }

            // ImageUrl kiểu "folder/file.jpg" hoặc "file.jpg"
            var cleaned = ImageUrl.Replace('\\', '/').TrimStart('/');

            // ✅ Fix #3: Giữ nguyên extension thực tế của file thay vì cứng .jpg
            // Kiểm tra xem có folder không
            if (cleaned.Contains("/"))
            {
                var folder   = System.IO.Path.GetDirectoryName(cleaned)?.Replace("\\", "/") ?? "";
                var fileName = System.IO.Path.GetFileName(cleaned); // giữ cả extension gốc

                if (string.IsNullOrWhiteSpace(folder))
                    return $"/images/products/{fileName}";

                return $"/images/products/{folder}/{fileName}";
            }
            else
            {
                // Chỉ file name => cần categoryFolder để xác định folder
                var fileName = cleaned; // giữ nguyên tên file + extension gốc

                if (!string.IsNullOrWhiteSpace(categoryFolder))
                {
                    // Chuẩn hoá categoryFolder: lowercase, space -> dash
                    var folder = categoryFolder.Trim().ToLower().Replace(" ", "-");
                    return $"/images/products/{folder}/{fileName}";
                }
                
                // Fallback: không có folder info - giữ nguyên tên file
                return $"/images/products/{fileName}";
            }
        }

        /// <summary>
        /// Trả về đường dẫn ảnh mặc định cho danh mục dựa theo tên.
        /// Ưu tiên: Admin upload (BannerImageUrl) → gọi hàm này làm fallback → icon.
        /// </summary>
        public static string? GetCategoryImageUrl(string? categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName)) return null;
            var name = categoryName.Trim().ToLower();
            return name switch
            {
                "laptop"                  => "/images/categories/Laptop.jpg",
                "laptop gaming"           => "/images/categories/LaptopGaming.jpg",
                "pc gvn"                  => "/images/categories/PC_GVN.png",
                "main, cpu, vga"          => "/images/categories/main.jpg",
                "case, nguồn, tản"        => "/images/categories/case.jpg",
                "ổ cứng, ram, thẻ nhớ"   => "/images/categories/RAM.jpg",
                "loa, micro, webcam"      => "/images/categories/Loa.jpg",
                "màn hình"                => "/images/categories/Manhinh.jpg",
                "bàn phím"                => "/images/categories/banphim.jpg",
                "chuột + lót chuột"       => "/images/categories/chuot.jpg",
                "tai nghe"                => "/images/categories/tainghe.jpg",
                "handheld, console"       => "/images/categories/console.jpg",
                // fallback: tên cũ / rút gọn vẫn match được
                _ when name.StartsWith("chuột")    => "/images/categories/chuot.jpg",
                _ when name.StartsWith("handheld") => "/images/categories/console.jpg",
                _                                  => null
            };
        }

        /// <summary>
        /// Map danh mục tiếng Việt -> tên folder ảnh
        /// Dùng cho khi cần convert Category.Name -> folder name
        /// </summary>
        public static string MapCategoryToFolder(string? categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                return "accessories";

            return categoryName.ToLower().Trim() switch
            {
                // Laptop (2 danh mục)
                "laptop" => "laptops",
                "laptop gaming" => "laptops-gaming",

                // Components & Parts (3 danh mục)
                "main, cpu, vga" => "components",
                "case, nguồn, tản" => "components",
                "ổ cứng, ram, thẻ nhớ" => "storage",

                // Peripherals & Accessories (6 danh mục)
                "loa, micro, webcam" => "audio",
                "màn hình" => "monitor",
                "bàn phím" => "peripherals",
                "chuột + lót chuột" => "peripherals",
                "tai nghe" => "audio",
                "ghế - bàn" => "furniture",

                // Others (2 danh mục)
                "handheld, console" => "handheld",
                "dịch vụ và thông tin khác" => "services",

                // PC GVN
                "pc gvn" => "pc-builds",

                // Default
                _ => "accessories"
            };
        }
    }
}
