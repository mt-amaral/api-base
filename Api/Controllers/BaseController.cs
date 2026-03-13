using System.ComponentModel.DataAnnotations;
using Api.Dto;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class BaseController : ControllerBase
{
    protected async Task<IActionResult?> ValidateRequestAsync<TRequest>(
        TRequest request, IValidator<TRequest> validator, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);

        if (validation.IsValid)
            return null;

        var firstError = validation.Errors
            .Select(x => x.ErrorMessage)
            .FirstOrDefault() ?? "Dados inválidos.";

        return BadRequest(new Response<object?>(null, firstError));
    }
}