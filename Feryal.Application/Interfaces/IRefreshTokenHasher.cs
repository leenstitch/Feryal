namespace Feryal.Application.Interfaces;

public interface IRefreshTokenHasher
{
    string Hash(string token);
}