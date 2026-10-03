using FluentMigrator;

namespace Bilito.Database.Migrations.Identity;

[Migration(20260930121000)]
public sealed class CreateUsersTable : Migration
{
    public override void Up()
    {
        Create.Table("Users").InSchema("identity")
            .WithColumn("Id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("Mobile").AsString(20).NotNullable()
            .WithColumn("MobileVerified").AsBoolean().NotNullable()
            .WithColumn("Status").AsInt32().NotNullable()
            .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
            .WithColumn("LastLoginAt").AsDateTimeOffset().Nullable();

        Create.UniqueConstraint("UX_Users_Mobile")
            .OnTable("Users").WithSchema("identity")
            .Column("Mobile");
    }

    public override void Down()
    {
        Delete.Table("Users").InSchema("identity");
    }
}
