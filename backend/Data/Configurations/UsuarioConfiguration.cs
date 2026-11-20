using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuario");
        builder.HasKey(x => x.UsuarioId);
        builder.Property(x => x.Nombre).HasMaxLength(120);
        builder.Property(x => x.Correo).HasMaxLength(150);
        builder.Property(x => x.PasswordHash).HasMaxLength(255);
        builder.Property(x => x.FechaCreacion).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(x => x.Correo).IsUnique();

        builder.HasOne(x => x.Rol).WithMany(r => r.Usuarios)
            .HasForeignKey(x => x.RolId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Creador).WithMany()
            .HasForeignKey(x => x.CreadoPor).OnDelete(DeleteBehavior.NoAction);
    }
}
