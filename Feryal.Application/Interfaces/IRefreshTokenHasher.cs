namespace Feryal.Application.Interfaces;

// Defines the contract for hashing refresh tokens.
public interface IRefreshTokenHasher
{
    // Generates a hash from the provided refresh token.
    string Hash(string token);
}