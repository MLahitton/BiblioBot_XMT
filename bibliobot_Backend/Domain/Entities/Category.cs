using Domain.Common;

namespace Domain.Entities;

public class Category : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<BookCategory> BookCategories { get; set; } = [];
}
