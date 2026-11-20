using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class AsignacionConfiguration : IEntityTypeConfiguration<Asignacion>
{
    public void Configure(EntityTypeBuilder<Asignacion> builder)
    {
        builder.ToTable("Asignacion");
        builder.HasKey(x => x.AsignacionId);
        builder.Property(x => x.LiderCliente).HasMaxLength(120);
        builder.Property(x => x.ValorCobrar).HasPrecision(12, 2);
        builder.Property(x => x.CostoColaborador).HasPrecision(12, 2);
        builder.Property(x => x.MargenPct).HasPrecision(6, 2)
            .HasComputedColumnSql(
                "CAST(CASE WHEN ValorCobrar = 0 THEN 0 ELSE (ValorCobrar - CostoColaborador) * 100.0 / ValorCobrar END AS DECIMAL(6,2))",
                stored: true);
        builder.Property(x => x.Estado).HasMaxLength(12).IsUnicode(false);
        builder.Property(x => x.MotivoCierre).HasMaxLength(200);
        builder.Property(x => x.FuncionesInicio).HasMaxLength(1000);
        builder.Property(x => x.FuncionesActuales).HasMaxLength(1000);
        builder.Property(x => x.FechaCreacion).HasDefaultValueSql("SYSUTCDATETIME()");

        // RN-01: una sola asignación activa por colaborador
        builder.HasIndex(x => x.ColaboradorId).IsUnique().HasFilter("[Estado] = 'Activa'")
            .HasDatabaseName("UQ_Asignacion_ActivaPorColaborador");

        builder.HasOne(x => x.Colaborador).WithMany(c => c.Asignaciones)
            .HasForeignKey(x => x.ColaboradorId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Cliente).WithMany(c => c.Asignaciones)
            .HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Proyecto).WithMany().HasForeignKey(x => x.ProyectoId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Cargo).WithMany().HasForeignKey(x => x.CargoId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Seniority).WithMany().HasForeignKey(x => x.SeniorityId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.ModalidadTarifa).WithMany().HasForeignKey(x => x.ModalidadTarifaId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.CreadoPor).OnDelete(DeleteBehavior.NoAction);
    }
}
