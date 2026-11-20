using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class CriterioPlantillaConfiguration : IEntityTypeConfiguration<CriterioPlantilla>
{
    public void Configure(EntityTypeBuilder<CriterioPlantilla> builder)
    {
        builder.ToTable("CriterioPlantilla");
        builder.HasKey(x => x.CriterioPlantillaId);
        builder.Property(x => x.Descripcion).HasMaxLength(200);
        builder.HasIndex(x => x.Descripcion).IsUnique();
    }
}
