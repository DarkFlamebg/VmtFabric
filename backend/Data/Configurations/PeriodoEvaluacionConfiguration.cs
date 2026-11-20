using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class PeriodoEvaluacionConfiguration : IEntityTypeConfiguration<PeriodoEvaluacion>
{
    public void Configure(EntityTypeBuilder<PeriodoEvaluacion> builder)
    {
        builder.ToTable("PeriodoEvaluacion");
        builder.HasKey(x => x.PeriodoEvaluacionId);
        builder.Property(x => x.Nombre).HasMaxLength(60);
        builder.HasIndex(x => x.Nombre).IsUnique();
    }
}
