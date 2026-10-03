using FluentMigrator;

namespace Bilito.Database.Migrations.Identity;

[Migration(20260930123000)]
public sealed class CreateRefreshTokensTable : Migration
{
    public override void Up()
    {
        Create.Table("RefreshTokens").InSchema("identity")
            .WithColumn("Id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("TokenHash").AsString(64).NotNullable()
            .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
            .WithColumn("ExpiresAt").AsDateTimeOffset().NotNullable()
            .WithColumn("RevokedAt").AsDateTimeOffset().Nullable()
            .WithColumn("ReplacedByTokenId").AsGuid().Nullable();

        Create.UniqueConstraint("UX_RefreshTokens_TokenHash")
            .OnTable("RefreshTokens").WithSchema("identity")
            .Column("TokenHash");

        Create.Index("IX_RefreshTokens_UserId_ExpiresAt")
            .OnTable("RefreshTokens").InSchema("identity")
            .OnColumn("UserId").Ascending()
            .OnColumn("ExpiresAt").Ascending();

        Create.ForeignKey("FK_RefreshTokens_Users")
            .FromTable("RefreshTokens").InSchema("identity").ForeignColumn("UserId")
            .ToTable("Users").InSchema("identity").PrimaryColumn("Id");

        Create.ForeignKey("FK_RefreshTokens_ReplacedBy")
            .FromTable("RefreshTokens").InSchema("identity").ForeignColumn("ReplacedByTokenId")
            .ToTable("RefreshTokens").InSchema("identity").PrimaryColumn("Id");
    }

    public override void Down()
    {
        Delete.Table("RefreshTokens").InSchema("identity");
    }
}
