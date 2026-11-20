using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class EstadoColaboradorConfiguration : IEntityTypeConfiguration<EstadoColaborador>
{
    public void Configure(EntityTypeBuilder<EstadoColaborador> builder)
    {
        builder.ToTable("EstadoColaborador");
        builder.HasKey(x => x.EstadoColaboradorId);
        builder.Property(x => x.EstadoColaboradorId).ValueGeneratedNever();
        builder.Property(x => x.Nombre).HasMaxLength(30);
    }
}
