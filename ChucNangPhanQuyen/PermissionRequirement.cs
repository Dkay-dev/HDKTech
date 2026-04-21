using Microsoft.AspNetCore.Authorization;

namespace HDKTech.ChucNangPhanQuyen
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// Khi Module == <see cref="Wildcard"/> hoặc Action == <see cref="Wildcard"/>,
        /// <see cref="PermissionHandler"/> coi là "bất kỳ permission nào"
        /// — dùng cho policy RequireAdminArea để cho phép mọi role có ít nhất
        /// 1 permission claim được vào admin area.
        /// </summary>
        public const string Wildcard = "*";

        public string Module { get; }
        public string Action { get; }

        public bool IsAnyPermission => Module == Wildcard || Action == Wildcard;

        public PermissionRequirement(string module, string action)
        {
            Module = module;
            Action = action;
        }
    }
}
