using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence.Configurations;

public class EscalaResumidaConfiguration : IEntityTypeConfiguration<EscalaResumida>
{
    public void Configure(EntityTypeBuilder<EscalaResumida> builder)
    {
        builder.ToTable("EscalaResumida");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.Observacao).HasMaxLength(1000);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.Ignore(x => x.DataInicio);
        builder.Ignore(x => x.DataFim);

        builder.HasIndex(x => x.NucleoId);
        builder.HasIndex(x => x.SetorId);
        builder.HasIndex(x => new { x.NucleoId, x.Ano, x.Mes }).IsUnique();
        builder.HasIndex(x => new { x.SetorId, x.Ano, x.Mes }).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.EscalaId);

        builder.HasOne(x => x.Nucleo)
            .WithMany()
            .HasForeignKey(x => x.NucleoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Setor)
            .WithMany()
            .HasForeignKey(x => x.SetorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Escala)
            .WithMany()
            .HasForeignKey(x => x.EscalaId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
