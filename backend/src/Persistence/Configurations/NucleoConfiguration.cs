using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence.Configurations;

public class NucleoConfiguration : IEntityTypeConfiguration<Nucleo>
{
    public void Configure(EntityTypeBuilder<Nucleo> builder)
    {
        builder.ToTable("Nucleo");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nome).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Sigla).HasMaxLength(40).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => x.Nome);
        builder.HasIndex(x => x.Sigla).IsUnique();

        builder.HasOne(x => x.ChefeServidor)
            .WithMany()
            .HasForeignKey(x => x.ChefeServidorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
