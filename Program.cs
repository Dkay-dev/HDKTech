using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HDKTech.Models;
using HDKTech.Data;
using HDKTech.Repositories;
using HDKTech.Repositories.Interfaces;
using HDKTech.Services;
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
            var connectionString = builder.Configuration.GetConnectionString("HDKTechContextConnection") ?? throw new InvalidOperationException("Connection string 'HDKTechContextConnection' not found.");

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

            // ── Giai đoạn 1: Granular Security — PermissionHandler ───────────
            // Kích hoạt custom AuthorizationHandler dùng bảng RolePermissions
            builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();

            // ── Giai đoạn 1: Authorization Policies ──────────────────────────
            // Mỗi Policy ánh xạ đến 1 cặp (Module, Action) trong bảng Permissions
            builder.Services.AddAuthorization(options =>
            {
                // Order
                options.AddPolicy("Order.Read",   p => p.AddRequirements(new PermissionRequirement("Order",   "Read")));
                options.AddPolicy("Order.Update", p => p.AddRequirements(new PermissionRequirement("Order",   "Update")));
                options.AddPolicy("Order.Delete", p => p.AddRequirements(new PermissionRequirement("Order",   "Delete")));
                // Product
                options.AddPolicy("Product.Create", p => p.AddRequirements(new PermissionRequirement("Product", "Create")));
                options.AddPolicy("Product.Update", p => p.AddRequirements(new PermissionRequirement("Product", "Update")));
                options.AddPolicy("Product.Delete", p => p.AddRequirements(new PermissionRequirement("Product", "Delete")));
                // Inventory
                options.AddPolicy("Inventory.Update", p => p.AddRequirements(new PermissionRequirement("Inventory", "Update")));
                // ── Giai đoạn 3: Smart Reporting ──────────────────────────────
                options.AddPolicy("Report.Export", p => p.AddRequirements(new PermissionRequirement("Report", "Export")));
            });

            // Register Cart Service (Session) - 7 days
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromDays(7);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
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

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseSession();  // Thêm dòng này

            app.UseAuthentication();
            app.UseAuthorization();

            // 1. Route cho các Area (Admin,...)
            // TUYỆT ĐỐI KHÔNG để {controller=Product} ở đây. 
            // Hãy để trống controller để nó không tự động gán Product vào ImageUrl khi bạn đăng nhập.
            app.MapControllerRoute(
                name: "MyAreas",
                pattern: "{area:exists}/{controller}/{action=Index}/{id?}");

            // 2. Route mặc định (Homepage)
            // Đây là route quan trọng nhất cho Logo và Đăng nhập.
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapRazorPages();
            app.Run();
            
        }
    }
}
