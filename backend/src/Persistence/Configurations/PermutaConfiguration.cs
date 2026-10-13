using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence.Configurations;

public class PermutaConfiguration : IEntityTypeConfiguration<Permuta>
{
    public void Configure(EntityTypeBuilder<Permuta> builder)
    {
        builder.ToTable("Permuta");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Observacao).HasMaxLength(1000);
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => x.SolicitanteServidorId);
        builder.HasIndex(x => x.PermutanteServidorId);
        builder.HasIndex(x => x.DataIda);
        builder.HasIndex(x => x.DataVolta);

        builder.HasOne(x => x.Solicitante)
            .WithMany()
            .HasForeignKey(x => x.SolicitanteServidorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Permutante)
            .WithMany()
            .HasForeignKey(x => x.PermutanteServidorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
