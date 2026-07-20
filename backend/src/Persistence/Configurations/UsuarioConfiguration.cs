using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuario");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Login).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SenhaHash).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => x.Login).IsUnique();
        builder.HasIndex(x => x.ServidorId).IsUnique();

        builder.HasOne(x => x.Servidor)
            .WithOne(x => x.Usuario)
            .HasForeignKey<Usuario>(x => x.ServidorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
