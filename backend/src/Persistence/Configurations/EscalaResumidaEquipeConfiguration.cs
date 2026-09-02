using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence.Configurations;

public class EscalaResumidaEquipeConfiguration : IEntityTypeConfiguration<EscalaResumidaEquipe>
{
    public void Configure(EntityTypeBuilder<EscalaResumidaEquipe> builder)
    {
        builder.ToTable("EscalaResumidaEquipe");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nome).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => x.EscalaResumidaSetorId);

        builder.HasOne(x => x.EscalaResumidaSetor)
            .WithMany(x => x.Equipes)
            .HasForeignKey(x => x.EscalaResumidaSetorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
