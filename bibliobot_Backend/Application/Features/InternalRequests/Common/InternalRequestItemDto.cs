namespace Application.Features.InternalRequests.Common;

public sealed class InternalRequestItemDto
{
    public Guid Id { get; init; }
    public Guid BookId { get; init; }
    public string BookTitle { get; init; } = string.Empty;
    public string? Isbn { get; init; }
    public int Quantity { get; init; }
}
