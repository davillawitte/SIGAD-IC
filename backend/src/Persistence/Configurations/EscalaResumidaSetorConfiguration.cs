using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence.Configurations;

public class EscalaResumidaSetorConfiguration : IEntityTypeConfiguration<EscalaResumidaSetor>
{
    public void Configure(EntityTypeBuilder<EscalaResumidaSetor> builder)
    {
        builder.ToTable("EscalaResumidaSetor");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SetorNomeSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SetorSiglaSnapshot).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => new { x.EscalaResumidaId, x.SetorId }).IsUnique();
        builder.HasIndex(x => x.SetorId);

        builder.HasOne(x => x.EscalaResumida)
            .WithMany(x => x.Setores)
            .HasForeignKey(x => x.EscalaResumidaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Setor)
            .WithMany()
            .HasForeignKey(x => x.SetorId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
