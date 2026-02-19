using System.Security.Cryptography;
using System.Text;

namespace Contento.Services;

public static class GravatarHelper
{
    public static string GetAvatarUrl(string? email, int size = 80, string defaultImage = "mp")
    {
        if (string.IsNullOrWhiteSpace(email))
            return $"https://www.gravatar.com/avatar/?d={defaultImage}&s={size}";

        var hash = Convert.ToHexStringLower(
            MD5.HashData(Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant())));
        return $"https://www.gravatar.com/avatar/{hash}?d={defaultImage}&s={size}";
    }
}
