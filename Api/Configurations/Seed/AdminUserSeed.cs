using Api.Configurations.Identity;
using Api.Configurations.Seed.Abstraction;
using Api.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace Api.Configurations.Seed;

public sealed class AdminUserSeed : IAppSeed
{
    public async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

        const string email = "admin@teste.com";
        const string password = "Lp59bh5Qa24hfI6SsTepaoBrs0ZBKqyz*";

        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new User("Admin", email);

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                throw new Exception($"Erro ao criar usuário admin: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, Permissions.Admin))
        {
            var addToRoleResult = await userManager.AddToRoleAsync(user, Permissions.Admin);
            if (!addToRoleResult.Succeeded)
            {
                var errors = string.Join("; ", addToRoleResult.Errors.Select(e => e.Description));
                throw new Exception($"Erro ao vincular admin à role '{Permissions.Admin}': {errors}");
            }
        }
    }
}

