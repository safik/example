using Orchestrator.Infrastructure.RisingWave.FluentMigrator;

namespace Orchestrator.Service.Infrastructure;

public class RisingWaveMigrator : IHostedService
{
    private readonly ISchemaMigrator _schemaMigrator;

    public RisingWaveMigrator(ISchemaMigrator schemaMigrator)
    {
        _schemaMigrator = schemaMigrator;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _schemaMigrator.MigrateSchema(DbConsts.Orchestration.Schema);
        _schemaMigrator.MigrateSchema(DbConsts.SignalGenerators.Schema);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}