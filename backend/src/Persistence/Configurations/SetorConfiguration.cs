using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence.Configurations;

public class SetorConfiguration : IEntityTypeConfiguration<Setor>
{
    public void Configure(EntityTypeBuilder<Setor> builder)
    {
        builder.ToTable("Setor");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nome).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Sigla).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Resumo).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => x.Sigla).IsUnique();
        builder.HasIndex(x => x.Nome);
        builder.HasIndex(x => x.NucleoId);

        builder.HasOne(x => x.Nucleo)
            .WithMany(x => x.Setores)
            .HasForeignKey(x => x.NucleoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
