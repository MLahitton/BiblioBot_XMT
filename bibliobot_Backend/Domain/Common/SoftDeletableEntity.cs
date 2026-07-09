namespace Domain.Common;

public abstract class SoftDeletableEntity : AuditableEntity
{
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
