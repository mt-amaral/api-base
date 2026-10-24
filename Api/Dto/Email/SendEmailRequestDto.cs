using System.ComponentModel.DataAnnotations;

namespace Api.Dto.Email;

public class SendEmailRequestDto
{
    [Required(ErrorMessage = "O destinatário é obrigatório.")]
    [EmailAddress(ErrorMessage = "Destinatário inválido.")]
    public string To { get; set; } = string.Empty;

    [Required(ErrorMessage = "O assunto é obrigatório.")]
    [StringLength(150, ErrorMessage = "O assunto deve ter no máximo 150 caracteres.")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "O corpo do e-mail é obrigatório.")]
    public string Body { get; set; } = string.Empty;
}
