using System.Security.Cryptography;
using System.Text;
using Feryal.Application.Interfaces;

namespace Feryal.Infrastructure.Authentication;

public class RefreshTokenHasher
    : IRefreshTokenHasher
{
    public string Hash(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException(
                "Token is required.",
                nameof(token));

        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(token)); 

        return Convert.ToHexString(bytes);
    }
}