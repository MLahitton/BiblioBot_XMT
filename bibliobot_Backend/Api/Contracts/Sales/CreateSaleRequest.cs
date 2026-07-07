namespace Api.Contracts.Sales;

public sealed class CreateSaleRequest
{
    public string SessionId { get; init; } = string.Empty;
    public Guid? BranchId { get; init; }
    public string? OriginCode { get; init; }
}

