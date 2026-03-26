using Api.Configurations.Identity;
using Api.Dto.User;
using Api.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.V1;

public class UserController(IUserService userService) : BaseController
{

    [HttpGet]
    [Route("list-users")]
    [Authorize(Policy = Permissions.UsersView)]
    public async Task<IActionResult> ListUsers([FromQuery] FilterUsersRequestDto request, CancellationToken ct)
    {
        var (data, status) = await userService.GetUsersAsync(request, ct);
        return StatusCode(status, data);
    }

    [HttpPost]
    [Route("create")]
    [Authorize(Policy = Permissions.UsersRegister)]
    public async Task<IActionResult> Create([FromBody] CreateRequestDto request, CancellationToken ct)
    {
        var (data, status) = await userService.CreateAsync(request, ct);
        return StatusCode(status, data);
    }

    [HttpPost]
    [Route("update")]
    [Authorize(Policy = Permissions.UsersUpdate)]
    public async Task<IActionResult> Update([FromQuery] long UserId, [FromBody] UpdateUserRequestDto userRequest, CancellationToken ct)
    {
        var (data, status) = await userService.UpdateAsnc(UserId, userRequest, ct);
        return StatusCode(status, data);
    }

    [HttpPost]
    [Route("update-logged")]
    public async Task<IActionResult> UpdateUserLogged([FromBody] UpdateUserRequestDto userRequest, CancellationToken ct)
    {
        var (data, status) = await userService.UpdateLoggedAsnc(userRequest, ct);
        return StatusCode(status, data);
    }

    [HttpDelete]
    [Route("delete/{id:long}")]
    [Authorize(Policy = Permissions.UsersDelete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var (data, status) = await userService.DeleteAsync(id, ct);
        return StatusCode(status, data);
    }
}