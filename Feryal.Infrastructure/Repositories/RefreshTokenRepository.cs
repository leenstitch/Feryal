using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Feryal.Infrastructure.Repositories;

// Provides database operations for managing refresh tokens.
public class RefreshTokenRepository
    : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _context;

    // Inject the database context to access refresh tokens.
    public RefreshTokenRepository(
        ApplicationDbContext context)
    {
        _context = context;
    }

    // Adds a new refresh token to the database context.
    public async Task AddAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken)
    {
        await _context.RefreshTokens.AddAsync(
            refreshToken,
            cancellationToken);
    }

    // Retrieves a refresh token using its hashed token value.
    public async Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(
                x => x.TokenHash == tokenHash,
                cancellationToken);
    }

    // Revokes all active, unexpired refresh tokens belonging to the specified user.
    public async Task RevokeActiveTokensAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        // Retrieve tokens that have not been revoked and have not expired.
        var refreshTokens =
            await _context.RefreshTokens
                .Where(x =>
                    x.UserId == userId &&
                    x.RevokedAt == null &&
                    x.ExpiresAt > DateTimeOffset.UtcNow)
                .ToListAsync(cancellationToken);

        // Revoke each matching token through the entity's domain method.
        foreach (var refreshToken in refreshTokens)
            refreshToken.Revoke();
    }

    // Persists pending changes made through the database context.
    public async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(
            cancellationToken);
    }
}