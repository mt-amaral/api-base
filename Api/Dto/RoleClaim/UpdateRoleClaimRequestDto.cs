using System.ComponentModel.DataAnnotations;

namespace Api.Dto.RoleClaim;

public class UpdateRoleClaimRequestDto
{
    [Range(1, long.MaxValue, ErrorMessage = "RoleId inválido.")]
    public long RoleId { get; set; }

    [Required(ErrorMessage = "A lista de claims é obrigatória.")]
    [MinLength(1, ErrorMessage = "A lista de claims deve conter pelo menos um item.")]
    public List<string> Claims { get; set; } = [];
}
