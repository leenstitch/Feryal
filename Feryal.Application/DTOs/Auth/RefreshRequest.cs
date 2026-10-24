using System.ComponentModel.DataAnnotations;

namespace Feryal.Application.DTOs.Auth;

public class RefreshRequest
{
    [Required]
    [MinLength(30)]
    [MaxLength(500)]
    public string RefreshToken { get; set; } = string.Empty;
}