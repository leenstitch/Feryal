namespace Feryal.Application.Interfaces;

// Defines the contract for generating and retrieving JWT access token information.
public interface IJwtService
{
    // Generates an access token for the specified user.
    Task<string> GenerateAccessTokenAsync(int userId);

    // Gets the expiration date and time of an access token.
    DateTimeOffset GetAccessTokenExpirationDate();
}