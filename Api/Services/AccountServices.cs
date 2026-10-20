using Api.Configurations;
using Api.Context;
using Api.Dto;
using Api.Dto.Account;
using Api.Entities.Identity;
using Api.Services.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

public class AccountServices(
    UserManager<User> userManager,
    SignInManager<User> signInMannger,
    ApplicationDbContext context,
    IHttpContextAccessor httpContextAccessor,
    IUserLoggedService userLoggedService) : IAccountServices
{




    public async Task<(Response<LoginResponseDto?>, short)> LoginAsync(LoginRequestDto request, CancellationToken ct)
    {
        try
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
                return (new Response<LoginResponseDto?>(null, "Credenciais inválidas"), 400);

            var signInResult = await signInMannger.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (signInResult.IsLockedOut)
                return (new Response<LoginResponseDto?>(null, "Usuário bloqueado por excesso de tentativas. Tente novamente mais tarde."), 423);

            if (!signInResult.Succeeded)
                return (new Response<LoginResponseDto?>(null, "Credenciais inválidas"), 400);

            await signInMannger.SignInAsync(user, new AuthenticationProperties
            {
                IsPersistent = request.RememberMe,
                AllowRefresh = true
            });

            var newRefreshToken = GenerateRefreshToken();

            var storedRefreshToken = await context.RefreshToken
                .FirstOrDefaultAsync(x => x.UserId == user.Id, ct);

            if (storedRefreshToken is null)
            {
                storedRefreshToken = new RefreshToken(HashRefreshToken(newRefreshToken), user.Id);
                await context.RefreshToken.AddAsync(storedRefreshToken, ct);
            }
            else
            {
                storedRefreshToken.Replace(HashRefreshToken(newRefreshToken));
            }

            var ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
            var userAgent = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

            var storedSession = await context.UserSession
                .FirstOrDefaultAsync(x => x.UserId == user.Id, ct);

            if (storedSession is null)
            {
                storedSession = new UserSession(user.Id, ipAddress, userAgent);
                await context.UserSession.AddAsync(storedSession, ct);
            }
            else
            {
                storedSession.Refresh(ipAddress, userAgent);
            }

            await context.SaveChangesAsync(ct);

            AppendRefreshTokenCookie(newRefreshToken);

            var response = await BuildLoginResponseAsync(user, ct);

            return (new Response<LoginResponseDto?>(response, null), 200);
        }
        catch
        {
            return (new Response<LoginResponseDto?>(null, "Erro interno ao realizar login"), 500);
        }
    }

    public async Task<(Response<string?>, short)> RefreshTokenAsync(CancellationToken ct)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext is null)
                return (new Response<string?>(null, "Contexto HTTP não encontrado"), 500);

            if (!httpContext.Request.Cookies.TryGetValue(ConfigApp.RefreshTokenCookieName, out var currentRefreshToken))
                return (new Response<string?>(null, "Refresh token não encontrado"), 401);

            var currentTokenHash = HashRefreshToken(currentRefreshToken);
            var storedToken = await context.RefreshToken
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Token == currentTokenHash, ct);

            if (storedToken is null || !storedToken.IsActive)
                return (new Response<string?>(null, "Refresh token inválido ou expirado"), 401);

            var newRefreshToken = GenerateRefreshToken();
            storedToken.Replace(HashRefreshToken(newRefreshToken));

            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();

            var storedSession = await context.UserSession
                .FirstOrDefaultAsync(x => x.UserId == storedToken.UserId, ct);

            if (storedSession is null)
            {
                storedSession = new UserSession(storedToken.UserId, ipAddress, userAgent);
                await context.UserSession.AddAsync(storedSession, ct);
            }
            else
            {
                storedSession.Refresh(ipAddress, userAgent);
            }

            await signInMannger.SignInAsync(storedToken.User, new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true
            });

            await context.SaveChangesAsync(ct);

            AppendRefreshTokenCookie(newRefreshToken);

            return (new Response<string?>(null, null), 200);
        }
        catch
        {
            return (new Response<string?>(null, "Erro ao renovar sessão"), 500);
        }
    }

    public async Task<(Response<string?>, short)> LogoutAsync(CancellationToken ct)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;

            if (httpContext != null &&
                httpContext.Request.Cookies.TryGetValue(ConfigApp.RefreshTokenCookieName, out var refreshToken))
            {
                var tokenHash = HashRefreshToken(refreshToken);
                var storedToken = await context.RefreshToken
                    .FirstOrDefaultAsync(x => x.Token == tokenHash, ct);

                if (storedToken != null && storedToken.IsActive)
                {
                    storedToken.Revoke();
                }

                if (storedToken != null)
                {
                    var storedSession = await context.UserSession
                        .FirstOrDefaultAsync(x => x.UserId == storedToken.UserId, ct);

                    if (storedSession != null && storedSession.IsActive)
                    {
                        storedSession.Revoke();
                    }
                }

                await context.SaveChangesAsync(ct);
                httpContext.Response.Cookies.Delete(ConfigApp.RefreshTokenCookieName, RefreshTokenCookieOptions());
            }

            await signInMannger.SignOutAsync();

            return (new Response<string?>(null, null), 200);
        }
        catch
        {
            return (new Response<string?>(null, "Erro ao realizar logout"), 500);
        }
    }


    public async Task<(Response<LoginResponseDto?>, short)> CheckMe(CancellationToken ct)
    {
        try
        {
            var user = await userLoggedService.GetUserLoggedAsync();

            var response = await BuildLoginResponseAsync(user, ct);

            return (new Response<LoginResponseDto?>(response, null), 200);
        }
        catch
        {
            return (new Response<LoginResponseDto?>(null, "Erro ao buscar usuário logado"), 500);
        }
    }

    private async Task<LoginResponseDto> BuildLoginResponseAsync(User user, CancellationToken ct)
    {
        var roleId = await context.Set<IdentityUserRole<long>>()
            .AsNoTracking()
            .Where(x => x.UserId == user.Id)
            .OrderBy(x => x.RoleId)
            .Select(x => (long?)x.RoleId)
            .FirstOrDefaultAsync(ct);

        var claims = await context.Set<RoleClaim>()
            .AsNoTracking()
            .Where(rc => rc.RoleId == roleId)
            .Select(rc => rc.ClaimValue!)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToListAsync(ct);

        return new LoginResponseDto(
            user.Id,
            user.UserName!,
            user.Email!,
            roleId,
            claims
        );
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }

    private static string HashRefreshToken(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

    private void AppendRefreshTokenCookie(string refreshToken)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null) return;

        var options = RefreshTokenCookieOptions();
        options.Expires = DateTimeOffset.UtcNow.AddMinutes(ConfigApp.RefreshTokenCookieTime);

        httpContext.Response.Cookies.Append(ConfigApp.RefreshTokenCookieName, refreshToken, options);
    }

    private static CookieOptions RefreshTokenCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = ConfigApp.CookieSameSite
    };

}