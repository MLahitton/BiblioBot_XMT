using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Admin;

public sealed class CreateAdminUserRequest
{
    [Required]
    [StringLength(160)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(180)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [StringLength(128)]
    public string Password { get; set; } = string.Empty;

    [StringLength(40)]
    public string? Phone { get; set; }

    [StringLength(50)]
    public string? DocumentNumber { get; set; }

    [Required]
    [MinLength(1)]
    public IReadOnlyCollection<string> RoleCodes { get; set; } = [];
}
