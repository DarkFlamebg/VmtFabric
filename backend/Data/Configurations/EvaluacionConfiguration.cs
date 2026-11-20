using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class EvaluacionConfiguration : IEntityTypeConfiguration<Evaluacion>
{
    public void Configure(EntityTypeBuilder<Evaluacion> builder)
    {
        builder.ToTable("Evaluacion");
        builder.HasKey(x => x.EvaluacionId);
        builder.Property(x => x.Nombre).HasMaxLength(120);
        builder.Property(x => x.Estado).HasMaxLength(12).IsUnicode(false);
        builder.Property(x => x.PuntajeFinal).HasPrecision(5, 2);
        builder.Property(x => x.ComentarioGeneral).HasMaxLength(1000);
        builder.Property(x => x.FechaCreacion).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.Periodo).WithMany().HasForeignKey(x => x.PeriodoEvaluacionId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Colaborador).WithMany().HasForeignKey(x => x.ColaboradorId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Evaluador).WithMany().HasForeignKey(x => x.EvaluadorUsuarioId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.CreadoPor).OnDelete(DeleteBehavior.NoAction);
    }
}
