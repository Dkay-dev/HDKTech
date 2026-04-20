using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
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
using HDKTech.Services.Interfaces;
using HDKTech.Areas.Admin.Services.Interfaces;

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

            // Module D: IDbContextFactory — dùng trong DashboardService để tạo
            // context riêng biệt cho từng Task.WhenAll group (tránh shared-context race)
            builder.Services.AddDbContextFactory<HDKTechContext>(options =>
                options.UseSqlServer(connectionString),
                ServiceLifetime.Scoped);

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

            // Chat realtime (SignalR)
            builder.Services.AddScoped<IChatRepository, ChatRepository>();
            builder.Services.AddScoped<IChatService, ChatService>();

            // Admin Repositories — canonical registration (Areas.Admin namespace)
            // HDKTech.Repositories.AdminProductRepository là deprecated stub, không register nữa
            builder.Services.AddScoped<HDKTech.Areas.Admin.Repositories.IAdminProductRepository,
                                       HDKTech.Areas.Admin.Repositories.AdminProductRepository>();
            builder.Services.AddScoped<HDKTech.Areas.Admin.Repositories.BannerRepository>();
            builder.Services.AddScoped<HDKTech.Areas.Admin.Repositories.BannerClickEventRepository>();
            builder.Services.AddScoped<HDKTech.Areas.Admin.Repositories.PromotionRepository>();
            builder.Services.AddScoped<HDKTech.Areas.Admin.Repositories.ISystemLogRepository,
                                       HDKTech.Areas.Admin.Repositories.SystemLogRepository>();
            builder.Services.AddScoped<ISystemLogService, SystemLogService>();

            // Admin Services
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<IProductAdminService,
                                       HDKTech.Areas.Admin.Services.ProductAdminService>();
            builder.Services.AddScoped<IOrderAdminService,
                                       HDKTech.Areas.Admin.Services.OrderAdminService>();

            // Inventory / Reports / Cart
            builder.Services.AddScoped<IInventoryService, InventoryService>();
            builder.Services.AddScoped<IReportService, ReportService>();
            builder.Services.AddScoped<IPromotionService, PromotionService>();

            // Module D: Email xác nhận đơn hàng (SMTP)
            builder.Services.AddScoped<IEmailService, SmtpEmailService>();

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

                options.AddPolicy("RequireAdminArea", policy =>
                policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("Admin") ||
                    ctx.User.IsInRole("Manager") ||
                    ctx.User.IsInRole("Staff") ||
                    // Hỗ trợ custom role tự tạo: có bất kỳ role nào không phải Customer
                    (ctx.User.Identity?.IsAuthenticated == true &&
                     !ctx.User.IsInRole("Customer") &&
                     ctx.User.Claims.Any(c =>
                         c.Type == System.Security.Claims.ClaimTypes.Role))
                ));

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

            // ── Cart: Session → Database (Module A Refactor) ──────────
            // Đổi từ SessionCartService sang DbCartService để:
            //   - Cart persist qua server restart / scale-out
            //   - Hỗ trợ merge guest cart khi login
            //   - Validate tồn kho real-time khi thêm/sửa giỏ
            builder.Services.AddScoped<ICartService, DbCartService>();

            // Background job expire PendingCheckout quá 30 phút
            builder.Services.AddHostedService<ExpireCheckoutJob>();

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

            // SignalR — realtime chat
            builder.Services.AddSignalR();

            // Payment Services
            builder.Services.AddScoped<IVnPayService, VnPayService>();
            builder.Services.Configure<MomoOptionModel>(builder.Configuration.GetSection("MomoAPI"));
            builder.Services.AddHttpClient<IMomoService, MomoService>();

            // Category cache / Product service
            builder.Services.AddSingleton<ICategoryCacheService, CategoryCacheService>();
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<IBrandService, BrandService>();
            builder.Services.AddScoped<IHomeService, HomeService>();
            builder.Services.AddScoped<ICheckoutService, CheckoutService>();

            // ─────────────────────────────────────────────────────────────
            // Rate Limiting — ASP.NET Core built-in (.NET 7+)
            //   "checkout"   : 5 request / phút / user  → POST /Checkout
            //   "add-to-cart": 20 request / phút / user → POST /Cart/AddToCart
            // Key theo ClaimTypes.NameIdentifier (userId); nếu chưa đăng nhập dùng IP.
            // ─────────────────────────────────────────────────────────────
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // ── Checkout: 5 req / 60s / user ─────────────────────────
                options.AddPolicy("checkout", httpContext =>
                {
                    var userId = httpContext.User.FindFirst(
                        System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    var key = string.IsNullOrEmpty(userId)
                        ? $"ip:{httpContext.Connection.RemoteIpAddress}"
                        : $"user:{userId}";

                    return RateLimitPartition.GetFixedWindowLimiter(key, _ =>
                        new FixedWindowRateLimiterOptions
                        {
                            PermitLimit          = 5,
                            Window               = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit           = 0   // Không queue, reject ngay
                        });
                });

                // ── AddToCart: 20 req / 60s / user ───────────────────────
                options.AddPolicy("add-to-cart", httpContext =>
                {
                    var userId = httpContext.User.FindFirst(
                        System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    var key = string.IsNullOrEmpty(userId)
                        ? $"ip:{httpContext.Connection.RemoteIpAddress}"
                        : $"user:{userId}";

                    return RateLimitPartition.GetFixedWindowLimiter(key, _ =>
                        new FixedWindowRateLimiterOptions
                        {
                            PermitLimit          = 20,
                            Window               = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit           = 0
                        });
                });
            });

            var app = builder.Build();

            // Initialize LoggingHelper với IServiceScopeFactory (singleton) — không dùng scoped instance
            // để tránh disposed-scope bug. Mỗi lần log sẽ tạo scope mới qua factory.
            LoggingHelper.Initialize(app.Services.GetRequiredService<IServiceScopeFactory>());

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
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "MyAreas",
                pattern: "{area:exists}/{controller}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapRazorPages();

            // SignalR Hub endpoint
            app.MapHub<HDKTech.Hubs.ChatHub>("/chathub");

            app.Run();
        }
    }
}
