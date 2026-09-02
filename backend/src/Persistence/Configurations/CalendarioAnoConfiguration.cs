using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence.Configurations;

public class CalendarioAnoConfiguration : IEntityTypeConfiguration<CalendarioAno>
{
    public void Configure(EntityTypeBuilder<CalendarioAno> builder)
    {
        builder.ToTable("CalendarioAno");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.PublicadoPor).HasMaxLength(100);
        builder.Property(x => x.Observacao).HasMaxLength(1000);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => x.Ano).IsUnique();
    }
}
