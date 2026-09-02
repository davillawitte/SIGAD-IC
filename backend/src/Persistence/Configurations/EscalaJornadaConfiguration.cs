using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence.Configurations;

public class EscalaJornadaConfiguration : IEntityTypeConfiguration<EscalaJornada>
{
    public void Configure(EntityTypeBuilder<EscalaJornada> builder)
    {
        builder.ToTable("EscalaJornada");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TipoJornada).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.RecorrenciaTipo).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.TipoOcorrenciaCodigo).HasMaxLength(10).IsRequired();
        builder.Property(x => x.TipoOcorrenciaFolgaCodigo).HasMaxLength(10);
        builder.Property(x => x.SequenciaCiclo).HasMaxLength(200);
        builder.Property(x => x.DiasSemana).HasMaxLength(40);
        builder.Property(x => x.Horas).HasPrecision(5, 2);
        builder.Property(x => x.Observacao).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => x.EscalaServidorId);
        builder.HasIndex(x => x.PadraoEscalaId);

        builder.HasOne(x => x.EscalaServidor)
            .WithMany(x => x.Jornadas)
            .HasForeignKey(x => x.EscalaServidorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PadraoEscala)
            .WithMany()
            .HasForeignKey(x => x.PadraoEscalaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.TipoOcorrencia)
            .WithMany()
            .HasForeignKey(x => x.TipoOcorrenciaCodigo)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
