using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence.Configurations;

public class SetupTokenConfiguration : IEntityTypeConfiguration<SetupToken>
{
    public void Configure(EntityTypeBuilder<SetupToken> builder)
    {
        builder.ToTable("SetupToken");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(x => x.TokenHash);
    }
}
