using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshToken");
        builder.HasKey(x => x.RefreshTokenId);
        builder.Property(x => x.TokenHash).HasMaxLength(255);
        builder.Property(x => x.FechaCreacion).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasOne(x => x.Usuario).WithMany()
            .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.NoAction);
    }
}
