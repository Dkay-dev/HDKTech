using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HDKTech.Models;
using HDKTech.Data;
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
using HDKTech.Areas.Admin.Constants;

namespace HDKTech
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("HDKTechContextConnection")
                ?? throw new InvalidOperationException("Connection string 'HDKTechContextConnection' not found.");

            // ─────────────────────────────────────────────────────────────
            // DbContext
            // ─────────────────────────────────────────────────────────────
            builder.Services.AddDbContext<HDKTechContext>(options =>
                options.UseSqlServer(connectionString));

            // ─────────────────────────────────────────────────────────────
            // Identity — Chuyển từ AddIdentityCore sang AddIdentity để hỗ trợ đầy đủ Store
            // ─────────────────────────────────────────────────────────────
            builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 4;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
                .AddEntityFrameworkStores<HDKTechContext>()
                .AddDefaultTokenProviders();

            // Sau khi dùng AddIdentity, bạn có thể xóa bớt các dòng AddSignInManager() 
            // và AddAuthentication(...) lẻ tẻ bên dưới vì AddIdentity đã lo hết rồi.
            // Đăng ký cookie scheme riêng (vì AddIdentityCore không đi kèm cookie)
            

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.SameSite     = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.HttpOnly     = true;
                options.LoginPath           = "/Identity/Account/Login";
                options.AccessDeniedPath    = "/Identity/Account/AccessDenied";
            });

            // ─────────────────────────────────────────────────────────────
            // Repositories
            // ─────────────────────────────────────────────────────────────
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<ProductRepository>();
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<CategoryRepository>();
            builder.Services.AddScoped<IBrandRepository, BrandRepository>();
            builder.Services.AddScoped<BrandRepository>();
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();

            builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
            builder.Services.AddScoped<IReviewService, ReviewService>();

            // Admin Repositories — hai namespace cùng interface
            builder.Services.AddScoped<HDKTech.Areas.Admin.Repositories.IAdminProductRepository,
                                       HDKTech.Areas.Admin.Repositories.AdminProductRepository>();
            builder.Services.AddScoped<HDKTech.Repositories.Interfaces.IAdminProductRepository,
                                       HDKTech.Repositories.AdminProductRepository>();
            builder.Services.AddScoped<HDKTech.Areas.Admin.Repositories.BannerRepository>();
            builder.Services.AddScoped<HDKTech.Areas.Admin.Repositories.BannerClickEventRepository>();
            builder.Services.AddScoped<HDKTech.Areas.Admin.Repositories.PromotionRepository>();
            builder.Services.AddScoped<HDKTech.Areas.Admin.Repositories.ISystemLogRepository,
                                       HDKTech.Areas.Admin.Repositories.SystemLogRepository>();
            builder.Services.AddScoped<ISystemLogService, SystemLogService>();

            // Admin Services
            builder.Services.AddScoped<IDashboardService, DashboardService>();

            // Inventory / Reports / Cart
            builder.Services.AddScoped<IInventoryService, InventoryService>();
            builder.Services.AddScoped<IReportService, ReportService>();

            // ─────────────────────────────────────────────────────────────
            // Authorization — Policy-based Permission (ASP.NET Identity)
            //   Handler đọc trực tiếp từ AspNetRoleClaims (Type="permission").
            //   Roles được seed trong IdentityRoleSeed.
            // ─────────────────────────────────────────────────────────────
            builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();
            builder.Services.AddAuthorization(options =>
            {
                // ─── Role-based policies (Identity) ─────────────────────────
                // Dùng CHUỖI role name từ AdminConstants — ánh xạ trực tiếp
                // sang Identity Roles ("Admin", "Manager") trong AspNetRoles.
                options.AddPolicy("RequireAdmin", policy =>
                    policy.RequireRole(AdminConstants.AdminRole));

                options.AddPolicy("RequireManager", policy =>
                    policy.RequireRole(AdminConstants.AdminRole, AdminConstants.ManagerRole));

                // ─── Permission-based policies ──────────────────────────────
                // Mỗi permission "Module.Action" được map thành 1 policy đồng tên;
                // PermissionHandler đọc AspNetRoleClaims để Succeed/Fail.
                foreach (var perm in HDKTech.Data.IdentityRoleSeed.AllPermissions)
                {
                    var parts = perm.Split('.');
                    if (parts.Length == 2)
                        options.AddPolicy(perm,
                            p => p.AddRequirements(new PermissionRequirement(parts[0], parts[1])));
                }
            });

            // ─────────────────────────────────────────────────────────────
            // Session / Cart
            // ─────────────────────────────────────────────────────────────
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout         = TimeSpan.FromDays(7);
                options.Cookie.HttpOnly     = true;
                options.Cookie.IsEssential  = true;
                options.Cookie.SameSite     = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICartService, SessionCartService>();

            // ─────────────────────────────────────────────────────────────
            // Memory cache (Dashboard)
            // ─────────────────────────────────────────────────────────────
            builder.Services.AddMemoryCache(options =>
            {
                options.SizeLimit            = null;
                options.CompactionPercentage = 0.25;
            });

            // MVC + Razor Pages (vẫn cần cho Identity scaffold UI nếu user muốn giữ)
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();

            // Payment Services
            builder.Services.AddScoped<IVnPayService, VnPayService>();
            builder.Services.Configure<MomoOptionModel>(builder.Configuration.GetSection("MomoAPI"));
            builder.Services.AddHttpClient<IMomoService, MomoService>();

            // Category cache / Product service
            builder.Services.AddSingleton<ICategoryCacheService, CategoryCacheService>();
            builder.Services.AddScoped<IProductService, ProductService>();

            var app = builder.Build();

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

            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = SameSiteMode.None,
                Secure                = CookieSecurePolicy.Always
            });

            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

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
