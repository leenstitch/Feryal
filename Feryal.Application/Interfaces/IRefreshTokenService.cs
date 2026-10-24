namespace Feryal.Application.Interfaces;

public interface IRefreshTokenService
{
    string GenerateToken();

    DateTimeOffset GetExpirationDate();
}