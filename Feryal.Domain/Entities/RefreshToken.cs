namespace Feryal.Domain.Entities;

public class RefreshToken
{
    private RefreshToken()
    {
    }

    public RefreshToken(
        int userId,
        string tokenHash,
        DateTimeOffset expiresAt)
    {
        if (userId <= 0)
            throw new ArgumentException(
                "User is required.",
                nameof(userId));

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException(
                "Token hash is required.",
                nameof(tokenHash));

        if (expiresAt <= DateTimeOffset.UtcNow)
            throw new ArgumentException(
                "Expiration date must be in the future.",
                nameof(expiresAt));

        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public int Id { get; private set; }

    public int UserId { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    public DateTimeOffset? ReplacedAt { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public bool IsRevoked => RevokedAt.HasValue;

    public bool IsExpired =>
        DateTimeOffset.UtcNow >= ExpiresAt;

    public bool IsActive =>
        !IsRevoked && !IsExpired;

    public bool WasReplaced =>
        ReplacedAt.HasValue;

    public void Revoke()
    {
        if (!IsRevoked)
            RevokedAt = DateTimeOffset.UtcNow;
    }

    public void ReplaceWith(string newTokenHash)
    {
        if (string.IsNullOrWhiteSpace(newTokenHash))
            throw new ArgumentException(
                "New token hash is required.",
                nameof(newTokenHash));

        Revoke();

        ReplacedByTokenHash = newTokenHash;
        ReplacedAt = DateTimeOffset.UtcNow;
    }
}