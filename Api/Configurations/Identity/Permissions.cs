namespace Api.Configurations.Identity;


public static class Permissions
{
    //Roles
    public const string Admin = "Admin";
    
    //ClaimTypes
    public const string Permission = "permission";
    
    //ClaimList
    public const string UsersRegister = "users.register";
    public const string UsersDelete = "users.delete";
    public static IEnumerable<PermissionDefinition> GetPermissions()
    {
        return new List<PermissionDefinition>
        {
            new ( UsersRegister, Permission, "Permite registrar novos usuários"),
            new (UsersDelete, Permission, "Permite deletar usuários"),
        };
    }
}
public record PermissionDefinition(
    string PermissionName,
    string ClaimType,
    string Description);
