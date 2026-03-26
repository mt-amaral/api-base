using System.ComponentModel.DataAnnotations;

namespace Api.Dto.Role;

public record FilterRoleRequestDto : PaginationRequestDto
{
    [StringLength(100, ErrorMessage = "SearchString deve ter no máximo 100 caracteres.")]
    public string? SearchString { get; init; }
}