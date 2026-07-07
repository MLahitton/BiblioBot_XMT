using Domain.Common;

namespace Domain.Entities;

public class SaleStatus : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<Sale> Sales { get; set; } = [];
}
