using Domain.Common;

namespace Domain.Entities;

public class Publisher : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<Book> Books { get; set; } = [];
}
