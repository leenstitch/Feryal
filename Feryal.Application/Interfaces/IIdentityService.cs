using Feryal.Application.DTOs.Auth;

// Defines the contract for managing user identity, authentication checks, and account-related operations.
public interface IIdentityService
{
    // Finds a user's ID using their email address.
    Task<int?> FindUserIdByEmailAsync(
        string email,
        CancellationToken cancellationToken);

    // Finds a user's ID using their username.
    Task<int?> FindUserIdByUsernameAsync(
        string username,
        CancellationToken cancellationToken);

    // Checks whether the provided password matches the specified user's password.
    Task<bool> CheckPasswordAsync(
        int userId,
        string password,
        CancellationToken cancellationToken);

    // Checks whether the specified user account is active.
    Task<bool> IsUserActiveAsync(
        int userId,
        CancellationToken cancellationToken);

    // Checks whether the specified user's email address has been confirmed.
    Task<bool> IsEmailConfirmedAsync(
        int userId,
        CancellationToken cancellationToken);

    // Checks whether the specified user account is currently locked out.
    Task<bool> IsLockedOutAsync(
        int userId,
        CancellationToken cancellationToken);

    // Creates a new user account using the provided registration data and returns the user ID.
    Task<int> CreateUserAsync(
        RegisterRequest request,
        CancellationToken cancellationToken);

    // Assigns the customer role to the specified user.
    Task AddCustomerRoleAsync(
        int userId,
        CancellationToken cancellationToken);

    // Increments the token version for the specified user to invalidate previously issued tokens.
    Task IncrementTokenVersionAsync(
        int userId,
        CancellationToken cancellationToken);
}