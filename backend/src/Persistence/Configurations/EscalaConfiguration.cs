using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence.Configurations;

public class EscalaConfiguration : IEntityTypeConfiguration<Escala>
{
    public void Configure(EntityTypeBuilder<Escala> builder)
    {
        builder.ToTable("Escala");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.TipoFuncionamento).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.Observacao).HasMaxLength(1000);
        builder.Property(x => x.PublicadaPor).HasMaxLength(100);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.Ignore(x => x.DataInicio);
        builder.Ignore(x => x.DataFim);

        builder.HasIndex(x => x.SetorId);
        builder.HasIndex(x => new { x.SetorId, x.Ano, x.Mes }).IsUnique();
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Setor)
            .WithMany()
            .HasForeignKey(x => x.SetorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
