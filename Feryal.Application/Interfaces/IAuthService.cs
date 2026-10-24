using Feryal.Application.DTOs.Auth;

namespace Feryal.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken);

    Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken);

    Task<AuthResponse> RefreshAsync(
        RefreshRequest request,
        CancellationToken cancellationToken);

    Task LogoutAsync(
        int userId,
        CancellationToken cancellationToken);
}