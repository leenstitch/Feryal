using Feryal.Application.DTOs.Auth;
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using System.Data;
using Feryal.Application.Exceptions;
namespace Feryal.Application.Services;

public class AuthService : IAuthService
{
    private readonly IIdentityService _identityService;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IRefreshTokenHasher _refreshTokenHasher;

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