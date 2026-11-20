using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class PlanObjetivoConfiguration : IEntityTypeConfiguration<PlanObjetivo>
{
    public void Configure(EntityTypeBuilder<PlanObjetivo> builder)
    {
        builder.ToTable("PlanObjetivo");
        builder.HasKey(x => x.PlanObjetivoId);
        builder.Property(x => x.Descripcion).HasMaxLength(200);
        builder.Property(x => x.Tipo).HasMaxLength(15).IsUnicode(false);
        builder.HasOne(x => x.PlanCarrera).WithMany(p => p.Objetivos)
            .HasForeignKey(x => x.PlanCarreraId).OnDelete(DeleteBehavior.Cascade);
    }
}
