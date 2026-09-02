using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence.Configurations;

public class PadraoEscalaConfiguration : IEntityTypeConfiguration<PadraoEscala>
{
    public void Configure(EntityTypeBuilder<PadraoEscala> builder)
    {
        builder.ToTable("PadraoEscala");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Codigo).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Nome).HasMaxLength(150).IsRequired();
        builder.Property(x => x.TipoFuncionamento).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.TipoJornada).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.RecorrenciaTipo).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.DiasSemana).HasMaxLength(40);
        builder.Property(x => x.TipoOcorrenciaTrabalho).HasMaxLength(10).IsRequired();
        builder.Property(x => x.TipoOcorrenciaFolga).HasMaxLength(10).IsRequired();
        builder.Property(x => x.SequenciaCiclo).HasMaxLength(200);
        builder.Property(x => x.HorasPadrao).HasPrecision(5, 2);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => x.Codigo).IsUnique();
        builder.HasIndex(x => x.TipoFuncionamento);
        builder.HasIndex(x => x.SetorId);

        builder.HasOne(x => x.Setor)
            .WithMany()
            .HasForeignKey(x => x.SetorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
