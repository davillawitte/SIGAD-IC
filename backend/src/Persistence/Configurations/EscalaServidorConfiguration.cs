using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence.Configurations;

public class EscalaServidorConfiguration : IEntityTypeConfiguration<EscalaServidor>
{
    public void Configure(EntityTypeBuilder<EscalaServidor> builder)
    {
        builder.ToTable("EscalaServidor");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ServidorNome).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Matricula).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CargoNome).HasMaxLength(150).IsRequired();
        builder.Property(x => x.CargoCodigo).HasMaxLength(80).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => new { x.EscalaId, x.ServidorId }).IsUnique();
        builder.HasIndex(x => x.ServidorId);

        builder.HasOne(x => x.Escala)
            .WithMany(x => x.Servidores)
            .HasForeignKey(x => x.EscalaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Servidor)
            .WithMany()
            .HasForeignKey(x => x.ServidorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Cargo)
            .WithMany()
            .HasForeignKey(x => x.CargoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
