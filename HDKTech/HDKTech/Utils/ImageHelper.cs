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
