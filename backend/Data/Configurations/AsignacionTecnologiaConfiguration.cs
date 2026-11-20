using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class AsignacionTecnologiaConfiguration : IEntityTypeConfiguration<AsignacionTecnologia>
{
    public void Configure(EntityTypeBuilder<AsignacionTecnologia> builder)
    {
        builder.ToTable("AsignacionTecnologia");
        builder.HasKey(x => new { x.AsignacionId, x.TecnologiaId });
        builder.HasOne(x => x.Asignacion).WithMany(a => a.Tecnologias)
            .HasForeignKey(x => x.AsignacionId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Tecnologia).WithMany()
            .HasForeignKey(x => x.TecnologiaId).OnDelete(DeleteBehavior.NoAction);
    }
}
