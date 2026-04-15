using HDKTech.Areas.Admin.Models;
using HDKTech.Models;
using HDKTech.Areas.Admin.Models;
using HDKTech.Models.Vnpay;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Data;

public class HDKTechContext : IdentityDbContext<AppUser>
{
    public HDKTechContext(DbContextOptions<HDKTechContext> options)
        : base(options)
    {
    }
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<Inventory> Inventories { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Invoice> Invoices { get; set; }

    // Cart entities - No longer used (using Session-based cart)
    // public DbSet<Cart> Carts { get; set; }
    // public DbSet<CartItem> CartItems { get; set; }

    public DbSet<SystemLog> SystemLogs { get; set; }
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<OTPRequest> OTPRequests { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Banner> Banners { get; set; }
    public DbSet<BannerClickEvent> BannerClickEvents { get; set; }
    public DbSet<Promotion> Promotions { get; set; }

    public DbSet<VNPAYModel> VNPAYModels { get; set; }
    public DbSet<ShippingModel> Shippings { get; set; }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 1-1 relationship: Order and Invoice
        builder.Entity<Invoice>()
                .HasOne(i => i.Order)
                .WithOne(o => o.Invoice)
                .HasForeignKey<Invoice>(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

        // ChatSession configuration
        builder.Entity<ChatSession>()
                .HasOne(c => c.Customer)
                .WithMany()
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ChatSession>()
            .HasOne(c => c.Staff)
            .WithMany()
            .HasForeignKey(c => c.StaffId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1-n relationship: Product and Inventory
        builder.Entity<Inventory>()
                .HasOne(i => i.Product)
                .WithMany(p => p.Inventories)
                .HasForeignKey(i => i.ProductId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

        // Decimal precision configuration
        foreach (var property in builder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }

        // ProductImage relationship
        builder.Entity<ProductImage>()
            .ToTable("ProductImages");

        builder.Entity<ProductImage>()
            .HasOne(pi => pi.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }

}

