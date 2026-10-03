using Bilito.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bilito.Identity.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", "identity");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Mobile).HasMaxLength(20).IsRequired();
        builder.Property(user => user.FirstName).HasMaxLength(100);
        builder.Property(user => user.LastName).HasMaxLength(100);
        builder.Property(user => user.NationalCode).HasMaxLength(10);
        builder.Property(user => user.Gender).HasMaxLength(20);
        builder.Property(user => user.Avatar).HasColumnType("nvarchar(max)");
        builder.Property(user => user.MobileVerified).IsRequired();
        builder.Property(user => user.Status).HasConversion<int>().IsRequired();
        builder.Property(user => user.CreatedAt).IsRequired();
        builder.HasIndex(user => user.Mobile).IsUnique();
    }
}
