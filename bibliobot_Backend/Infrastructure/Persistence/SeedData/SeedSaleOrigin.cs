namespace Infrastructure.Persistence.SeedData;

public sealed class SeedSaleOrigin
{
    public SeedSaleOrigin(Guid id, string code, string name)
    {
        Id = id;
        Code = code;
        Name = name;
    }

    public Guid Id { get; }
    public string Code { get; }
    public string Name { get; }
}
