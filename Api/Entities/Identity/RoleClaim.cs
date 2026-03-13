using Microsoft.AspNetCore.Identity;

namespace Api.Entities.Identity;

public class RoleClaim : IdentityRoleClaim<long>
{
    public string? Description { get; set; }
}