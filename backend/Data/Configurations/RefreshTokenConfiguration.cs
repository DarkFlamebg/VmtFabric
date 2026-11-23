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
        builder.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("UQ_RefreshToken_TokenHash");
        builder.HasIndex(x => new { x.UsuarioId, x.Revocado }).HasDatabaseName("IX_RefreshToken_Usuario");
        builder.Property(x => x.FechaCreacion).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasOne(x => x.Usuario).WithMany()
            .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.NoAction);
    }
}
