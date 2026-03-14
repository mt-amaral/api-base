using System.Security.Claims;

namespace Api.Configurations;

public static class ConfigApp
{

    // URL usada em ambiente de desenvolvimento
    public static string WebDevUrl = "http://localhost:5173";

    // URL principal de produção
    public static string WebProdUrl = "http://147.93.5.237:10003";

    // Caso precise com barra no final
    public static string WebProd2Url = "http://147.93.5.237:10003/";

    // Tamanho mínimo da senha
    public static int PasswordRequiredLength = 8;

    // Quantidade mínima de caracteres únicos na senha
    public static int PasswordRequiredUniqueChars = 1;


    // Se precisa confirmar conta antes de logar (email, etc)
    public static bool RequireConfirmedAccount = false;


    // Quais caracteres são permitidos no UserName
    public static string AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

    // Quantas tentativas erradas antes de bloquear
    public static int LockoutMaxFailedAccessAttempts = 5;

    // Tempo que o usuário fica bloqueado
    public static TimeSpan LockoutDefaultTimeSpan = TimeSpan.FromMinutes(20);


    // Claim usada como identificador do usuário
    public static string UserIdClaimType = ClaimTypes.NameIdentifier;

    // Claim usada como nome do usuário
    public static string UserNameClaimType = ClaimTypes.Name;

    // Claim usada para roles/permissões
    public static string RoleClaimType = ClaimTypes.Role;

    // Chave identificadora do cookie de refresh token
    public static string RefreshTokenCookieName = "refreshToken";
    
    // Tempo de duração de token (minutos)
    public static int TokenCookieTime = 15;
    
    // Tempo de duração de token (minutos)
    public static int RefreshTokenCookieTime = 300;
}