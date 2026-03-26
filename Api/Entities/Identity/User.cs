using Microsoft.AspNetCore.Identity;

namespace Api.Entities.Identity;

public class User : IdentityUser<long>
{
    public User() { }

    public User(string userName, string email)
    {
        UserName = userName;
        Email = email;
    }

}
