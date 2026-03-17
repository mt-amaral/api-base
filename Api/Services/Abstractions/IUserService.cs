using Api.Dto;
using Api.Dto.User;

namespace Api.Services.Abstractions;

public interface IUserService
{
    Task<(PagedResponse<List<UserResponse>?>, short)> GetUsersAsync(CancellationToken ct);
}