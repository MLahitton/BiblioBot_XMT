using Domain.Common;

namespace Domain.Entities;

public class BookReview : AuditableEntity
{
    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public bool IsVerifiedPurchase { get; set; }
}
