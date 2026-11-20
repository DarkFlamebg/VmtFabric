using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class ColaboradorConfiguration : IEntityTypeConfiguration<Colaborador>
{
    public void Configure(EntityTypeBuilder<Colaborador> builder)
    {
        builder.ToTable("Colaborador");
        builder.HasKey(x => x.ColaboradorId);
        builder.Property(x => x.Cedula).HasMaxLength(10).IsFixedLength().IsUnicode(false);
        builder.Property(x => x.Nombres).HasMaxLength(80);
        builder.Property(x => x.Apellidos).HasMaxLength(80);
        builder.Property(x => x.Correo).HasMaxLength(150);
        builder.Property(x => x.Celular).HasMaxLength(20).IsUnicode(false);
        builder.Property(x => x.Ubicacion).HasMaxLength(100);
        builder.Property(x => x.CostoMensualRef).HasPrecision(12, 2);
        builder.Property(x => x.FechaCreacion).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(x => x.Cedula).IsUnique();
        builder.HasIndex(x => x.Correo).IsUnique();

        builder.HasOne(x => x.Estado).WithMany().HasForeignKey(x => x.EstadoColaboradorId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Cargo).WithMany().HasForeignKey(x => x.CargoId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Seniority).WithMany().HasForeignKey(x => x.SeniorityId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.CreadoPor).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.ModificadoPor).OnDelete(DeleteBehavior.NoAction);
    }
}
