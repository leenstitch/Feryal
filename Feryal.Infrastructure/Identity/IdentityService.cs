using Feryal.Application.DTOs.Auth;
using Feryal.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Feryal.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(
        UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<int?> FindUserIdByEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
        var user =
            await _userManager.FindByEmailAsync(
                email);

        return user?.Id;
    }

    public async Task<int?> FindUserIdByUsernameAsync(
        string username,
        CancellationToken cancellationToken)
    {
        var user =
            await _userManager.FindByNameAsync(
                username);

        return user?.Id;
    }

    public async Task<bool> CheckPasswordAsync(
        int userId,
        string password,
        CancellationToken cancellationToken)
    {
        var user =
            await _userManager.FindByIdAsync(
                userId.ToString());

        if (user is null)
            return false;

        return await _userManager.CheckPasswordAsync(
            user,
            password);
    }

    public async Task<bool> IsUserActiveAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var user =
            await _userManager.FindByIdAsync(
                userId.ToString());

        return user is not null && user.IsActive;
    }

    public async Task<bool> IsEmailConfirmedAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var user =
            await _userManager.FindByIdAsync(
                userId.ToString());

        return user?.EmailConfirmed == true;
    }

    public async Task<bool> IsLockedOutAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var user =
            await _userManager.FindByIdAsync(
                userId.ToString());

        if (user is null)
            return true;

        return await _userManager.IsLockedOutAsync(user);
    }

    public async Task<int> CreateUserAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var user = new ApplicationUser(
            request.FirstName.Trim(),
            request.LastName.Trim(),
            request.Email.Trim());

        user.UserName = request.Username.Trim();

        var result =
            await _userManager.CreateAsync(
                user,
                request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                ", ",
                result.Errors.Select(x => x.Description));

            throw new InvalidOperationException(errors);
        }

        return user.Id;
    }

    public async Task AddCustomerRoleAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var user =
            await _userManager.FindByIdAsync(
                userId.ToString());

        if (user is null)
            throw new InvalidOperationException(
                "User not found.");

        var result =
            await _userManager.AddToRoleAsync(
                user,
                "Customer");

        if (!result.Succeeded)
        {
            var errors = string.Join(
                ", ",
                result.Errors.Select(x => x.Description));

            throw new InvalidOperationException(errors);
        }
    }

    public async Task IncrementTokenVersionAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var user =
            await _userManager.FindByIdAsync(
                userId.ToString());

        if (user is null)
            throw new InvalidOperationException(
                "User not found.");

        user.IncrementTokenVersion();

        var result =
            await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                ", ",
                result.Errors.Select(x => x.Description));

            throw new InvalidOperationException(errors);
        }
    }
}