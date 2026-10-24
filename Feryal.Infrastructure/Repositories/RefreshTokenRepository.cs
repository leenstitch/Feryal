using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;

namespace Feryal.Infrastructure.Repositories;

public class RefreshTokenRepository
    : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _context;

    public RefreshTokenRepository(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken)
    {
        await _context.RefreshTokens.AddAsync(
            refreshToken,
            cancellationToken);
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(
                x => x.TokenHash == tokenHash,
                cancellationToken);
    }

    public async Task RevokeActiveTokensAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var refreshTokens =
            await _context.RefreshTokens
                .Where(x =>
                    x.UserId == userId &&
                    x.RevokedAt == null &&
                    x.ExpiresAt > DateTimeOffset.UtcNow)
                .ToListAsync(cancellationToken);

        foreach (var refreshToken in refreshTokens)
            refreshToken.Revoke();
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(
            cancellationToken);
    }
}