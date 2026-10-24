using System.Security.Cryptography;
using Feryal.Application.Interfaces;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Feryal.Infrastructure.Authentication;

// Generates refresh tokens and calculates their expiration dates.
public class RefreshTokenService : IRefreshTokenService
{
    // Contains the JWT and refresh token configuration settings.
    private readonly JwtOptions _jwtOptions;

    // Initializes the service with the configured JWT options.
    public RefreshTokenService(
        IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    // Generates a cryptographically secure random refresh token.
    public string GenerateToken()
    {
        // Generate 64 random bytes using a cryptographically secure generator.
        var randomBytes =
            RandomNumberGenerator.GetBytes(64);

        // Encode the random bytes as a URL-safe Base64 string.
        return WebEncoders.Base64UrlEncode(randomBytes);
    }

    // Calculates the refresh token expiration date based on the configured lifetime.
    public DateTimeOffset GetExpirationDate()
    {
        return DateTimeOffset.UtcNow.AddDays(
            _jwtOptions.RefreshTokenExpirationDays);
    }
}