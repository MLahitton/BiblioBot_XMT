using Domain.Common;

namespace Domain.Entities;

public class Author : AuditableEntity
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<BookAuthor> BookAuthors { get; set; } = [];
}
