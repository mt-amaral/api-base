using Api.Configurations.Identity;
using Api.Dto.RoleClaim;
using Api.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.V1;

public class RoleClaimController(IRoleClaimService roleClaimService) : BaseController
{
    [HttpGet]
    [Route("{roleId:long}")]
    [Authorize(Policy = Permissions.ClaimsView)]
    public async Task<IActionResult> GetByRoleId(long roleId, CancellationToken ct)
    {
        var (data, status) = await roleClaimService.GetByRoleIdAsync(roleId, ct);
        return StatusCode(status, data);
    }

    [HttpPost]
    [Route("update")]
    [Authorize(Policy = Permissions.ClaimsUpdate)]
    public async Task<IActionResult> Update([FromBody] UpdateRoleClaimRequestDto request, CancellationToken ct)
    {
        var (data, status) = await roleClaimService.UpdateAsync(request, ct);
        return StatusCode(status, data);
    }
}