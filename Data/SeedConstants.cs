namespace HDKTech.Data.Seeds
{
    /// <summary>
    /// Tập trung toàn bộ ID cố định để tránh Foreign Key conflict
    /// Đây là nguồn sự thật duy nhất cho mọi seed file
    /// </summary>
    public static class SeedConstants
    {
        // ─── Users ────────────────────────────────────────────────────────
        public const string AdminUserId = "00000000-0000-0000-0000-000000000001";
        public const string User1Id = "00000000-0000-0000-0000-000000000002";
        public const string User2Id = "00000000-0000-0000-0000-000000000003";
        public const string User3Id = "00000000-0000-0000-0000-000000000004";
        public const string User4Id = "00000000-0000-0000-0000-000000000005";
        public const string User5Id = "00000000-0000-0000-0000-000000000006";

        // ─── Brands (chỉ lưu ID của các brand cần dùng FK rõ ràng) ─────
        public const int BrandAsus = 1;
        public const int BrandMsi = 2;
        public const int BrandGigabyte = 3;
        public const int BrandLenovo = 4;
        public const int BrandAcer = 5;
        public const int BrandDell = 6;
        public const int BrandApple = 7;
        public const int BrandIntel = 8;
        public const int BrandAmd = 9;
        public const int BrandNvidia = 10;
        public const int BrandLogitech = 11;
        public const int BrandRazer = 12;
        public const int BrandSamsung = 13;
        public const int BrandCorsair = 14;
        public const int BrandKingston = 15;
        public const int BrandNzxt = 16;
        public const int BrandLianLi = 17;
        public const int BrandAkko = 18;
        public const int BrandEdifier = 19;
        public const int BrandHyperX = 20;
        public const int BrandSteelSeries = 21;
        public const int BrandLg = 22;
        public const int BrandBenq = 23;

        // ─── Root Categories ─────────────────────────────────────────────
        public const int CatLaptop = 1;
        public const int CatLaptopGaming = 2;
        public const int CatPcGvn = 3;
        public const int CatMainCpuVga = 4;
        public const int CatCaseNguonTan = 5;
        public const int CatStorageRam = 6;
        public const int CatLoaMicWebcam = 7;
        public const int CatManHinh = 8;
        public const int CatBanPhim = 9;
        public const int CatChuot = 10;
        public const int CatTaiNghe = 11;
        public const int CatHandheld = 12;

        // ─── Products (ID bands để tránh collision) ──────────────────────
        // Laptop:          1 – 10
        // Laptop Gaming:  11 – 20
        // PC GVN:         21 – 26
        // Components:     27 – 36
        // Peripherals:    37 – 60
        // Monitors:       61 – 70
    }
}