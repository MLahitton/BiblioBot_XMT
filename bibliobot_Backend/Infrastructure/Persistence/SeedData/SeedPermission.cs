namespace Infrastructure.Persistence.SeedData;

public sealed class SeedPermission
{
    public SeedPermission(Guid id, string code, string name, string? description)
    {
        Id = id;
        Code = code;
        Name = name;
        Description = description;
    }

    public Guid Id { get; }
    public string Code { get; }
    public string Name { get; }
    public string? Description { get; }
}
