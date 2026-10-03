using Bilito.Identity.Domain.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bilito.Identity.Infrastructure.Persistence.Configurations;

public sealed class OtpChallengeConfiguration : IEntityTypeConfiguration<OtpChallenge>
{
    public void Configure(EntityTypeBuilder<OtpChallenge> builder)
    {
        builder.ToTable("OtpChallenges", "identity");
        builder.HasKey(challenge => challenge.Id);
        builder.Property(challenge => challenge.Mobile).HasMaxLength(20).IsRequired();
        builder.Property(challenge => challenge.CodeHash).HasMaxLength(128).IsRequired();
        builder.Property(challenge => challenge.Purpose).HasConversion<int>().IsRequired();
        builder.Property(challenge => challenge.CreatedAt).IsRequired();
        builder.Property(challenge => challenge.ExpiresAt).IsRequired();
        builder.Property(challenge => challenge.FailedAttempts).IsRequired();
        builder.HasIndex(challenge => new { challenge.Mobile, challenge.Purpose, challenge.CreatedAt });
    }
}
