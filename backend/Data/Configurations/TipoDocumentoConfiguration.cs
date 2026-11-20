using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class TipoDocumentoConfiguration : IEntityTypeConfiguration<TipoDocumento>
{
    public void Configure(EntityTypeBuilder<TipoDocumento> builder)
    {
        builder.ToTable("TipoDocumento");
        builder.HasKey(x => x.TipoDocumentoId);
        builder.Property(x => x.TipoDocumentoId).ValueGeneratedNever();
        builder.Property(x => x.Nombre).HasMaxLength(40);
    }
}
