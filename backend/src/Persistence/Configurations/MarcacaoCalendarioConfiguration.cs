using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence.Configurations;

public class MarcacaoCalendarioConfiguration : IEntityTypeConfiguration<MarcacaoCalendario>
{
    public void Configure(EntityTypeBuilder<MarcacaoCalendario> builder)
    {
        builder.ToTable("MarcacaoCalendario");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.Origem).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.Nome).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Descricao).HasMaxLength(2000);
        builder.Property(x => x.Local).HasMaxLength(200);
        builder.Property(x => x.PublicoAlvo).HasMaxLength(200);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => new { x.CalendarioAnoId, x.Data });

        builder.HasOne<CalendarioAno>()
            .WithMany()
            .HasForeignKey(x => x.CalendarioAnoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
