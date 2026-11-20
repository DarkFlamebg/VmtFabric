using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class ProyectoConfiguration : IEntityTypeConfiguration<Proyecto>
{
    public void Configure(EntityTypeBuilder<Proyecto> builder)
    {
        builder.ToTable("Proyecto");
        builder.HasKey(x => x.ProyectoId);
        builder.Property(x => x.Nombre).HasMaxLength(120);
        builder.Property(x => x.Descripcion).HasMaxLength(500);
        builder.HasIndex(x => new { x.ClienteId, x.Nombre }).IsUnique();
        builder.HasOne(x => x.Cliente).WithMany(c => c.Proyectos)
            .HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.NoAction);
    }
}
