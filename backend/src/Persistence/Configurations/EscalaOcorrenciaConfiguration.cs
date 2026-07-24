using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence.Configurations;

public class EscalaOcorrenciaConfiguration : IEntityTypeConfiguration<EscalaOcorrencia>
{
    public void Configure(EntityTypeBuilder<EscalaOcorrencia> builder)
    {
        builder.ToTable("EscalaOcorrencia");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TipoOcorrenciaCodigo).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Origem).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Horas).HasPrecision(5, 2);
        builder.Property(x => x.Observacao).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => new { x.EscalaServidorId, x.Data }).IsUnique();
        builder.HasIndex(x => x.Data);

        builder.HasOne(x => x.EscalaServidor)
            .WithMany(x => x.Ocorrencias)
            .HasForeignKey(x => x.EscalaServidorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.TipoOcorrencia)
            .WithMany()
            .HasForeignKey(x => x.TipoOcorrenciaCodigo)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.EscalaJornada)
            .WithMany(x => x.Ocorrencias)
            .HasForeignKey(x => x.EscalaJornadaId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
