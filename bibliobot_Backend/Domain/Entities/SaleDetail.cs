using Domain.Common;

namespace Domain.Entities;

public class SaleDetail : BaseEntity
{
    public Guid SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;
    public string BookTitleSnapshot { get; set; } = string.Empty;
    public string? IsbnSnapshot { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
