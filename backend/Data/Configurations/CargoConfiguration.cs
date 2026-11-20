using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class CargoConfiguration : IEntityTypeConfiguration<Cargo>
{
    public void Configure(EntityTypeBuilder<Cargo> builder)
    {
        builder.ToTable("Cargo");
        builder.HasKey(x => x.CargoId);
        builder.Property(x => x.Nombre).HasMaxLength(80);
        builder.HasIndex(x => x.Nombre).IsUnique();
    }
}
