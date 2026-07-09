namespace Api.Contracts.Branches;

public sealed class UpdateBranchRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
}

