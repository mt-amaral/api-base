using Api.Entities.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Api.Configurations.Identity;

/// <summary>
/// Adiciona ao usuário autenticado as permissões vindas das roles.
/// Executa a cada requisição, mantendo as permissões sincronizadas com o banco.
/// </summary>
public class AppClaimsTransformation : IClaimsTransformation
{
    private readonly IServiceProvider _serviceProvider;

    public AppClaimsTransformation(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            return principal;

        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdValue))
            return principal;

        if (!long.TryParse(userIdValue, out _))
            return principal;

        using var scope = _serviceProvider.CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

        var user = await userManager.FindByIdAsync(userIdValue);
        if (user is null)
            return principal;

        var roleNames = await userManager.GetRolesAsync(user);

        var permissions = new HashSet<string>();

        foreach (var roleName in roleNames)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
                continue;

            var roleClaims = await roleManager.GetClaimsAsync(role);

            foreach (var claim in roleClaims)
            {
                if (claim.Type == Permissions.Permission && !string.IsNullOrWhiteSpace(claim.Value))
                {
                    permissions.Add(claim.Value);
                }
            }
        }

        var existingPermissionClaims = identity
            .FindAll(Permissions.Permission)
            .ToList();

        foreach (var claim in existingPermissionClaims)
        {
            identity.RemoveClaim(claim);
        }

        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim(Permissions.Permission, permission));
        }

        return principal;
    }
}