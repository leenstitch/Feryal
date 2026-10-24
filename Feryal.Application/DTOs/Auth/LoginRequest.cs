using System.ComponentModel.DataAnnotations;

namespace Feryal.Application.DTOs.Auth;

public class LoginRequest
{
    // Gets or sets the user's email address used for authentication.
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    // Gets or sets the user's password.
    [Required]
    public string Password { get; set; } = string.Empty;
}