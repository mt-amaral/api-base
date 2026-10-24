using Api.Entities;
using Api.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Api.Context;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<
    User,
    Role,
    long,
    IdentityUserClaim<long>,
    IdentityUserRole<long>,
    IdentityUserLogin<long>,
    RoleClaim,
    IdentityUserToken<long>>(options)
{
    public DbSet<User> User => Set<User>();
    public DbSet<RefreshToken> RefreshToken => Set<RefreshToken>();
    public DbSet<UserSession> UserSession => Set<UserSession>();
    public DbSet<OutboxMessage> OutboxMessage => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessage => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}


