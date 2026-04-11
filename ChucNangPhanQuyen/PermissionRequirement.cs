using Microsoft.AspNetCore.Authorization;

namespace HDKTech.ChucNangPhanQuyen
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Module { get; }
        public string Action { get; }

        public PermissionRequirement(string module, string action)
        {
            Module = module;
            Action = action;
        }
    }
}
