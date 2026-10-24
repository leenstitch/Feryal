namespace Feryal.Application.DTOs.Auth;

public class AuthResponse
{
    // Gets or sets the JWT access token used to authenticate API requests.
    public string AccessToken { get; set; } = string.Empty;

    // Gets or sets the refresh token used to obtain a new access token.
    public string RefreshToken { get; set; } = string.Empty;

    // Gets or sets the date and time when the access token expires.
    public DateTimeOffset AccessTokenExpiresAt { get; set; }
}