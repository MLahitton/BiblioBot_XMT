namespace Infrastructure.Persistence.SeedData;

public sealed class SeedUser
{
    public SeedUser(
        Guid id,
        string fullName,
        string email,
        string tempPassword,
        bool isActive = true)
    {
        Id = id;
        FullName = fullName;
        Email = email;
        TempPassword = tempPassword;
        IsActive = isActive;
    }

    public Guid Id { get; }
    public string FullName { get; }
    public string Email { get; }
    public string TempPassword { get; }
    public bool IsActive { get; }
    public string RoleCode { get; init; } = Domain.Constants.RoleCodes.Admin;
}
