namespace Feryal.Application.Interfaces;

public interface IJwtService
{
    Task<string> GenerateAccessTokenAsync(int userId);

    DateTimeOffset GetAccessTokenExpirationDate();
}