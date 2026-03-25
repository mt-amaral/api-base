using System.ComponentModel.DataAnnotations;

public record UpdateUserRequestDto
{

    [Required(ErrorMessage = "UserName é obrigatório.")]
    [MinLength(3, ErrorMessage = "UserName deve ter no mínimo 3 caracteres.")]
    [MaxLength(100, ErrorMessage = "UserName deve ter no máximo 100 caracteres.")]
    public string UserName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Email é obrigatório.")]
    [EmailAddress(ErrorMessage = "Email inválido.")]
    public string Email { get; init; } = string.Empty;

    [MinLength(6, ErrorMessage = "NewPassword deve ter no mínimo 6 caracteres.")]
    [MaxLength(100, ErrorMessage = "NewPassword deve ter no máximo 100 caracteres.")]
    public string? NewPassword { get; init; }

    [Compare("NewPassword", ErrorMessage = "ConfirmPassword deve ser igual a NewPassword.")]
    public string? ConfirmPassword { get; init; }

    [Range(1, long.MaxValue, ErrorMessage = "RoleId inválido.")]
    public long RoleId { get; init; }
}