namespace Infrastructure.Persistence.SeedData;

public sealed class SeedSaleStatus
{
    public SeedSaleStatus(Guid id, string code, string name)
    {
        Id = id;
        Code = code;
        Name = name;
    }

    public Guid Id { get; }
    public string Code { get; }
    public string Name { get; }
}
