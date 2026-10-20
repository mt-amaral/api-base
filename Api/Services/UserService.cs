using Api.Context;
using Api.Dto;
using Api.Dto.User;
using Api.Entities.Identity;
using Api.Services.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class UserService(ApplicationDbContext context,
    UserManager<User> userManager, IUserLoggedService userLoggedService) : IUserService
{

    public async Task<(PagedResponse<List<UserResponseDto>?>, short)> GetUsersAsync(FilterUsersRequestDto request, CancellationToken ct)
    {
        try
        {
            var query =
                from u in context.User.AsNoTracking()
                orderby u.Id
                select new
                {
                    u.Id,
                    Name = u.UserName!,
                    Email = u.Email!,
                    RoleId = context.Set<IdentityUserRole<long>>()
                        .Where(ur => ur.UserId == u.Id)
                        .OrderBy(ur => ur.RoleId)
                        .Select(ur => (long?)ur.RoleId)
                        .FirstOrDefault()
                };

            if (!string.IsNullOrWhiteSpace(request.SearchString))
            {
                var search = request.SearchString.Trim().ToLower();

                query = query.Where(x =>
                    x.Name.ToLower().Contains(search) ||
                    x.Email.ToLower().Contains(search));
            }

            if (request.RoleId > 0)
            {
                query = query.Where(x => x.RoleId == request.RoleId);
            }

            var totalCount = await query.CountAsync(ct);

            var users = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new UserResponseDto(
                    x.Id,
                    x.Name,
                    x.Email,
                    x.RoleId
                ))
                .ToListAsync(ct);

            return (
                new PagedResponse<List<UserResponseDto>?>(users, totalCount, request.PageNumber, request.PageSize, null),
                200
            );
        }
        catch (Exception)
        {
            return (
                new PagedResponse<List<UserResponseDto>?>(null, 0, request.PageNumber, request.PageSize, "Erro ao recuperar usuários"),
                500
            );
        }
    }

    public async Task<(Response<UserResponseDto?>, short)> CreateAsync(CreateRequestDto request, CancellationToken ct)
    {
        try
        {
            var entity = await userManager.FindByEmailAsync(request.Email);
            if (entity != null)
                return (new Response<UserResponseDto?>(null, "Já existe um usuário registrado com esse email."), 400);

            if (string.IsNullOrWhiteSpace(request.NewPassword))
                return (new Response<UserResponseDto?>(null, "Senha é obrigatória."), 400);

            if (request.NewPassword != request.ConfirmPassword)
                return (new Response<UserResponseDto?>(null, "As senhas não conferem."), 400);

            var roleExists = await RoleExistsAsync(request.RoleId, ct);
            if (!roleExists)
                return (new Response<UserResponseDto?>(null, "Perfil não encontrado."), 400);

            var user = new User(userName: request.UserName, email: request.Email);

            await using var transaction = await context.Database.BeginTransactionAsync(ct);

            var statusCreate = await userManager.CreateAsync(user, request.NewPassword);
            if (!statusCreate.Succeeded)
                return (new Response<UserResponseDto?>(null, $"Erro ao criar usuário {user.UserName}"), 400);

            await ReplaceRoleAsync(user.Id, request.RoleId, ct);

            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            var response = new UserResponseDto(
                user.Id,
                user.UserName!,
                user.Email!,
                request.RoleId
            );

            return (new Response<UserResponseDto?>(response, $"Usuário {user.UserName} registrado com sucesso!"), 200);
        }
        catch
        {
            return (new Response<UserResponseDto?>(null, $"Erro na criação de usuário: {request.Email}"), 500);
        }
    }

    public async Task<(Response<UserResponseDto?>, short)> UpdateAsnc(long userId, UpdateUserRequestDto userRequest, CancellationToken ct)
    {
        try
        {
            var entity = await userManager.Users
                .FirstOrDefaultAsync(x => x.Id == userId, ct);

            if (entity == null)
                return (new Response<UserResponseDto?>(null, "Usuário não encontrado"), 404);

            if (!await RoleExistsAsync(userRequest.RoleId, ct))
                return (new Response<UserResponseDto?>(null, "Perfil não encontrado"), 400);

            await using var transaction = await context.Database.BeginTransactionAsync(ct);

            var userResult = await UpdateUserDataAsync(entity, userRequest.UserName, userRequest.Email);
            if (userResult is not null)
                return userResult.Value;

            if (!string.IsNullOrWhiteSpace(userRequest.NewPassword))
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(entity);
                var passwordResult = await userManager.ResetPasswordAsync(entity, token, userRequest.NewPassword);
                if (!passwordResult.Succeeded)
                    return (new Response<UserResponseDto?>(null, JoinErrors(passwordResult)), 400);
            }

            await ReplaceRoleAsync(entity.Id, userRequest.RoleId, ct);

            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return await BuildUpdatedResponseAsync(entity, ct);
        }
        catch
        {
            return (new Response<UserResponseDto?>(null, $"Erro na atualização de usuário: {userRequest.Email}"), 500);
        }
    }

    public async Task<(Response<UserResponseDto?>, short)> UpdateLoggedAsnc(UpdateLoggedUserRequestDto request, CancellationToken ct)
    {
        try
        {
            var entity = await userLoggedService.GetUserLoggedAsync();

            await using var transaction = await context.Database.BeginTransactionAsync(ct);

            var userResult = await UpdateUserDataAsync(entity, request.UserName, request.Email);
            if (userResult is not null)
                return userResult.Value;

            if (!string.IsNullOrWhiteSpace(request.NewPassword))
            {
                if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                    return (new Response<UserResponseDto?>(null, "Senha atual é obrigatória para trocar a senha."), 400);

                var passwordResult = await userManager.ChangePasswordAsync(entity, request.CurrentPassword, request.NewPassword);
                if (!passwordResult.Succeeded)
                    return (new Response<UserResponseDto?>(null, JoinErrors(passwordResult)), 400);
            }

            await transaction.CommitAsync(ct);

            return await BuildUpdatedResponseAsync(entity, ct);
        }
        catch
        {
            return (new Response<UserResponseDto?>(null, $"Erro na atualização de usuário: {request.Email}"), 500);
        }
    }

    private async Task<(Response<UserResponseDto?>, short)?> UpdateUserDataAsync(User entity, string userName, string email)
    {
        entity.UserName = userName;
        entity.Email = email;

        var updateUserResult = await userManager.UpdateAsync(entity);
        if (updateUserResult.Succeeded)
            return null;

        return (new Response<UserResponseDto?>(null, JoinErrors(updateUserResult)), 400);
    }

    private async Task<(Response<UserResponseDto?>, short)> BuildUpdatedResponseAsync(User entity, CancellationToken ct)
    {
        var roleId = await context.Set<IdentityUserRole<long>>()
            .AsNoTracking()
            .Where(x => x.UserId == entity.Id)
            .OrderBy(x => x.RoleId)
            .Select(x => (long?)x.RoleId)
            .FirstOrDefaultAsync(ct);

        var response = new UserResponseDto(
            entity.Id,
            entity.UserName!,
            entity.Email!,
            roleId
        );

        return (new Response<UserResponseDto?>(response, $"Usuário {response.Name} atualizado com sucesso"), 200);
    }

    private static string JoinErrors(IdentityResult result) =>
        string.Join(" | ", result.Errors.Select(x => x.Description));


    public async Task<(Response<bool>, short)> DeleteAsync(long userId, CancellationToken ct)
    {
        try
        {
            var entity = await context.User
                .FirstOrDefaultAsync(x => x.Id == userId, ct);

            if (entity == null)
                return (new Response<bool>(false, "Usuário não encontrado"), 404);

            var result = await userManager.DeleteAsync(entity);

            if (!result.Succeeded)
                return (new Response<bool>(false, "Erro ao remover usuário"), 400);

            return (new Response<bool>(true, "Usuário removido com sucesso"), 200);
        }
        catch
        {
            return (new Response<bool>(false, "Erro ao remover usuário"), 500);
        }
    }


    private async Task ReplaceRoleAsync(long userId, long roleId, CancellationToken ct)
    {
        var userRoles = await context.Set<IdentityUserRole<long>>()
            .Where(x => x.UserId == userId)
            .ToListAsync(ct);

        if (userRoles.Count == 1 && userRoles[0].RoleId == roleId)
            return;

        context.RemoveRange(userRoles);
        context.Add(new IdentityUserRole<long>
        {
            UserId = userId,
            RoleId = roleId
        });
    }

    private async Task<bool> RoleExistsAsync(long roleId, CancellationToken ct)
    {
        return await context.Roles
            .AsNoTracking()
            .AnyAsync(x => x.Id == roleId, ct);
    }
}

