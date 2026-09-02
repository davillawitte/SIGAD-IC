using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence.Configurations;

public class EscalaResumidaDiaConfiguration : IEntityTypeConfiguration<EscalaResumidaDia>
{
    public void Configure(EntityTypeBuilder<EscalaResumidaDia> builder)
    {
        builder.ToTable("EscalaResumidaDia");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ServidorNomeSnapshot).HasMaxLength(200);
        builder.Property(x => x.ServidorNomeSnapshot2).HasMaxLength(200);
        builder.Property(x => x.TextoLivre).HasMaxLength(200);
        builder.Property(x => x.Origem).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.Ignore(x => x.Rotulo);

        builder.HasIndex(x => new { x.EscalaResumidaEquipeId, x.Data }).IsUnique();
        builder.HasIndex(x => x.Data);

        builder.HasOne(x => x.EscalaResumidaEquipe)
            .WithMany(x => x.Dias)
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

        builder.HasOne(x => x.RotacaoMembro)
            .WithMany()
            .HasForeignKey(x => x.RotacaoMembroId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
