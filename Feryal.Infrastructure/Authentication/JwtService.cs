using Feryal.Application.Interfaces;
using Feryal.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Feryal.Infrastructure.Authentication;

// Generates JWT access tokens for authenticated users.
public class JwtService : IJwtService
{
    // Contains the JWT configuration settings.
    private readonly JwtOptions _jwtOptions;

    // Provides user lookup and role management operations.
    private readonly UserManager<ApplicationUser> _userManager;

    // Initializes the service with JWT options and the user manager.
    public JwtService(
        IOptions<JwtOptions> jwtOptions,
        UserManager<ApplicationUser> userManager)
    {
        _jwtOptions = jwtOptions.Value;
        _userManager = userManager;
    }

    // Generates a signed JWT access token containing the user's identity and roles.
    public async Task<string> GenerateAccessTokenAsync(int userId)
    {
        // Retrieve the user using the supplied ID.
        var user = await _userManager.FindByIdAsync(userId.ToString());

        // Stop token generation if the user does not exist.
        if (user is null)
        {
            throw new InvalidOperationException(
                "User not found.");
        }

        // Retrieve all roles assigned to the user.
        var roles = await _userManager.GetRolesAsync(user);

        // Define the identity claims included in the access token.
        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new(
                ClaimTypes.Name,
                user.UserName ?? string.Empty),

            new(
                ClaimTypes.Email,
                user.Email ?? string.Empty),

            // Include the token version for token validation or invalidation logic.
            new(
                "TokenVersion",
                user.TokenVersion.ToString())
        };

        // Add each assigned role as a role claim.
        foreach (var role in roles)
        {
            claims.Add(
                new Claim(ClaimTypes.Role, role));
        }

        // Create the symmetric signing key from the configured secret.
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtOptions.Key));

        // Configure the HMAC-SHA256 token signing algorithm.
        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        // Calculate the token expiration time in UTC.
        var expiresAt = DateTime.UtcNow.AddMinutes(
            _jwtOptions.AccessTokenExpirationMinutes);

        // Create the JWT with its issuer, audience, claims, expiration, and signature.
        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        // Serialize the JWT into its compact string representation.
        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    // Returns the UTC expiration date and time for an access token.
    public DateTimeOffset GetAccessTokenExpirationDate()
    {
        return DateTimeOffset.UtcNow.AddMinutes(
            _jwtOptions.AccessTokenExpirationMinutes);
    }
}