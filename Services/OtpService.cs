using Microsoft.Extensions.Caching.Memory;

namespace HDKTech.Services
{
    public interface IOtpService
    {
        string GenerateOtp(string email, string type);
        bool ValidateOtp(string email, string type, string otp);
    }

    public class OtpService : IOtpService
    {
        private readonly IMemoryCache _cache;

        public OtpService(IMemoryCache cache) => _cache = cache;

        public string GenerateOtp(string email, string type)
        {
            var otp = Random.Shared.Next(100000, 999999).ToString();
            _cache.Set(Key(email, type), otp, TimeSpan.FromMinutes(15));
            return otp;
        }

        public bool ValidateOtp(string email, string type, string otp)
        {
            var key = Key(email, type);
            if (_cache.TryGetValue(key, out string? stored) && stored == otp?.Trim())
            {
                _cache.Remove(key);
                return true;
            }
            return false;
        }

        private static string Key(string email, string type)
            => $"otp:{type}:{email.Trim().ToLowerInvariant()}";
    }
}
