using System.ComponentModel.DataAnnotations;

namespace Feryal.Application.DTOs.Auth;

public class RefreshRequest
{
    // Gets or sets the refresh token used to obtain a new access token.
    [Required]
    [MinLength(30)]
    [MaxLength(500)]
    public string RefreshToken { get; set; } = string.Empty;
}