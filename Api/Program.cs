using Api.Configurations;
using Api.Configurations.Identity;
using Api.Configurations.Seed;
using Api.Configurations.Seed.Abstraction;
using Api.Context;
using Api.Entities.Identity;
using Api.Middleware;
using Api.Services;
using Api.Services.Abstractions;
using Api.Validators.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Services
builder.Services.AddScoped<IAccountServices, AccountServices>();
builder.Services.AddScoped<IUserLoggedService, UserLoggedService>();

// Validations
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestDtoValidator>();

// Claims Transformation
builder.Services.AddScoped<IClaimsTransformation, AppClaimsTransformation>();

// Seed (
if (builder.Environment.IsStaging())
{
    builder.Services.AddScoped<IAppSeed, RoleSeed>();
    builder.Services.AddScoped<IAppSeed, AdminUserSeed>();
    builder.Services.AddScoped<IAppSeed, RoleClaimSeed>();
}

// CORS
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

// Identity
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

// Cookie Configuration
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login";
    options.LogoutPath = "/Logout";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.None; //👉 atenção em produção
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

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Permissions.GetPermissions())
    {
        options.AddPolicy(permission.PermissionName, policy =>
            policy.RequireClaim(permission.ClaimType, permission.PermissionName));
    }
});

// Controllers with Global Authorization
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
});

// Swagger
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

// Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline
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

    // Apply migrations and seed data in Staging
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

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseMiddleware<ExceptionMiddleware>();

app.Run();