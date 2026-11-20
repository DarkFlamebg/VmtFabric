using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class IngresoConfiguration : IEntityTypeConfiguration<Ingreso>
{
    public void Configure(EntityTypeBuilder<Ingreso> builder)
    {
        builder.ToTable("Ingreso");
        builder.HasKey(x => x.IngresoId);
        builder.Property(x => x.Monto).HasPrecision(12, 2);
        builder.Property(x => x.Costo).HasPrecision(12, 2);
        builder.Property(x => x.Estado).HasMaxLength(12).IsUnicode(false);
        builder.Property(x => x.NumeroFactura).HasMaxLength(30).IsUnicode(false);
        builder.Property(x => x.Observacion).HasMaxLength(300);
        builder.Property(x => x.FechaRegistro).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(x => new { x.AsignacionId, x.Periodo }).IsUnique();
        builder.HasOne(x => x.Asignacion).WithMany(a => a.Ingresos)
            .HasForeignKey(x => x.AsignacionId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.RegistradoPor).OnDelete(DeleteBehavior.NoAction);
    }
}
