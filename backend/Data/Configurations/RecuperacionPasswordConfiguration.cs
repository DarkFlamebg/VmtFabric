using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class RecuperacionPasswordConfiguration : IEntityTypeConfiguration<RecuperacionPassword>
{
    public void Configure(EntityTypeBuilder<RecuperacionPassword> builder)
    {
        builder.ToTable("RecuperacionPassword");
        builder.HasKey(x => x.RecuperacionId);
        builder.Property(x => x.TokenHash).HasMaxLength(255);
        builder.Property(x => x.FechaCreacion).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasOne(x => x.Usuario).WithMany()
            .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.NoAction);
    }
}
