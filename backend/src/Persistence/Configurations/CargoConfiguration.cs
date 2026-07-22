using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence.Configurations;

public class CargoConfiguration : IEntityTypeConfiguration<Cargo>
{
    public void Configure(EntityTypeBuilder<Cargo> builder)
    {
        builder.ToTable("Cargo");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nome).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Codigo).HasMaxLength(80).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => x.Codigo).IsUnique();
        builder.HasIndex(x => x.Nome);
    }
}
