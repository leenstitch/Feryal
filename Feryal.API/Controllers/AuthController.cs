using Feryal.Application.DTOs.Auth;
using Feryal.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Feryal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    // Initializes the controller with the authentication service.
    public AuthController(
        IAuthService authService)
    {
        _authService = authService;
    }

    // Registers a new user and returns the authentication response.
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var response =
            await _authService.RegisterAsync(
                request,
                cancellationToken);

        return Ok(response);
    }

    // Authenticates a user and returns the authentication response.
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response =
            await _authService.LoginAsync(
                request,
                cancellationToken);

        return Ok(response);
    }

    // Refreshes the user's authentication tokens.
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(
        RefreshRequest request,
        CancellationToken cancellationToken)
    {
        var response =
            await _authService.RefreshAsync(
                request,
                cancellationToken);

        return Ok(response);
    }

    // Logs out the authenticated user by invalidating their active authentication session.
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        CancellationToken cancellationToken)
    {
        var userIdClaim =
            User.FindFirst(
                ClaimTypes.NameIdentifier);

        if (userIdClaim is null ||
            !int.TryParse(
                userIdClaim.Value,
                out var userId))
        {
            return Unauthorized();
        }

        await _authService.LogoutAsync(
            userId,
            cancellationToken);

        return NoContent();
    }
}