using Domain.Common;

namespace Domain.Entities;

public class UserFavoriteBook : AuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;
}
