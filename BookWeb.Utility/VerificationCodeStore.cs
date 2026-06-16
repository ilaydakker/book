using System.Collections.Concurrent;

namespace BookWeb.Utility
{
    public static class VerificationCodeStore
    {
        private static readonly ConcurrentDictionary<string, CodeEntry> _codes = new();

        public static string GenerateCode(string userId, string token)
        {
            var code = Random.Shared.Next(100000, 999999).ToString();
            _codes[code] = new CodeEntry(userId, token, DateTime.UtcNow.AddMinutes(15));

            // Clean expired codes
            foreach (var kvp in _codes)
            {
                if (kvp.Value.Expiry < DateTime.UtcNow)
                    _codes.TryRemove(kvp.Key, out _);
            }

            return code;
        }

        public static (string? UserId, string? Token) GetToken(string code)
        {
            if (_codes.TryRemove(code, out var entry))
            {
                if (entry.Expiry >= DateTime.UtcNow)
                    return (entry.UserId, entry.Token);
            }
            return (null, null);
        }

        private record CodeEntry(string UserId, string Token, DateTime Expiry);
    }
}
