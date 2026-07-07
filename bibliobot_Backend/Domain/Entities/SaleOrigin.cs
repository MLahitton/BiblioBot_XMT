using Domain.Common;

namespace Domain.Entities;

public class SaleOrigin : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<Sale> Sales { get; set; } = [];
}
