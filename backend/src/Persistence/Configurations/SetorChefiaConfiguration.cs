using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence.Configurations;

public class SetorChefiaConfiguration : IEntityTypeConfiguration<SetorChefia>
{
    public void Configure(EntityTypeBuilder<SetorChefia> builder)
    {
        builder.ToTable("SetorChefia");
        builder.HasKey(x => new { x.SetorId, x.TipoChefia });

        builder.Property(x => x.TipoChefia)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.HasIndex(x => x.ServidorId);

        builder.HasOne(x => x.Setor)
            .WithMany(x => x.Chefias)
            .HasForeignKey(x => x.SetorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Servidor)
            .WithMany()
            .HasForeignKey(x => x.ServidorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
