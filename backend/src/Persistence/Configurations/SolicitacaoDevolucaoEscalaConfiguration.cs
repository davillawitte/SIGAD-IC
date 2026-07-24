using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence.Configurations;

public class SolicitacaoDevolucaoEscalaConfiguration : IEntityTypeConfiguration<SolicitacaoDevolucaoEscala>
{
    public void Configure(EntityTypeBuilder<SolicitacaoDevolucaoEscala> builder)
    {
        builder.ToTable("SolicitacaoDevolucaoEscala");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Justificativa).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.RespondidoPor).HasMaxLength(100);
        builder.Property(x => x.ObservacaoResposta).HasMaxLength(2000);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => x.EscalaId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.EscalaId, x.Status });

        builder.HasOne(x => x.Escala)
            .WithMany(x => x.SolicitacoesDevolucao)
            .HasForeignKey(x => x.EscalaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SolicitanteUsuario)
            .WithMany()
            .HasForeignKey(x => x.SolicitanteUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
