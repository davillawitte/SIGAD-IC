using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence.Configurations;

public class TipoOcorrenciaConfiguration : IEntityTypeConfiguration<TipoOcorrencia>
{
    public void Configure(EntityTypeBuilder<TipoOcorrencia> builder)
    {
        builder.ToTable("TipoOcorrencia");
        builder.HasKey(x => x.Codigo);

        builder.Property(x => x.Codigo).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Nome).HasMaxLength(120).IsRequired();
        builder.Property(x => x.HorasPadrao).HasPrecision(5, 2);
        builder.Property(x => x.Categoria).HasConversion<string>().HasMaxLength(40).IsRequired();
    }
}
