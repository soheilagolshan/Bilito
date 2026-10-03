using Bilito.Database;
using FluentMigrator.Runner;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


const string ConnectionStringKey = "ConnectionStrings";
const string AppSettingPath = "appsettings.json";

var options = GetSettings(args, Directory.GetCurrentDirectory());

var connectionString = options.Database;

CreateDatabase(connectionString);

var runner = CreateRunner(connectionString, options);
runner.MigrateUp();

static void CreateDatabase(string connectionString)
{
    var databaseName = GetDatabaseName(connectionString);
    var masterConnectionString = ChangeDatabaseName(
        connectionString,
        "master");
    var commandScript = $"if db_id(N'{databaseName}') is null " +
                        $"create database [{databaseName}]";

    using var connection = new SqlConnection(masterConnectionString);
    using var command = new SqlCommand(commandScript, connection);
    connection.Open();
    command.ExecuteNonQuery();
    connection.Close();
}
static string ChangeDatabaseName(
   string connectionString,
   string databaseName)
{
    var csb = new SqlConnectionStringBuilder(connectionString)
    {
        InitialCatalog = databaseName
    };
    return csb.ConnectionString;
}
static string GetDatabaseName(string connectionString)
{
    return new SqlConnectionStringBuilder(connectionString).InitialCatalog;
}
static IMigrationRunner CreateRunner(
    string connectionString,
    MigrationSettings options)
{
    var container = new ServiceCollection()
        .AddFluentMigratorCore()
        .ConfigureRunner(_ => _
            .AddSqlServer()
            .WithGlobalConnectionString(connectionString)
            .ScanIn(typeof(Program).Assembly).For.All())
        .AddSingleton(options)
        .AddLogging(_ => _.AddFluentMigratorConsole())
        .BuildServiceProvider();
    return container.GetRequiredService<IMigrationRunner>();
}

static MigrationSettings GetSettings(string[] args, string baseDir)
{
    var configurations = new ConfigurationBuilder()
        .SetBasePath(baseDir)
        .AddJsonFile(
            AppSettingPath,
            optional: true,
            reloadOnChange: false)
        .AddEnvironmentVariables()
        .AddCommandLine(args)
        .Build();

    var settings = new MigrationSettings();
    configurations.Bind(ConnectionStringKey, settings);
    return settings;
}

//var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
//{
//    Args = args,
//    ContentRootPath = AppContext.BaseDirectory
//});
//var connectionString = builder.Configuration.GetConnectionString("Database");

//if (string.IsNullOrWhiteSpace(connectionString))
//{
//    throw new InvalidOperationException(
//        "ConnectionStrings:Database is not configured. Set it in appsettings, user secrets, or ConnectionStrings__Database.");
//}

//builder.Services
//    .AddFluentMigratorCore()
//    .ConfigureRunner(runner => runner
//        .AddSqlServer()
//        .WithGlobalConnectionString(connectionString)
//        .ScanIn(typeof(Program).Assembly).For.Migrations())
//    .AddLogging(logging => logging.AddFluentMigratorConsole());

//using var host = builder.Build();
//using var scope = host.Services.CreateScope();
//var migrationRunner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

//try
//{
//    migrationRunner.MigrateUp();
//    Console.WriteLine("Database migrations completed successfully.");
//}
//catch (Exception exception)
//{
//    Console.Error.WriteLine($"Database migrations failed: {exception.Message}");
//    return 1;
//}

//return 0;
