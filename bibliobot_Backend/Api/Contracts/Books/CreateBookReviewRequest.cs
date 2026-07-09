using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Books;

public sealed class CreateBookReviewRequest
{
    [Range(1, 5)]
    public int Rating { get; init; }

    [Required]
    [StringLength(1000, MinimumLength = 5)]
    public string Comment { get; init; } = string.Empty;
}
