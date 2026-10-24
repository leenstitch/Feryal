using Feryal.Application.DTOs.Auth;
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using System.Data;
using Feryal.Application.Exceptions;

namespace Feryal.Application.Services;

// Handles user registration, login, token refresh, and logout operations.
public class AuthService : IAuthService
{
    // Provides user identity operations, including account lookup and validation.
    private readonly IIdentityService _identityService;

    // Generates JWT access tokens.
    private readonly IJwtService _jwtService;

    // Generates refresh tokens and determines their expiration dates.
    private readonly IRefreshTokenService _refreshTokenService;

    // Manages refresh token persistence and revocation.
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    // Executes operations within database transactions.
    private readonly ITransactionManager _transactionManager;

    // Hashes refresh tokens before they are stored or searched in the database.
    private readonly IRefreshTokenHasher _refreshTokenHasher;

    // Initializes the authentication service with its required dependencies.
    public AuthService(
        IIdentityService identityService,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService,
        IRefreshTokenRepository refreshTokenRepository,
        ITransactionManager transactionManager,
        IRefreshTokenHasher refreshTokenHasher)
    {
        _identityService = identityService;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _transactionManager = transactionManager;
        _refreshTokenHasher = refreshTokenHasher;
    }

    // Registers a new user, assigns the customer role, and generates authentication tokens.
    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        request.Email = request.Email.Trim();
        request.Username = request.Username.Trim();

        var existingEmail =
            await _identityService.FindUserIdByEmailAsync(
                request.Email,
                cancellationToken);

        if (existingEmail.HasValue)
            throw new ConflictException(
                "Email is already registered.");

        var existingUsername =
            await _identityService.FindUserIdByUsernameAsync(
                request.Username,
                cancellationToken);

        if (existingUsername.HasValue)
            throw new ConflictException(
                "Username is already taken.");

        return await _transactionManager.ExecuteAsync(
            async ct =>
            {
                var userId =
                    await _identityService.CreateUserAsync(
                        request,
                        ct);

                await _identityService.AddCustomerRoleAsync(
                    userId,
                    ct);

                return await CreateAuthResponseAsync(
                    userId,
                    ct);
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }

    // Authenticates a user by validating their email, account status, lockout status, and password.
    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        request.Email = request.Email.Trim();

        var userId =
            await _identityService.FindUserIdByEmailAsync(
                request.Email,
                cancellationToken);

        if (!userId.HasValue)
            throw new UnauthorizedAccessException(
                "Invalid email or password.");

        var isActive =
            await _identityService.IsUserActiveAsync(
                userId.Value,
                cancellationToken);

        if (!isActive)
            throw new UnauthorizedAccessException(
                "Invalid email or password.");

        var isLockedOut =
            await _identityService.IsLockedOutAsync(
                userId.Value,
                cancellationToken);

        if (isLockedOut)
            throw new UnauthorizedAccessException(
                "Invalid email or password.");

        var passwordValid =
            await _identityService.CheckPasswordAsync(
                userId.Value,
                request.Password,
                cancellationToken);

        if (!passwordValid)
            throw new UnauthorizedAccessException(
                "Invalid email or password.");

        return await _transactionManager.ExecuteAsync(
            ct => CreateAuthResponseAsync(
                userId.Value,
                ct),
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }

    // Validates an existing refresh token, rotates it, and generates a new access token.
    public async Task<AuthResponse> RefreshAsync(
        RefreshRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(
                request.RefreshToken))
        {
            throw new UnauthorizedAccessException(
                "Invalid refresh token.");
        }

        var tokenHash =
            _refreshTokenHasher.Hash(
                request.RefreshToken);

        return await _transactionManager.ExecuteAsync(
            async ct =>
            {
                var storedToken =
                    await _refreshTokenRepository
                        .GetByTokenHashAsync(
                            tokenHash,
                            ct);

                if (storedToken is null)
                    throw new UnauthorizedAccessException(
                        "Invalid refresh token.");

                if (!storedToken.IsActive)
                {
                    if (storedToken.WasReplaced)
                    {
                        await _identityService
                            .IncrementTokenVersionAsync(
                                storedToken.UserId,
                                ct);

                        await _refreshTokenRepository
                            .RevokeActiveTokensAsync(
                                storedToken.UserId,
                                ct);
                    }

                    throw new UnauthorizedAccessException(
                        "Invalid refresh token.");
                }

                var isActive =
                    await _identityService
                        .IsUserActiveAsync(
                            storedToken.UserId,
                            ct);

                if (!isActive)
                    throw new UnauthorizedAccessException(
                        "Invalid refresh token.");

                var newRawRefreshToken =
                    _refreshTokenService.GenerateToken();

                var newRefreshTokenHash =
                    _refreshTokenHasher.Hash(
                        newRawRefreshToken);

                storedToken.ReplaceWith(
                    newRefreshTokenHash);

                await _refreshTokenRepository
                    .SaveChangesAsync(ct);

                var accessToken =
                    await _jwtService
                        .GenerateAccessTokenAsync(
                            storedToken.UserId);

                var newRefreshToken =
                    new RefreshToken(
                        storedToken.UserId,
                        newRefreshTokenHash,
                        _refreshTokenService
                            .GetExpirationDate());

                await _refreshTokenRepository
                    .AddAsync(
                        newRefreshToken,
                        ct);

                await _refreshTokenRepository
                    .SaveChangesAsync(ct);

                return new AuthResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = newRawRefreshToken,
                    AccessTokenExpiresAt =
                        _jwtService
                            .GetAccessTokenExpirationDate()
                };
            },
            IsolationLevel.Serializable,
            cancellationToken);
    }

    // Increments the user's token version and revokes all active refresh tokens.
    public async Task LogoutAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        await _transactionManager.ExecuteAsync(
            async ct =>
            {
                await _identityService
                    .IncrementTokenVersionAsync(
                        userId,
                        ct);

                await _refreshTokenRepository
                    .RevokeActiveTokensAsync(
                        userId,
                        ct);

                await _refreshTokenRepository
                    .SaveChangesAsync(ct);
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }

    // Generates authentication tokens, stores the hashed refresh token, and builds the authentication response.
    private async Task<AuthResponse>
        CreateAuthResponseAsync(
            int userId,
            CancellationToken cancellationToken)
    {
        var accessToken =
            await _jwtService
                .GenerateAccessTokenAsync(userId);

        var rawRefreshToken =
            _refreshTokenService.GenerateToken();

        var refreshTokenHash =
            _refreshTokenHasher.Hash(
                rawRefreshToken);

        var refreshToken =
            new RefreshToken(
                userId,
                refreshTokenHash,
                _refreshTokenService
                    .GetExpirationDate());

        await _refreshTokenRepository.AddAsync(
            refreshToken,
            cancellationToken);

        await _refreshTokenRepository.SaveChangesAsync(
            cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            AccessTokenExpiresAt =
                _jwtService
                    .GetAccessTokenExpirationDate()
        };
    }
}