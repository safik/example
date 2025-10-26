using System.Reflection;
using FluentMigrator;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.Processors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Orchestrator.Infrastructure.RisingWave.FluentMigrator;

public interface ISchemaMigrator
{
    void MigrateSchema(string schema);
}

public class SchemaMigrator : ISchemaMigrator
{
    private readonly IConfiguration _configuration;
    
    public SchemaMigrator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void MigrateSchema(string schema)
    {
        var serviceProvider = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb =>
                {
                    rb
                        .AddPostgres()
                        .WithGlobalConnectionString(_configuration.GetConnectionString("Default"))
                        .WithVersionTable(new SchemaAwareVersionTableMetaData(schema)) // Use specific schema
                        .ScanIn(Assembly.GetExecutingAssembly())
                        .For.Migrations();
                }
            )
            .AddScoped<IVersionLoader>(sp =>
                {
                    var accessor = sp.GetRequiredService<IVersionTableMetaDataAccessor>();
                    var processorAccessor = sp.GetRequiredService<IProcessorAccessor>();
                    var migrationRunnerConventions = sp.GetRequiredService<IMigrationRunnerConventions>();
                    var migrationRunner = sp.GetRequiredService<IMigrationRunner>();

                    return new RisingWaveVersionLoader(
                        processorAccessor,
                        migrationRunnerConventions,
                        accessor.VersionTableMetaData,
                        migrationRunner
                    );
                }
            )
            .AddScoped<IMigrationScopeManager>(sp =>
                {
                    var accessor = sp.GetRequiredService<IMigrationProcessor>();

                    return new RisingWaveMigrationScopeHandler(accessor);
                }
            )
            .Configure<RunnerOptions>(opt =>
                {
                    opt.Tags = [schema];
                }
            )
            .BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();
    }
}