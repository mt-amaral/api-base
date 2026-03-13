using Api.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Mapping.Identity;

public class RoleClaimMapping : IEntityTypeConfiguration<RoleClaim>
{
    public void Configure(EntityTypeBuilder<RoleClaim> builder)
    {
        builder.ToTable("IdentityRoleClaim");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClaimType).HasMaxLength(100);
        builder.Property(x => x.ClaimValue).HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(300);
    }
}