

using Api.Entities.Identity;

namespace Api.Services.Abstractions
{
    public interface IUserLoggedService
    {
        Task<User> GetUserLoggedAsync();
    }
}