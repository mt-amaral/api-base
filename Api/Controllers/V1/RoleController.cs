using Api.Configurations.Identity;
using Api.Dto.Role;
using Api.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.V1;

public class RoleController(IRoleService roleService) : BaseController
{
    [HttpGet]
    [Route("list-roles")]
    [Authorize(Policy = Permissions.RolesView)]
    public async Task<IActionResult> ListRolesPage([FromQuery] FilterRoleRequestDto request, CancellationToken ct)
    {
        var (data, status) = await roleService.ListRolesPageAsync(request, ct);
        return StatusCode(status, data);
    }

    [HttpGet]
    [Route("list-all-roles")]
    public async Task<IActionResult> ListRoles(CancellationToken ct)
    {
        var (data, status) = await roleService.ListAllRolesAsync(ct);
        return StatusCode(status, data);
    }

    [HttpPost]
    [Route("create")]
    [Authorize(Policy = Permissions.RolesRegister)]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequestDto request, CancellationToken ct)
    {
        var (data, status) = await roleService.CreateAsync(request, ct);
        return StatusCode(status, data);
    }

    [HttpPost]
    [Route("update")]
    [Authorize(Policy = Permissions.RolesUpdate)]
    public async Task<IActionResult> Update([FromBody] UpdateRoleRequestDto request, CancellationToken ct)
    {
        var (data, status) = await roleService.UpdateAsync(request, ct);
        return StatusCode(status, data);
    }

    [HttpDelete]
    [Route("delete/{id:long}")]
    [Authorize(Policy = Permissions.RolesDelete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var (data, status) = await roleService.DeleteAsync(id, ct);
        return StatusCode(status, data);
    }
}