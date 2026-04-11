using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HDKTech.Areas.Identity.Data;
using HDKTech.Models;
using HDKTech.Data;
using HDKTech.Repositories;
using HDKTech.Repositories.Interfaces;
using HDKTech.Services;

using HDKTech.Utilities;

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
                 .AddIdentity<NguoiDung, IdentityRole>(options => 
                 {
                     options.SignIn.RequireConfirmedAccount = false;
                     options.Password.RequireDigit = false;
                     options.Password.RequiredLength = 4;
                     options.Password.RequireNonAlphanumeric = false;
                     options.Password.RequireUppercase = false;
                     options.Password.RequireLowercase = false;
                 }) 
                 .AddEntityFrameworkStores<HDKTechContext>()
                 .AddDefaultUI()
                 .AddDefaultTokenProviders();

            // Đăng ký Repository Pattern
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<ProductRepository>();
            builder.Services.AddScoped<CategoryRepository>();
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();

            // Đăng ký Admin Repositories
            builder.Services.AddScoped<IAdminProductRepository, AdminProductRepository>();
            builder.Services.AddScoped<BannerRepository>();
            builder.Services.AddScoped<BannerClickEventRepository>();
            builder.Services.AddScoped<KhuyenMaiRepository>();
            builder.Services.AddScoped<ISystemLogRepository, SystemLogRepository>();
            builder.Services.AddScoped<ISystemLogService, SystemLogService>();

            // Đăng ký Cart Service (Session) - 7 ngày để giỏ hàng không bị mất
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromDays(7); // 7 ngày
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Lax; // Security
            });
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICartService, SessionCartService>();

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
                // Ensure database schema is up-to-date before running seed data
                await context.Database.MigrateAsync();

                await DataSeed.KhoiTaoDuLieuMacDinh(scope.ServiceProvider);
                await DataSeedProductsOptimized.SeedProducts(scope.ServiceProvider);
                await BannerSeeder.SeedBannersAsync(context);
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
            // Hãy để trống controller để nó không tự động gán Product vào URL khi bạn đăng nhập.
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