using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class DocumentoConfiguration : IEntityTypeConfiguration<Documento>
{
    public void Configure(EntityTypeBuilder<Documento> builder)
    {
        builder.ToTable("Documento");
        builder.HasKey(x => x.DocumentoId);
        builder.Property(x => x.NombreArchivo).HasMaxLength(200);
        builder.Property(x => x.RutaAlmacen).HasMaxLength(400);
        builder.Property(x => x.MimeType).HasMaxLength(100).IsUnicode(false);
        builder.Property(x => x.FechaCarga).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasOne(x => x.Colaborador).WithMany(c => c.Documentos)
            .HasForeignKey(x => x.ColaboradorId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Cliente).WithMany(c => c.Documentos)
            .HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.TipoDocumento).WithMany()
            .HasForeignKey(x => x.TipoDocumentoId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.SubidoPor).OnDelete(DeleteBehavior.NoAction);
    }
}
