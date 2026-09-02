using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence.Configurations;

public class EscalaResumidaRotacaoMembroConfiguration : IEntityTypeConfiguration<EscalaResumidaRotacaoMembro>
{
    public void Configure(EntityTypeBuilder<EscalaResumidaRotacaoMembro> builder)
    {
        builder.ToTable("EscalaResumidaRotacaoMembro");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => new { x.EscalaResumidaEquipeId, x.Posicao }).IsUnique();
        builder.HasIndex(x => x.ServidorId);

        builder.HasOne(x => x.EscalaResumidaEquipe)
            .WithMany(x => x.Rotacao)
            .HasForeignKey(x => x.EscalaResumidaEquipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Servidor)
            .WithMany()
            .HasForeignKey(x => x.ServidorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Servidor2)
            .WithMany()
            .HasForeignKey(x => x.ServidorId2)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
