using FluentMigrator;

namespace Bilito.Database.Migrations.Identity;

[Migration(20260930122000)]
public sealed class CreateOtpChallengesTable : Migration
{
    public override void Up()
    {
        Create.Table("OtpChallenges").InSchema("identity")
            .WithColumn("Id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("Mobile").AsString(20).NotNullable()
            .WithColumn("CodeHash").AsString(128).NotNullable()
            .WithColumn("Purpose").AsInt32().NotNullable()
            .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
            .WithColumn("ExpiresAt").AsDateTimeOffset().NotNullable()
            .WithColumn("ConsumedAt").AsDateTimeOffset().Nullable()
            .WithColumn("FailedAttempts").AsInt32().NotNullable().WithDefaultValue(0);

        Create.Index("IX_OtpChallenges_Mobile_Purpose_CreatedAt")
            .OnTable("OtpChallenges").InSchema("identity")
            .OnColumn("Mobile").Ascending()
            .OnColumn("Purpose").Ascending()
            .OnColumn("CreatedAt").Descending();
    }

    public override void Down()
    {
        Delete.Table("OtpChallenges").InSchema("identity");
    }
}
