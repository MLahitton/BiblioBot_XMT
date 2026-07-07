namespace Api.Contracts.Branches;

public sealed class CreateBranchRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
}

