using Domain.Common;

namespace Domain.Entities;

public class Cart : AuditableEntity
{
    public string SessionId { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string Status { get; set; } = "ACTIVE";

    public ICollection<CartItem> CartItems { get; set; } = [];
}
