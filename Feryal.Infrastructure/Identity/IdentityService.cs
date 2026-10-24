using Feryal.Application.DTOs.Auth;
using Feryal.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Feryal.Infrastructure.Identity;

// Implements identity-related operations using ASP.NET Core Identity.
public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    // Inject UserManager to manage application users and their credentials.
    public IdentityService(
        UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    // Finds a user's ID using their email address.
    public async Task<int?> FindUserIdByEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
        var user =
            await _userManager.FindByEmailAsync(
                email);

        return user?.Id;
    }

    // Finds a user's ID using their username.
    public async Task<int?> FindUserIdByUsernameAsync(
        string username,
        CancellationToken cancellationToken)
    {
        var user =
            await _userManager.FindByNameAsync(
                username);

        return user?.Id;
    }

    // Checks whether the specified user has the provided password.
    public async Task<bool> CheckPasswordAsync(
        int userId,
        string password,
        CancellationToken cancellationToken)
    {
        var user =
            await _userManager.FindByIdAsync(
                userId.ToString());

        // Return false if the user does not exist.
        if (user is null)
            return false;

        return await _userManager.CheckPasswordAsync(
            user,
            password);
    }

    // Checks whether the user exists and is active.
    public async Task<bool> IsUserActiveAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var user =
            await _userManager.FindByIdAsync(
                userId.ToString());

        return user is not null && user.IsActive;
    }

    // Checks whether the user's email address has been confirmed.
    public async Task<bool> IsEmailConfirmedAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var user =
            await _userManager.FindByIdAsync(
                userId.ToString());

        return user?.EmailConfirmed == true;
    }

    // Checks whether the user is currently locked out.
    public async Task<bool> IsLockedOutAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var user =
            await _userManager.FindByIdAsync(
                userId.ToString());

        // Treat a nonexistent user as locked out.
        if (user is null)
            return true;

        return await _userManager.IsLockedOutAsync(user);
    }

    // Creates a new user account using the registration details.
    public async Task<int> CreateUserAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        // Trim whitespace from user-provided identity fields.
        var user = new ApplicationUser(
            request.FirstName.Trim(),
            request.LastName.Trim(),
            request.Email.Trim());

        user.UserName = request.Username.Trim();

        // Create the user and validate the supplied password.
        var result =
            await _userManager.CreateAsync(
                user,
                request.Password);

        // Throw an exception if Identity rejects the registration.
        if (!result.Succeeded)
        {
            var errors = string.Join(
                ", ",
                result.Errors.Select(x => x.Description));

            throw new InvalidOperationException(errors);
        }

        return user.Id;
    }

    // Assigns the Customer role to an existing user.
    public async Task AddCustomerRoleAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var user =
            await _userManager.FindByIdAsync(
                userId.ToString());

        // Stop if the specified user does not exist.
        if (user is null)
            throw new InvalidOperationException(
                "User not found.");

        var result =
            await _userManager.AddToRoleAsync(
                user,
                "Customer");

        // Throw an exception if the role assignment fails.
        if (!result.Succeeded)
        {
            var errors = string.Join(
                ", ",
                result.Errors.Select(x => x.Description));

            throw new InvalidOperationException(errors);
        }
    }

    // Increments the user's token version to invalidate tokens based on the previous version.
    public async Task IncrementTokenVersionAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var user =
            await _userManager.FindByIdAsync(
                userId.ToString());

        // Stop if the specified user does not exist.
        if (user is null)
            throw new InvalidOperationException(
                "User not found.");

        // Update the user's token version.
        user.IncrementTokenVersion();

        // Persist the updated user information.
        var result =
            await _userManager.UpdateAsync(user);

        // Throw an exception if the update fails.
        if (!result.Succeeded)
        {
            var errors = string.Join(
                ", ",
                result.Errors.Select(x => x.Description));

            throw new InvalidOperationException(errors);
        }
    }
}