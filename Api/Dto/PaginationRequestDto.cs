using System.ComponentModel.DataAnnotations;

namespace Api.Dto;

public record PaginationRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "PageNumber deve ser maior que 0.")]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100, ErrorMessage = "PageSize deve estar entre 1 e 100.")]
    public int PageSize { get; init; } = 10;
}