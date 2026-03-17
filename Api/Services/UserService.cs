using Api.Context;
using Api.Dto;
using Api.Dto.User;
using Api.Services.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class UserService(ApplicationDbContext context) : IUserService
{
    
    public async Task<(PagedResponse<List<UserResponse>?>, short)> GetUsersAsync(CancellationToken ct)
    {
        var page = 1;
        var pageSize = 10;
        // TODO Remover isso aqui 

        try
        {
            var baseQuery =
                from u in context.User.AsNoTracking()
                join ur in context.Set<IdentityUserRole<long>>().AsNoTracking()
                    on u.Id equals ur.UserId
                orderby u.Id
                select new
                {
                    u.Id,
                    Name = u.UserName!,
                    Email = u.Email!,
                    ur.RoleId
                };

            var totalCount = await context.User
                .AsNoTracking()
                .CountAsync(ct);

            var users = await baseQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new UserResponse(
                    x.Id,
                    x.Name,
                    x.Email,
                    x.RoleId
                ))
                .ToListAsync(ct);
            
            return (
                new PagedResponse<List<UserResponse>?>(users, totalCount, page, pageSize, null),
                200
            );
        }
        catch (Exception)
        {
            return (
                new PagedResponse<List<UserResponse>?>(null, 0, page, pageSize, "Erro ao recuperar usuários"),
                500
            );
        }
    }
}

