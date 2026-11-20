using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class ModalidadTarifaConfiguration : IEntityTypeConfiguration<ModalidadTarifa>
{
    public void Configure(EntityTypeBuilder<ModalidadTarifa> builder)
    {
        builder.ToTable("ModalidadTarifa");
        builder.HasKey(x => x.ModalidadTarifaId);
        builder.Property(x => x.ModalidadTarifaId).ValueGeneratedNever();
        builder.Property(x => x.Nombre).HasMaxLength(20);
    }
}
