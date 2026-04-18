using HDKTech.Areas.Admin.Models;
using HDKTech.Models;
using HDKTech.Models.Vnpay;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HDKTech.Data;

public class HDKTechContext : IdentityDbContext<AppUser, IdentityRole, string>
{
    public HDKTechContext(DbContextOptions<HDKTechContext> options) : base(options)
    {
    }

    // ── Catalog ──────────────────────────────────────────────────
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductVariant> ProductVariants { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<ProductTag> ProductTags { get; set; }
    public DbSet<Review> Reviews { get; set; }

    // ── Inventory ────────────────────────────────────────────────
    public DbSet<Inventory> Inventories { get; set; }
    public DbSet<StockMovement> StockMovements { get; set; }

    // ── Orders ───────────────────────────────────────────────────
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Invoice> Invoices { get; set; }

    // ── Customer profile ─────────────────────────────────────────
    public DbSet<UserAddress> UserAddresses { get; set; }

    // ── Phân quyền đã hợp nhất hoàn toàn vào ASP.NET Identity ───
    //     Roles     → AspNetRoles
    //     Claims    → AspNetRoleClaims (Type = "permission")
    //     UserRoles → AspNetUserRoles

    // ── Other ────────────────────────────────────────────────────
    public DbSet<SystemLog> SystemLogs { get; set; }
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<OTPRequest> OTPRequests { get; set; }
    public DbSet<Banner> Banners { get; set; }
    public DbSet<BannerClickEvent> BannerClickEvents { get; set; }
    public DbSet<VNPAYModel> VNPAYModels { get; set; }
    public DbSet<ShippingModel> Shippings { get; set; }

    // ── Module 5: Promotion ─────────────────────────────────────
    public DbSet<Promotion> Promotions { get; set; }
    public DbSet<PromotionProduct> PromotionProducts { get; set; }
    public DbSet<OrderPromotion> OrderPromotions { get; set; }

    // ── Module 6: Warranty ──────────────────────────────────────
    public DbSet<WarrantyPolicy> WarrantyPolicies { get; set; }
    public DbSet<WarrantyClaim> WarrantyClaims { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ============================================================
        //  MODULE 1: PRODUCT & VARIANT
        // ============================================================
        builder.Entity<Product>()
            .HasOne(p => p.Category).WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Product>()
            .HasOne(p => p.Brand).WithMany(b => b.Products)
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Product>()
            .HasIndex(p => p.Slug).IsUnique()
            .HasFilter("[Slug] IS NOT NULL");

        builder.Entity<ProductVariant>()
            .HasOne(v => v.Product).WithMany(p => p.Variants)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProductVariant>()
            .HasIndex(v => v.Sku).IsUnique();

        builder.Entity<ProductVariant>()
            .HasIndex(v => new { v.ProductId, v.IsDefault })
            .IsUnique()
            .HasFilter("[IsDefault] = 1");

        builder.Entity<ProductImage>()
            .HasOne(pi => pi.Product).WithMany(p => p.Images)
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProductTag>()
            .HasOne(pt => pt.Product).WithMany(p => p.Tags)
            .HasForeignKey(pt => pt.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProductTag>()
            .HasIndex(pt => new { pt.ProductId, pt.TagKey });
        builder.Entity<ProductTag>()
            .HasIndex(pt => new { pt.TagKey, pt.TagValue });

        // ============================================================
        //  MODULE 2: INVENTORY & STOCK MOVEMENT
        // ============================================================
        builder.Entity<Inventory>()
            .HasOne(i => i.Product).WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Inventory>()
            .HasOne(i => i.Variant).WithMany(v => v.Inventories)
            .HasForeignKey(i => i.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Inventory>()
            .HasIndex(i => new { i.ProductVariantId, i.WarehouseId })
            .IsUnique();

        builder.Entity<Inventory>()
            .Property(i => i.RowVersion).IsRowVersion();

        builder.Entity<StockMovement>()
            .HasOne(sm => sm.Inventory).WithMany(i => i.Movements)
            .HasForeignKey(sm => sm.InventoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StockMovement>()
            .HasIndex(sm => new { sm.InventoryId, sm.CreatedAt });
        builder.Entity<StockMovement>()
            .HasIndex(sm => new { sm.ReferenceType, sm.ReferenceId });

        // ============================================================
        //  MODULE 3: ORDER & SNAPSHOT
        // ============================================================

        // Order ─ AppUser (n-1) RESTRICT (không cho xoá user nếu đã có đơn)
        builder.Entity<Order>()
            .HasOne(o => o.User).WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Order ─ UserAddress (n-1) SetNull: xoá địa chỉ không được mất đơn
        builder.Entity<Order>()
            .HasOne(o => o.UserAddress).WithMany()
            .HasForeignKey(o => o.UserAddressId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Order>()
            .HasIndex(o => o.OrderCode).IsUnique();

        builder.Entity<Order>()
            .HasIndex(o => new { o.UserId, o.OrderDate });

        builder.Entity<Order>()
            .HasIndex(o => o.Status);

        // Order ─ Invoice (1-1) giữ Cascade (Invoice con của Order)
        builder.Entity<Invoice>()
            .HasOne(i => i.Order).WithOne(o => o.Invoice)
            .HasForeignKey<Invoice>(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // OrderItem ─ Order (n-1) Cascade
        builder.Entity<OrderItem>()
            .HasOne(oi => oi.Order).WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // OrderItem ─ Product (n-1) RESTRICT — bảo toàn lịch sử
        builder.Entity<OrderItem>()
            .HasOne(oi => oi.Product).WithMany()
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // OrderItem ─ Variant (n-1) RESTRICT — bảo toàn lịch sử
        builder.Entity<OrderItem>()
            .HasOne(oi => oi.Variant).WithMany(v => v.OrderItems)
            .HasForeignKey(oi => oi.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OrderItem>()
            .HasIndex(oi => new { oi.OrderId, oi.ProductVariantId });

        // ============================================================
        //  MODULE 4: CUSTOMER PROFILE — UserAddress
        // ============================================================
        builder.Entity<UserAddress>()
            .HasOne(a => a.User).WithMany(u => u.Addresses)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Mỗi user chỉ có 1 địa chỉ mặc định (filtered unique index)
        builder.Entity<UserAddress>()
            .HasIndex(a => new { a.UserId, a.IsDefault })
            .IsUnique()
            .HasFilter("[IsDefault] = 1");

        // ============================================================
        //  MODULE 4: IDENTITY & ROLE — dùng hoàn toàn ASP.NET Identity
        //  Bảng AspNetRoles/AspNetUserRoles/AspNetRoleClaims do
        //  IdentityDbContext<AppUser, IdentityRole, string> tự cấu hình.
        //  KHÔNG còn bảng custom Roles / Permissions / RolePermissions.
        // ============================================================

        // ============================================================
        //  MODULE 5: PROMOTION / DISCOUNT
        // ============================================================

        // Promotion ─ PromotionProduct (1-n) Cascade
        builder.Entity<PromotionProduct>()
            .HasOne(pp => pp.Promotion).WithMany(p => p.PromotionProducts)
            .HasForeignKey(pp => pp.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        // PromotionProduct ─ Product (n-1) Restrict (không cho xoá Product đang khuyến mãi)
        builder.Entity<PromotionProduct>()
            .HasOne(pp => pp.Product).WithMany()
            .HasForeignKey(pp => pp.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PromotionProduct>()
            .HasOne(pp => pp.Variant).WithMany()
            .HasForeignKey(pp => pp.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PromotionProduct>()
            .HasOne(pp => pp.Category).WithMany()
            .HasForeignKey(pp => pp.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PromotionProduct>()
            .HasOne(pp => pp.Brand).WithMany()
            .HasForeignKey(pp => pp.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes để query nhanh "khuyến mãi nào đang áp cho product X"
        builder.Entity<PromotionProduct>()
            .HasIndex(pp => new { pp.PromotionId, pp.ScopeType });
        builder.Entity<PromotionProduct>()
            .HasIndex(pp => pp.ProductId);
        builder.Entity<PromotionProduct>()
            .HasIndex(pp => pp.ProductVariantId);
        builder.Entity<PromotionProduct>()
            .HasIndex(pp => pp.CategoryId);
        builder.Entity<PromotionProduct>()
            .HasIndex(pp => pp.BrandId);

        // Check constraint: chính xác 1 trong 4 FK phải != null và khớp ScopeType
        builder.Entity<PromotionProduct>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_PromotionProduct_ExactlyOneScope",
                "(CASE WHEN ProductId         IS NOT NULL THEN 1 ELSE 0 END " +
                "+ CASE WHEN ProductVariantId IS NOT NULL THEN 1 ELSE 0 END " +
                "+ CASE WHEN CategoryId       IS NOT NULL THEN 1 ELSE 0 END " +
                "+ CASE WHEN BrandId          IS NOT NULL THEN 1 ELSE 0 END) = 1"));

        // Promotion: unique PromoCode (chỉ với promo có mã)
        builder.Entity<Promotion>()
            .HasIndex(p => p.PromoCode).IsUnique()
            .HasFilter("[PromoCode] IS NOT NULL");

        builder.Entity<Promotion>()
            .HasIndex(p => new { p.Status, p.StartDate, p.EndDate });

        // OrderPromotion ─ Order (n-1) Cascade
        builder.Entity<OrderPromotion>()
            .HasOne(op => op.Order).WithMany(o => o.Promotions)
            .HasForeignKey(op => op.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // OrderPromotion ─ Promotion (n-1) SetNull (giữ lịch sử khi Promotion bị xoá)
        builder.Entity<OrderPromotion>()
            .HasOne(op => op.Promotion).WithMany(p => p.OrderPromotions)
            .HasForeignKey(op => op.PromotionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<OrderPromotion>()
            .HasIndex(op => new { op.OrderId, op.PromotionId });

        // ============================================================
        //  MODULE 6: WARRANTY
        // ============================================================

        // Product ─ WarrantyPolicy (n-1) SetNull (xoá policy không được mất product)
        builder.Entity<Product>()
            .HasOne(p => p.WarrantyPolicy).WithMany(w => w.Products)
            .HasForeignKey(p => p.WarrantyPolicyId)
            .OnDelete(DeleteBehavior.SetNull);

        // WarrantyPolicy: unique Code (chỉ khi khác null)
        builder.Entity<WarrantyPolicy>()
            .HasIndex(w => w.Code).IsUnique()
            .HasFilter("[Code] IS NOT NULL");

        // WarrantyClaim ─ OrderItem (n-1) Restrict — KHÔNG được mất lịch sử
        builder.Entity<WarrantyClaim>()
            .HasOne(wc => wc.OrderItem).WithMany(oi => oi.WarrantyClaims)
            .HasForeignKey(wc => wc.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WarrantyClaim>()
            .HasIndex(wc => wc.ClaimCode).IsUnique();

        builder.Entity<WarrantyClaim>()
            .HasIndex(wc => wc.SerialNumber);

        builder.Entity<WarrantyClaim>()
            .HasIndex(wc => new { wc.Status, wc.ClaimDate });

        // ============================================================
        //  Chat, Review, v.v.
        // ============================================================
        builder.Entity<ChatSession>()
            .HasOne(c => c.Customer).WithMany()
            .HasForeignKey(c => c.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ChatSession>()
            .HasOne(c => c.Staff).WithMany()
            .HasForeignKey(c => c.StaffId)
            .OnDelete(DeleteBehavior.Restrict);

        // ============================================================
        //  Global: decimal(18,2)
        // ============================================================
        foreach (var property in builder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => (p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?))
                                 && string.IsNullOrEmpty(p.GetColumnType())))
        {
            property.SetColumnType("decimal(18,2)");
        }
    }
}
