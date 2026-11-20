using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class AvisoConfiguration : IEntityTypeConfiguration<Aviso>
{
    public void Configure(EntityTypeBuilder<Aviso> builder)
    {
        builder.ToTable("Aviso");
        builder.HasKey(x => x.AvisoId);
        builder.Property(x => x.Tipo).HasMaxLength(30).IsUnicode(false);
        builder.Property(x => x.Titulo).HasMaxLength(150);
        builder.Property(x => x.Mensaje).HasMaxLength(500);
        builder.Property(x => x.EntidadTipo).HasMaxLength(30).IsUnicode(false);
        builder.Property(x => x.FechaCreacion).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.NoAction);
    }
}
