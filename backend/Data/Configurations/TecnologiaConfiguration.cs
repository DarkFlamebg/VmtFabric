using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class TecnologiaConfiguration : IEntityTypeConfiguration<Tecnologia>
{
    public void Configure(EntityTypeBuilder<Tecnologia> builder)
    {
        builder.ToTable("Tecnologia");
        builder.HasKey(x => x.TecnologiaId);
        builder.Property(x => x.Nombre).HasMaxLength(60);
        builder.Property(x => x.Categoria).HasMaxLength(40);
        builder.Property(x => x.FechaCreacion).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(x => x.Nombre).IsUnique();
    }
}
