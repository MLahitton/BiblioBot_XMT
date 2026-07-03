using Domain.Common;

namespace Domain.Entities;

public class InternalRequest : BaseEntity
{
    public Guid RequestTypeId { get; set; }
    public RequestType RequestType { get; set; } = null!;
    public Guid StatusId { get; set; }
    public RequestStatus Status { get; set; } = null!;
    public Guid ActorId { get; set; }
    public User Actor { get; set; } = null!;
    public Guid? SourceBranchId { get; set; }
    public Branch? SourceBranch { get; set; }
    public Guid? TargetBranchId { get; set; }
    public Branch? TargetBranch { get; set; }
    public string? Description { get; set; }
    public string? Observations { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset? ExecutedAt { get; set; }

    public ICollection<InternalRequestItem> Items { get; set; } = [];
}
