using Api.Dto;
using Api.Dto.Email;

namespace Api.Services.Abstractions;

public interface IEmailService
{
    Task<(Response<EmailQueuedResponseDto?>, short)> SendAsync(SendEmailRequestDto request, CancellationToken ct);
}
