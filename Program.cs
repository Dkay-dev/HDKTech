// Program.cs — Phiên bản đầy đủ với Google OAuth + Email Confirmation
// Thay thế Program.cs gốc bằng file này
using HDKTech.Areas.Admin.Constants;
using HDKTech.Areas.Admin.Repositories;
using HDKTech.Areas.Admin.Services;
using HDKTech.Areas.Admin.Services.Interfaces;
using HDKTech.ChucNangPhanQuyen;
using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Models.Momo;
using HDKTech.Repositories;
using HDKTech.Repositories.Interfaces;
using HDKTech.Services;
using HDKTech.Services.Interfaces;
using HDKTech.Services.Momo;
using HDKTech.Services.Vnpay;
using HDKTech.Utilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

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

            builder.Services.AddDbContextFactory<HDKTechContext>(options =>
                options.UseSqlServer(connectionString),
                ServiceLifetime.Scoped);

            // ─────────────────────────────────────────────────────────────
            // ✅ Email Service (MailKit — thay System.Net.Mail)
            // ─────────────────────────────────────────────────────────────
            builder.Services.AddSingleton<IEmailSender, MailKitEmailSender>();

            // Adapter: bọc IEmailSender của HDKTech → Identity UI IEmailSender
            builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender,
                IdentityEmailSenderAdapter>();

            // ─────────────────────────────────────────────────────────────
            // Identity — với RequireConfirmedEmail = true
            // ─────────────────────────────────────────────────────────────
            builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;  // ✅ Bật xác nhận email
                options.SignIn.RequireConfirmedEmail = true;  // ✅ Bật xác nhận email

                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
                .AddEntityFrameworkStores<HDKTechContext>()
                .AddDefaultTokenProviders()
                .AddDefaultUI();  // ✅ Cần cho Razor Pages Identity

            // ─────────────────────────────────────────────────────────────
            // ✅ Google OAuth Authentication
            // ─────────────────────────────────────────────────────────────
            builder.Services.AddAuthentication()
                .AddGoogle(googleOptions =>
                {
                    var googleId = builder.Configuration["GoogleAuth:ClientId"];
                    var googleSecret = builder.Configuration["GoogleAuth:ClientSecret"];

                    if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(googleSecret))
                    {
                        // Log warning nhưng không crash — Google login sẽ không hiện
                        Console.WriteLine("⚠ CẢNH BÁO: GoogleAuth:ClientId hoặc ClientSecret chưa được cấu hình. Đăng nhập bằng Google sẽ không khả dụng.");
                    }
                    else
                    {
                        googleOptions.ClientId = googleId;
                        googleOptions.ClientSecret = googleSecret;

                        // Lấy thêm thông tin profile
                        googleOptions.Scope.Add("profile");
                        googleOptions.ClaimActions.MapJsonKey("picture", "picture", "url");

                        googleOptions.SaveTokens = true;

                        // URL callback — phải khớp với Google Console
                        // Mặc định: /signin-google (không cần đổi)
                    }
                });

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.HttpOnly = true;
                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
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

            builder.Services.AddScoped<IChatRepository, ChatRepository>();
            builder.Services.AddScoped<IChatService, ChatService>();

            builder.Services.AddScoped<HDKTech.Areas.Admin.Repositories.IAdminProductRepository,
                                       HDKTech.Areas.Admin.Repositories.AdminProductRepository>();
            builder.Services.AddScoped<HDKTech.Areas.Admin.Repositories.BannerRepository>();
            builder.Services.AddScoped<HDKTech.Areas.Admin.Repositories.BannerClickEventRepository>();
            builder.Services.AddScoped<HDKTech.Areas.Admin.Repositories.PromotionRepository>();
            builder.Services.AddScoped<HDKTech.Areas.Admin.Repositories.ISystemLogRepository,
                                       HDKTech.Areas.Admin.Repositories.SystemLogRepository>();
            builder.Services.AddScoped<ISystemLogService, SystemLogService>();

            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<IProductAdminService,
                                       HDKTech.Areas.Admin.Services.ProductAdminService>();
            builder.Services.AddScoped<IOrderAdminService,
                                       HDKTech.Areas.Admin.Services.OrderAdminService>();

            builder.Services.AddScoped<IInventoryService, InventoryService>();
            builder.Services.AddScoped<IReportService, ReportService>();
            builder.Services.AddScoped<IPromotionService, PromotionService>();

            // ✅ SmtpEmailService giờ dùng IEmailSender nội bộ
            builder.Services.AddScoped<IEmailService, SmtpEmailService>();

            // OTP Service (dùng IMemoryCache, TTL 15 phút)
            builder.Services.AddSingleton<IOtpService, OtpService>();

            // ─────────────────────────────────────────────────────────────
            // Authorization
            // ─────────────────────────────────────────────────────────────
            builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdmin", policy =>
                    policy.RequireRole(AdminConstants.AdminRole));

                options.AddPolicy("RequireManager", policy =>
                    policy.RequireRole(AdminConstants.AdminRole, AdminConstants.ManagerRole));

                options.AddPolicy("RequireAdminArea", policy =>
                policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("Admin") ||
                    ctx.User.IsInRole("Manager") ||
                    ctx.User.IsInRole("Staff") ||
                    (ctx.User.Identity?.IsAuthenticated == true &&
                     !ctx.User.IsInRole("Customer") &&
                     ctx.User.Claims.Any(c =>
                         c.Type == System.Security.Claims.ClaimTypes.Role))
                ));

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
                options.IdleTimeout = TimeSpan.FromDays(7);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICartService, DbCartService>();
            builder.Services.AddHostedService<ExpireCheckoutJob>();

            // ─────────────────────────────────────────────────────────────
            // Memory cache / MVC / SignalR
            // ─────────────────────────────────────────────────────────────
            builder.Services.AddMemoryCache(options =>
            {
                options.SizeLimit = null;
                options.CompactionPercentage = 0.25;
            });

            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();
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
            // Rate Limiting
            // ─────────────────────────────────────────────────────────────
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

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
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                });

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
                            PermitLimit = 20,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                });
            });

            var app = builder.Build();

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
                Secure = CookieSecurePolicy.Always
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
            app.MapHub<HDKTech.Hubs.ChatHub>("/chathub");

            app.Run();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Adapter: Bọc IEmailSender (HDKTech) → IEmailSender (Identity UI)
    // Đặt ngay dưới class Program hoặc tách file riêng
    // ─────────────────────────────────────────────────────────────
    /// <summary>
    /// ASP.NET Identity UI dùng interface Microsoft.AspNetCore.Identity.UI.Services.IEmailSender
    /// để gửi confirm email và reset password. Adapter này chuyển đổi sang IEmailSender của HDKTech.
    /// </summary>
    public class IdentityEmailSenderAdapter
        : Microsoft.AspNetCore.Identity.UI.Services.IEmailSender
    {
        private readonly HDKTech.Services.IEmailSender _sender;

        public IdentityEmailSenderAdapter(HDKTech.Services.IEmailSender sender)
        {
            _sender = sender;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
            => _sender.SendEmailAsync(email, subject, htmlMessage);
    }
}