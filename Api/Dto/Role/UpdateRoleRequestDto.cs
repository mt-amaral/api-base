using System.ComponentModel.DataAnnotations;

namespace Api.Dto.Role;

public record UpdateRoleRequestDto
{
    [Range(1, long.MaxValue, ErrorMessage = "Id inválido.")]
    public long Id { get; init; }

    [Required(ErrorMessage = "O nome do perfil é obrigatório.")]
    [StringLength(100, ErrorMessage = "O nome do perfil deve ter no máximo 100 caracteres.")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "Descrição é obrigatória.")]
    [StringLength(255, ErrorMessage = "A descrição do perfil deve ter no máximo 255 caracteres.")]
    public string Description { get; init; } = string.Empty;
}
