using System.ComponentModel.DataAnnotations;

namespace Feryal.Application.DTOs.Auth;

public class RegisterRequest
{
    // Gets or sets the user's first name.
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    // Gets or sets the user's last name.
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    // Gets or sets the user's email address.
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    // Gets or sets the username used to identify the user.
    [Required]
    [MinLength(3)]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    // Gets or sets the password for the new account.
    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    // Gets or sets the password confirmation, which must match the password.
    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;
}