namespace Api.Dto.Account;

public record LoginRequestDto(string Email, string Password, bool RememberMe);