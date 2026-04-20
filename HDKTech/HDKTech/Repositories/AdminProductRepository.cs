using HDKTech.Data;
using HDKTech.Models;
using HDKTech.Repositories.Interfaces;

namespace HDKTech.Repositories
{
    /// <summary>
    /// [DEPRECATED — Module C cleanup]
    /// Thin-wrapper giữ namespace để không phá build. Toàn bộ logic nằm ở
    /// HDKTech.Areas.Admin.Repositories.AdminProductRepository.
    /// DI registration cho class này đã bị xoá khỏi Program.cs.
    /// </summary>
    [Obsolete("Use HDKTech.Areas.Admin.Repositories.AdminProductRepository")]
#pragma warning disable CS0618
    internal sealed class AdminProductRepository
        : HDKTech.Areas.Admin.Repositories.AdminProductRepository,
          IAdminProductRepository
#pragma warning restore CS0618
    {
        public AdminProductRepository(
            HDKTechContext context,
            ILogger<HDKTech.Areas.Admin.Repositories.AdminProductRepository> logger)
            : base(context, logger)
        {
        }
    }
}
