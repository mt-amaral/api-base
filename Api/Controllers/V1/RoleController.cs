using Api.Dto.Account;
using Api.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.V1;

public class RoleController(IRoleService roleService) : BaseController
{
    
    [HttpGet]
    [Route("list-users")]
    [AllowAnonymous]
    public async Task<IActionResult> ListUsers(CancellationToken ct)
    {
        var (data, status) = await roleService.ListRolesAsync(ct);
        return StatusCode(status, data);
    }
}