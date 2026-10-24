using System.Security.Cryptography;
using Feryal.Application.Interfaces;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Feryal.Infrastructure.Authentication;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly JwtOptions _jwtOptions;

    public RefreshTokenService(
        IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public string GenerateToken()
    {
        var randomBytes =
            RandomNumberGenerator.GetBytes(64);

        return WebEncoders.Base64UrlEncode(randomBytes);
    }

    public DateTimeOffset GetExpirationDate()
    {
        return DateTimeOffset.UtcNow.AddDays(
            _jwtOptions.RefreshTokenExpirationDays);
    }
}