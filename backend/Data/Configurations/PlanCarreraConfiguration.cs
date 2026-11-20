using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class PlanCarreraConfiguration : IEntityTypeConfiguration<PlanCarrera>
{
    public void Configure(EntityTypeBuilder<PlanCarrera> builder)
    {
        builder.ToTable("PlanCarrera");
        builder.HasKey(x => x.PlanCarreraId);
        builder.Property(x => x.Estado).HasMaxLength(12).IsUnicode(false);
        builder.Property(x => x.FechaCreacion).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(x => x.ColaboradorId).IsUnique().HasFilter("[Estado] = 'Vigente'")
            .HasDatabaseName("UQ_PlanCarrera_Vigente");

        builder.HasOne(x => x.Colaborador).WithMany().HasForeignKey(x => x.ColaboradorId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.SeniorityActual).WithMany().HasForeignKey(x => x.SeniorityActualId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.SeniorityObjetivo).WithMany().HasForeignKey(x => x.SeniorityObjetivoId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.CargoObjetivo).WithMany().HasForeignKey(x => x.CargoObjetivoId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.CreadoPor).OnDelete(DeleteBehavior.NoAction);
    }
}
