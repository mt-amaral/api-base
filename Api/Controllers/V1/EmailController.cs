using Api.Configurations.Identity;
using Api.Dto.Email;
using Api.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.V1;

[Route("api/v1/emails")]
public class EmailController(IEmailService emailService) : BaseController
{
    [HttpPost]
    [Authorize(Policy = Permissions.EmailsSend)]
    public async Task<IActionResult> Send([FromBody] SendEmailRequestDto request, CancellationToken ct)
    {
        var (data, status) = await emailService.SendAsync(request, ct);
        return StatusCode(status, data);
    }
}
