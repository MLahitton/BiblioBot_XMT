using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Auth;

public sealed class RegisterRequest
{
    [Required]
    [StringLength(150)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [StringLength(180)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    [StringLength(100)]
    public string Password { get; init; } = string.Empty;

    [StringLength(40)]
    public string? Phone { get; init; }

    [StringLength(50)]
    public string? DocumentNumber { get; init; }
}
