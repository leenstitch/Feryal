namespace Feryal.Application.Interfaces;

// Defines the contract for generating refresh tokens and retrieving their expiration dates.
public interface IRefreshTokenService
{
    // Generates a new refresh token.
    string GenerateToken();

    // Gets the expiration date and time of a refresh token.
    DateTimeOffset GetExpirationDate();
}