using System.ComponentModel.DataAnnotations;

namespace Api.Dto.Account;

public record LoginRequestDto
{
    [Required(ErrorMessage = "Email é obrigatório.")]
    [EmailAddress(ErrorMessage = "Email inválido.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória.")]
    [MinLength(6, ErrorMessage = "Senha deve ter no mínimo 6 caracteres.")]
    public string Password { get; init; } = string.Empty;

    public bool RememberMe { get; init; }
}
