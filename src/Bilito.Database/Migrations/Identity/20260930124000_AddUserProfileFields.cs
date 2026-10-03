using FluentMigrator;

namespace Bilito.Database.Migrations.Identity;

[Migration(20260930124000)]
public sealed class AddUserProfileFields : Migration
{
    public override void Up()
    {
        Alter.Table("Users").InSchema("identity")
            .AddColumn("FirstName").AsString(100).Nullable()
            .AddColumn("LastName").AsString(100).Nullable()
            .AddColumn("NationalCode").AsString(10).Nullable()
            .AddColumn("Gender").AsString(20).Nullable()
            .AddColumn("Avatar").AsCustom("nvarchar(max)").Nullable();
    }

    public override void Down()
    {
        Delete.Column("Avatar").FromTable("Users").InSchema("identity");
        Delete.Column("Gender").FromTable("Users").InSchema("identity");
        Delete.Column("NationalCode").FromTable("Users").InSchema("identity");
        Delete.Column("LastName").FromTable("Users").InSchema("identity");
        Delete.Column("FirstName").FromTable("Users").InSchema("identity");
    }
}
