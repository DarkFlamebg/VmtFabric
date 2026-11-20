using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Cliente");
        builder.HasKey(x => x.ClienteId);
        builder.Property(x => x.Nombre).HasMaxLength(120);
        builder.Property(x => x.Ruc).HasMaxLength(13).IsFixedLength().IsUnicode(false);
        builder.Property(x => x.Industria).HasMaxLength(60);
        builder.Property(x => x.ContactoNombre).HasMaxLength(120);
        builder.Property(x => x.ContactoCorreo).HasMaxLength(150);
        builder.Property(x => x.ContactoTelefono).HasMaxLength(20).IsUnicode(false);
        builder.Property(x => x.FechaCreacion).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(x => x.Nombre).IsUnique();
        builder.HasIndex(x => x.Ruc).IsUnique().HasFilter("[Ruc] IS NOT NULL");
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.CreadoPor).OnDelete(DeleteBehavior.NoAction);
    }
}
