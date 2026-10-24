using Feryal.Domain.Entities;

// Defines the contract for managing refresh tokens in the database.
public interface IRefreshTokenRepository
{
    // Adds a new refresh token to the database.
    Task AddAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken);

    // Retrieves a refresh token by its hashed token value.
    Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken);

    // Revokes all active refresh tokens belonging to the specified user.
    Task RevokeActiveTokensAsync(
        int userId,
        CancellationToken cancellationToken);

    // Saves pending changes to the database.
    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}