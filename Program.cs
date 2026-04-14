using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HDKTech.Models;
using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Models.Momo;
using HDKTech.Repositories;
using HDKTech.Repositories.Interfaces;
using HDKTech.Services;
using HDKTech.Services.Momo;
using HDKTech.Services.Vnpay;
using HDKTech.Areas.Admin.Repositories;
using HDKTech.Areas.Admin.Services;
using HDKTech.Utilities;
using HDKTech.ChucNangPhanQuyen;

namespace HDKTech
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("HDKTechContextConnection")
                ?? throw new InvalidOperationException("Connection string 'HDKTechContextConnection' not found.");

            builder.Services.AddDbContext<HDKTechContext>(options => options.UseSqlServer(connectionString));

            builder.Services
                 .AddIdentity<AppUser, IdentityRole>(options =>
                 {
                     // ── Password Policy ───────────────────────────────────────
                     options.SignIn.RequireConfirmedAccount = false;
                     options.Password.RequireDigit           = false;
                     options.Password.RequiredLength         = 4;
                     options.Password.RequireNonAlphanumeric = false;
                     options.Password.RequireUppercase       = false;
                     options.Password.RequireLowercase       = false;

                     // ── Giai đoạn 4: Brute-force Protection — Account Lockout ─
                     // Khoá tài khoản 15 phút sau 5 lần nhập sai liên tiếp.
                     // LockoutEnabled mặc định = true cho mọi user mới đăng ký.
                     options.Lockout.AllowedForNewUsers      = true;
                     options.Lockout.MaxFailedAccessAttempts = 5;
                     options.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(15);
                 })
                 .AddEntityFrameworkStores<HDKTechContext>()
                 .AddDefaultUI()
                 .AddDefaultTokenProviders();

            // ✅ Sửa cookie Identity để hoạt động với VNPay callback
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.HttpOnly = true;
            });

            // Register Repository Pattern
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<ProductRepository>();
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<CategoryRepository>();
            builder.Services.AddScoped<IBrandRepository, BrandRepository>();
            builder.Services.AddScoped<BrandRepository>();
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();

            // Register Admin Repositories
            // ✅ Fix #4: Đăng ký đúng namespace IAdminProductRepository mà Areas/Admin/Controllers/ProductController sử dụng
            // ProductController inject: HDKTech.Areas.Admin.Repositories.IAdminProductRepository
            // => Phải đăng ký interface đúng namespace, map tới implementation tương ứng
            builder.Services.AddScoped<HDKTech.Areas.Admin.Repositories.IAdminProductRepository, HDKTech.Areas.Admin.Repositories.AdminProductRepository>();
            // Giữ lại registration cũ cho các nơi khác trong project dùng namespace Repositories.Interfaces
            builder.Services.AddScoped<HDKTech.Repositories.Interfaces.IAdminProductRepository, HDKTech.Repositories.AdminProductRepository>();
            builder.Services.AddScoped<HDKTech.Areas.Admin.Repositories.BannerRepository>();
            builder.Services.AddScoped<HDKTech.Areas.Admin.Repositories.BannerClickEventRepository>();
            builder.Services.AddScoped<HDKTech.Areas.Admin.Repositories.PromotionRepository>();
            builder.Services.AddScoped<HDKTech.Areas.Admin.Repositories.ISystemLogRepository, HDKTech.Areas.Admin.Repositories.SystemLogRepository>();
            builder.Services.AddScoped<ISystemLogService, SystemLogService>();

            // Register Admin Services
            builder.Services.AddScoped<IDashboardService, DashboardService>();

            // ── Giai đoạn 1: Inventory Sync ──────────────────────────────────
            builder.Services.AddScoped<IInventoryService, InventoryService>();

            // ── Giai đoạn 3: Smart Reporting ─────────────────────────────────
            builder.Services.AddScoped<IReportService, ReportService>();

            // ── Sprint 1: Policy-based Authorization — PermissionHandler ────────
            // Handler mới đọc từ AspNetRoleClaims thay vì bảng custom RolePermissions.
            // RoleManager<IdentityRole> đã được đăng ký sẵn bởi AddIdentity() ở trên.
            builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();

            // ── Sprint 1: Authorization Policies ─────────────────────────────────
            // Tự động tạo policy cho mọi permission trong AllSystemPermissions.
            // Format policy name: "Module.Action" (vd: "Inventory.Update")
            // Dùng trên controller: [Authorize(Policy = "Inventory.Update")]
            builder.Services.AddAuthorization(options =>
            {
                foreach (var perm in HDKTech.Areas.Admin.Controllers.RoleController.AllSystemPermissions)
                {
                    var parts = perm.Split('.');
                    if (parts.Length == 2)
                        options.AddPolicy(perm,
                            p => p.AddRequirements(new PermissionRequirement(parts[0], parts[1])));
                }
            });

            // Register Cart Service (Session) - 7 days
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromDays(7);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.None;    // ✅ Đổi từ Lax → None
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // ✅ Thêm dòng này
            });

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICartService, SessionCartService>();

            // ── Giai đoạn 2: Observability — IMemoryCache cho Dashboard ─────
            // Cache mặc định dùng bộ nhớ process — phù hợp single-server deployment
            builder.Services.AddMemoryCache(options =>
            {
                options.SizeLimit            = null;  // không giới hạn số entry
                options.CompactionPercentage = 0.25;  // dọn 25% khi đầy
            });

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Connect VNPay API
            builder.Services.AddScoped<IVnPayService, VnPayService>();
            
            // Connect Momo API
            builder.Services.Configure<MomoOptionModel>(builder.Configuration.GetSection("MomoAPI"));
            builder.Services.AddHttpClient<IMomoService, MomoService>();

            var app = builder.Build();

            // Initialize LoggingHelper
            using (var scope = app.Services.CreateScope())
            {
                var logService = scope.ServiceProvider.GetRequiredService<ISystemLogService>();
                LoggingHelper.Initialize(logService);
            }

            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<HDKTechContext>();
                await HDKTech.Data.DbInitializer.InitializeAsync(scope.ServiceProvider, context);
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            // ✅ Thêm CookiePolicy trước Authentication
            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = SameSiteMode.None,
                Secure = CookieSecurePolicy.Always
            });

            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            // 1. Route cho các Area (Admin,...)
            // TUYỆT ĐỐI KHÔNG để {controller=Product} ở đây. 
            // Hãy để trống controller để nó không tự động gán Product vào ImageUrl khi bạn đăng nhập.
            app.MapControllerRoute(
                name: "MyAreas",
                pattern: "{area:exists}/{controller}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapRazorPages();
            app.Run();
        }
    }
}
