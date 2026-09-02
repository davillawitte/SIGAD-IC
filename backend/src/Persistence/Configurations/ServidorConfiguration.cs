using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence.Configurations;

public class ServidorConfiguration : IEntityTypeConfiguration<Servidor>
{
    public void Configure(EntityTypeBuilder<Servidor> builder)
    {
        builder.ToTable("Servidor", t => t.HasCheckConstraint(
            "CK_Servidor_Lotacao_SetorOuNucleo",
            "(\"SetorId\" IS NOT NULL AND \"NucleoId\" IS NULL) OR (\"SetorId\" IS NULL AND \"NucleoId\" IS NOT NULL)"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nome).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Matricula).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Cpf).HasMaxLength(11).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Telefone).HasMaxLength(30);
        builder.Property(x => x.DataNascimento).HasColumnType("date").IsRequired();
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.Ignore(x => x.EstaAtivo);
        builder.Ignore(x => x.LotadoDiretoNoNucleo);

        builder.HasIndex(x => x.Matricula).IsUnique();
        builder.HasIndex(x => x.Cpf).IsUnique();
        builder.HasIndex(x => x.Email);
        builder.HasIndex(x => x.CargoId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.NucleoId);

        builder.HasOne(x => x.Cargo)
            .WithMany(x => x.Servidores)
            .HasForeignKey(x => x.CargoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Setor)
            .WithMany(x => x.Servidores)
            .HasForeignKey(x => x.SetorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Nucleo)
            .WithMany()
            .HasForeignKey(x => x.NucleoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
