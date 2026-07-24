using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence.Configurations;

public class AfastamentoConfiguration : IEntityTypeConfiguration<Afastamento>
{
    public void Configure(EntityTypeBuilder<Afastamento> builder)
    {
        builder.ToTable("Afastamento");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TipoOcorrenciaCodigo).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Observacao).HasMaxLength(1000);
        builder.Property(x => x.Sei).HasMaxLength(100);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => x.ServidorId);
        builder.HasIndex(x => new { x.DataInicio, x.DataFim });
        builder.HasIndex(x => x.TipoOcorrenciaCodigo);

        builder.HasOne(x => x.Servidor)
            .WithMany()
            .HasForeignKey(x => x.ServidorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
