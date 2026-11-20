using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class EvaluacionCriterioConfiguration : IEntityTypeConfiguration<EvaluacionCriterio>
{
    public void Configure(EntityTypeBuilder<EvaluacionCriterio> builder)
    {
        builder.ToTable("EvaluacionCriterio");
        builder.HasKey(x => x.EvaluacionCriterioId);
        builder.Property(x => x.Descripcion).HasMaxLength(200);
        builder.Property(x => x.Peso).HasPrecision(5, 2);
        builder.Property(x => x.Comentario).HasMaxLength(500);
        builder.HasIndex(x => new { x.EvaluacionId, x.Orden }).IsUnique();
        builder.HasOne(x => x.Evaluacion).WithMany(e => e.Criterios)
            .HasForeignKey(x => x.EvaluacionId).OnDelete(DeleteBehavior.Cascade);
    }
}
