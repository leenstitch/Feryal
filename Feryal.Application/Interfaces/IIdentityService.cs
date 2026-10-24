using Feryal.Application.DTOs.Auth;

namespace Feryal.Application.Interfaces;

public interface IIdentityService
{
    Task<int?> FindUserIdByEmailAsync(
        string email,
        CancellationToken cancellationToken);

    Task<int?> FindUserIdByUsernameAsync(
        string username,
        CancellationToken cancellationToken);

    Task<bool> CheckPasswordAsync(
        int userId,
        string password,
        CancellationToken cancellationToken);

    Task<bool> IsUserActiveAsync(
        int userId,
        CancellationToken cancellationToken);

    Task<bool> IsEmailConfirmedAsync(
        int userId,
        CancellationToken cancellationToken);

    Task<bool> IsLockedOutAsync(
        int userId,
        CancellationToken cancellationToken);

    Task<int> CreateUserAsync(
        RegisterRequest request,
        CancellationToken cancellationToken);

    Task AddCustomerRoleAsync(
        int userId,
        CancellationToken cancellationToken);

    Task IncrementTokenVersionAsync(
        int userId,
        CancellationToken cancellationToken);
}