namespace Application.Features.Branches.Common;

public sealed class BranchDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
    public bool IsActive { get; init; }
}

