using System.ComponentModel.DataAnnotations;

namespace Api.Dto.User;

public class UpdateLoggedUserRequestDto
{
    [Required(ErrorMessage = "UserName é obrigatório.")]
    [MinLength(3, ErrorMessage = "UserName deve ter no mínimo 3 caracteres.")]
    [MaxLength(100, ErrorMessage = "UserName deve ter no máximo 100 caracteres.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email é obrigatório.")]
    [EmailAddress(ErrorMessage = "Email inválido.")]
    public string Email { get; set; } = string.Empty;

    public string? CurrentPassword { get; set; }

    [MinLength(6, ErrorMessage = "NewPassword deve ter no mínimo 6 caracteres.")]
    [MaxLength(100, ErrorMessage = "NewPassword deve ter no máximo 100 caracteres.")]
    public string? NewPassword { get; set; }

    [Compare("NewPassword", ErrorMessage = "ConfirmPassword deve ser igual a NewPassword.")]
    public string? ConfirmPassword { get; set; }
}
