using Feryal.Application.DTOs.Auth;

// Defines the contract for authentication-related operations.
public interface IAuthService
{
    // Registers a new user and returns the authentication response.
    Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken);

    // Authenticates an existing user and returns the authentication response.
    Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken);

    // Refreshes the authentication credentials using the provided refresh request.
    Task<AuthResponse> RefreshAsync(
        RefreshRequest request,
        CancellationToken cancellationToken);

    // Logs out the specified user and performs the required logout operations.
    Task LogoutAsync(
        int userId,
        CancellationToken cancellationToken);
}