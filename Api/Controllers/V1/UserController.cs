using Api.Dto.Account;
using Api.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.V1;

public class UserController(IUserService userService) : BaseController
{
    
    [HttpGet]
    [Route("list-users")]
    public async Task<IActionResult> ListUsers(CancellationToken ct)
    {
        var (data, status) = await userService.GetUsersAsync(ct);
        return StatusCode(status, data);
    }
    
}