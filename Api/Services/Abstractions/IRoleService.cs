using Api.Dto;
using Api.Dto.Account;
using Api.Dto.Role;
using Api.Dto.User;

namespace Api.Services.Abstractions;

public interface IRoleService
{
    Task<(Response<List<RoleResponseDto>?>, short)> ListRolesAsync(CancellationToken ct);
}