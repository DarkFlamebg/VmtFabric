using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class RolConfiguration : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        builder.ToTable("Rol");
        builder.HasKey(x => x.RolId);
        builder.Property(x => x.RolId).ValueGeneratedNever();
        builder.Property(x => x.Nombre).HasMaxLength(30);
        builder.Property(x => x.Descripcion).HasMaxLength(200);
        builder.HasIndex(x => x.Nombre).IsUnique();
    }
}
