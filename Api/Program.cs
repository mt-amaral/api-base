using System.Security.Claims;
using System.Threading.RateLimiting;
using Api.Configurations;
using Api.Configurations.Identity;
using Api.Configurations.Seed;
using Api.Configurations.Seed.Abstraction;
using Api.Context;
using Api.Dto;
using Api.Entities.Identity;
using Api.Middleware;
using Api.Services;
using Api.Services.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddScoped<IAccountServices, AccountServices>();
builder.Services.AddScoped<IUserLoggedService, UserLoggedService>();
builder.Services.AddScoped<IRoleService, RoleServices>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleClaimService, RoleClaimService>();
builder.Services.AddScoped<IClaimsTransformation, AppClaimsTransformation>();

if (builder.Environment.IsStaging())
{
    builder.Services.AddScoped<IAppSeed, RoleSeed>();
    builder.Services.AddScoped<IAppSeed, AdminUserSeed>();
    builder.Services.AddScoped<IAppSeed, RoleClaimSeed>();
}

if (!builder.Environment.IsDevelopment())
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    });
}

builder.Services.AddRateLimiter(options =>
{
    // TODO fazer mais testes
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("api", httpContext =>
    {
        string GetRealIp()
        {
            if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                var ip = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
                if (!string.IsNullOrEmpty(ip)) return ip;
            }
            return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        string key;
        bool isAuthenticated = httpContext.User.Identity?.IsAuthenticated == true;
        
        if (isAuthenticated)
        {
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            key = userId ?? GetRealIp();
        }
        else
        {
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            key = $"{GetRealIp()}:{userAgent}";
        }
        
        var permitLimit = isAuthenticated 
            ? ConfigApp.RateLimitPermitLimitAuthenticated 
            : ConfigApp.RateLimitPermitLimitAnonymous;
            
        var queueLimit = isAuthenticated 
            ? ConfigApp.RateLimitQueueLimitAuthenticated 
            : ConfigApp.RateLimitQueueLimitAnonymous;

        return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(ConfigApp.RateLimitWindowSeconds),
            SegmentsPerWindow = ConfigApp.RateLimitSegmentsPerWindow,
            QueueProcessingOrder = ConfigApp.QueueProcessingOrder,
            QueueLimit = queueLimit,
            AutoReplenishment = true
        });
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsDev", policy => policy
        .WithOrigins(ConfigApp.WebDevUrl)
        .WithOrigins(ConfigApp.WebProdUrl)
        .WithOrigins(ConfigApp.WebProd2Url)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());

    options.AddPolicy("CorsProd", policy => policy
        .WithOrigins(ConfigApp.WebProdUrl)
        .WithOrigins(ConfigApp.WebProd2Url)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

builder.Services.AddIdentity<User, Role>(options =>
{
    options.Password.RequiredLength = ConfigApp.PasswordRequiredLength;
    options.Password.RequiredUniqueChars = ConfigApp.PasswordRequiredUniqueChars;
    options.SignIn.RequireConfirmedAccount = ConfigApp.RequireConfirmedAccount;
    options.User.AllowedUserNameCharacters = ConfigApp.AllowedUserNameCharacters;
    options.Lockout.MaxFailedAccessAttempts = ConfigApp.LockoutMaxFailedAccessAttempts;
    options.Lockout.DefaultLockoutTimeSpan = ConfigApp.LockoutDefaultTimeSpan;
    options.ClaimsIdentity.UserIdClaimType = ConfigApp.UserIdClaimType;
    options.ClaimsIdentity.UserNameClaimType = ConfigApp.UserNameClaimType;
    options.ClaimsIdentity.RoleClaimType = ConfigApp.RoleClaimType;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login";
    options.LogoutPath = "/Logout";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.None;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Permissions.GetPermissions())
    {
        options.AddPolicy(permission.PermissionName, policy =>
            policy.RequireClaim(permission.ClaimType, permission.PermissionName));
    }
});

builder.Services.AddControllers(options =>
{
    var policy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(policy));
})
.AddNewtonsoftJson(options =>
{
    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
})
.ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .SelectMany(x => x.Value!.Errors.Select(e =>
                string.IsNullOrWhiteSpace(e.ErrorMessage)
                    ? $"Campo inválido: {x.Key}"
                    : e.ErrorMessage))
            .ToList();

        var response = new Response<object?>(
            null,
            "Dados inválidos.",
            errors
        );

        return new BadRequestObjectResult(response);
    };
});

builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
    c.SwaggerDoc("v1", new()
    {
        Title = "Api Doc",
        Description = ""
    });
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCors("CorsDev");
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Admin API V1");
        c.InjectStylesheet("/swagger-ui/SwaggerDark.css");
    });
}

if (app.Environment.IsProduction())
{
    app.UseCors("CorsProd");
}

if (app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseCors("CorsDev");
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Admin API V1");
        c.InjectStylesheet("/swagger-ui/SwaggerDark.css");
    });

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync();

            var seeds = services.GetServices<IAppSeed>();
            foreach (var seed in seeds)
            {
                await seed.SeedAsync(services);
            }
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Erro ao aplicar migrações ou seeds no ambiente Staging.");
        }
    }
}
app.UseRouting();
app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();
app.MapControllers().RequireRateLimiting("api");
app.UseMiddleware<ExceptionMiddleware>();

app.Run();