using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence.Configurations;

public class EscalaConfiguration : IEntityTypeConfiguration<Escala>
{
    public void Configure(EntityTypeBuilder<Escala> builder)
    {
        builder.ToTable("Escala", t => t.HasCheckConstraint(
            "CK_Escala_Lotacao_SetorOuNucleo",
            "(\"SetorId\" IS NOT NULL AND \"NucleoId\" IS NULL) OR (\"SetorId\" IS NULL AND \"NucleoId\" IS NOT NULL)"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.TipoFuncionamento).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.Observacao).HasMaxLength(1000);
        builder.Property(x => x.PublicadaPor).HasMaxLength(100);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.Ignore(x => x.DataInicio);
        builder.Ignore(x => x.DataFim);

        // Só escala PUBLICADA ocupa o mês: rascunhos/finalizadas do mesmo setor/núcleo+mês podem
        // coexistir (versões montadas antes de publicar a que vale) — mesma regra de
        // `EscalaService.CreateAsync`/`CopiarAsync`/`PublicarAsync`.
        builder.HasIndex(x => x.SetorId);
        builder.HasIndex(x => new { x.SetorId, x.Ano, x.Mes })
            .IsUnique()
            .HasFilter("\"Status\" = 'Publicada'");
        builder.HasIndex(x => x.NucleoId);
        builder.HasIndex(x => new { x.NucleoId, x.Ano, x.Mes })
            .IsUnique()
            .HasFilter("\"Status\" = 'Publicada'");
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Setor)
            .WithMany()
            .HasForeignKey(x => x.SetorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Nucleo)
            .WithMany()
            .HasForeignKey(x => x.NucleoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
