using Api.Context;
using Api.Dto;
using Api.Dto.Account;
using Api.Dto.Role;
using Api.Entities.Identity;
using Api.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class RoleServices(ApplicationDbContext context) : IRoleService
{
    public async Task<(Response<List<RoleResponseDto>?>, short)> ListRolesAsync(CancellationToken ct)
    {
        try
        {
            var result = new List<RoleResponseDto>();
            var roles = await context.Roles.AsNoTracking().ToListAsync(ct);

            foreach (var role in roles)
            {
                result.Add(new RoleResponseDto(role.Id, role.Name!, role.Description!));
            }
            return (new Response<List<RoleResponseDto>?>(result, null), 200);
        }
        catch
        {
            return (new Response<List<RoleResponseDto>?>(null, $"Erro ao listar Roles!"), 500);
        }
    }
}