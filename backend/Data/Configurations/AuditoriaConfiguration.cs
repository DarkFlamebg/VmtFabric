using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class AuditoriaConfiguration : IEntityTypeConfiguration<Auditoria>
{
    public void Configure(EntityTypeBuilder<Auditoria> builder)
    {
        builder.ToTable("Auditoria");
        builder.HasKey(x => x.AuditoriaId);
        builder.Property(x => x.Accion).HasMaxLength(20).IsUnicode(false);
        builder.Property(x => x.Entidad).HasMaxLength(50).IsUnicode(false);
        builder.Property(x => x.Fecha).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.NoAction);
    }
}
