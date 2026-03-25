using System.ComponentModel.DataAnnotations;

public record CreateRequestDto
{
    [Required(ErrorMessage = "UserName é obrigatório.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "UserName deve ter entre 3 e 100 caracteres.")]
    public string UserName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Email é obrigatório.")]
    [EmailAddress(ErrorMessage = "Email inválido.")]
    public string Email { get; init; } = string.Empty;

    [StringLength(100, MinimumLength = 6, ErrorMessage = "NewPassword deve ter entre 6 e 100 caracteres.")]
    public string? NewPassword { get; init; }

    [Compare("NewPassword", ErrorMessage = "ConfirmPassword deve ser igual a NewPassword.")]
    public string? ConfirmPassword { get; init; }

    [Range(1, long.MaxValue, ErrorMessage = "RoleId deve ser maior que 0.")]
    public long RoleId { get; init; }
}