using Api.Configurations;
using Api.Configurations.Identity;
using Api.Dto.Account;
using Api.Services.Abstractions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.V1;


public class AccountController(IAccountServices accountServices, IValidator<RegisterRequestDto> registerValidator) : BaseController
{
    
    [HttpPost]
    [Route("Register")]
    [Authorize(Policy = Permissions.UsersRegister)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken ct)
    {
        var validationResult = await ValidateRequestAsync(request, registerValidator, ct);
        if (validationResult is not null)
            return validationResult;

        var (data, status) = await accountServices.RegisterAsync(request, ct);
        return StatusCode(status, data);
    }

    [HttpPost]
    [Route("Login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var (data, status) = await accountServices.LoginAsync(request, ct);
        return StatusCode(status, data);
    }

    [HttpPost]
    [Route("Refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var (data, status) = await accountServices.RefreshTokenAsync(ct);
        return StatusCode(status, data);
    }

    [HttpPost]
    [Route("Logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var (data, status) = await accountServices.LogoutAsync(ct);
        return StatusCode(status, data);
    }
    
}