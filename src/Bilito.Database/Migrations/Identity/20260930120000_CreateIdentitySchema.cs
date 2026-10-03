using FluentMigrator;

namespace Bilito.Database.Migrations.Identity;

[Migration(20260930120000)]
public sealed class CreateIdentitySchema : Migration
{
    public override void Up()
    {
        Execute.Sql("""
            IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'identity')
            BEGIN
                EXEC('CREATE SCHEMA [identity]')
            END
            """);
    }

    public override void Down()
    {
        // The schema may contain tables owned by later migrations or module work.
        // It is intentionally not dropped automatically during rollback.
    }
}
