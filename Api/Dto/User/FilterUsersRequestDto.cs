using System.ComponentModel.DataAnnotations;

namespace Api.Dto.User;

public record FilterUsersRequestDto : PaginationRequestDto
{
    [StringLength(100, ErrorMessage = "SearchString deve ter no máximo 100 caracteres.")]
    public string? SearchString { get; init; }

    [Range(1, long.MaxValue, ErrorMessage = "RoleId inválido.")]
    public long? RoleId { get; init; }
}