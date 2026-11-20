using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmtFabric.Api.Models.Entities;

namespace VmtFabric.Api.Data.Configurations;

public class SeniorityConfiguration : IEntityTypeConfiguration<Seniority>
{
    public void Configure(EntityTypeBuilder<Seniority> builder)
    {
        builder.ToTable("Seniority");
        builder.HasKey(x => x.SeniorityId);
        builder.Property(x => x.SeniorityId).ValueGeneratedNever();
        builder.Property(x => x.Nombre).HasMaxLength(30);
    }
}
