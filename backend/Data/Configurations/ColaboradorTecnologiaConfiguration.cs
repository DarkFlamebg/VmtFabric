using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class ColaboradorTecnologiaConfiguration : IEntityTypeConfiguration<ColaboradorTecnologia>
{
    public void Configure(EntityTypeBuilder<ColaboradorTecnologia> builder)
    {
        builder.ToTable("ColaboradorTecnologia");
        builder.HasKey(x => new { x.ColaboradorId, x.TecnologiaId });
        builder.Property(x => x.AniosExperiencia).HasPrecision(4, 1);
        builder.HasOne(x => x.Colaborador).WithMany(c => c.Tecnologias)
            .HasForeignKey(x => x.ColaboradorId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Tecnologia).WithMany(t => t.Colaboradores)
            .HasForeignKey(x => x.TecnologiaId).OnDelete(DeleteBehavior.NoAction);
    }
}
