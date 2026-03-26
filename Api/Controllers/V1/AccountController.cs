using Api.Dto.Account;
using Api.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.V1;


public class AccountController(IAccountServices accountServices) : BaseController
{


    [HttpPost]
    [Route("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var (data, status) = await accountServices.LoginAsync(request, ct);
        return StatusCode(status, data);
    }

    [HttpPost]
    [Route("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var (data, status) = await accountServices.RefreshTokenAsync(ct);
        return StatusCode(status, data);
    }

    [HttpGet]
    [Route("checkme")]
    public async Task<IActionResult> CheckMe(CancellationToken ct)
    {
        var (data, status) = await accountServices.CheckMe(ct);
        return StatusCode(status, data);
    }

    [HttpPost]
    [Route("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var (data, status) = await accountServices.LogoutAsync(ct);
        return StatusCode(status, data);
    }

}