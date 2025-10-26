using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Orchestrator.Infrastructure.RisingWave.FluentMigrator;
using Orchestrator.Domain;

namespace Orchestrator.Infrastructure.RisingWave;

public class ExperimentRunEnumerator : IAsyncEnumerator<SignalGeneratorExperimentRunEvent>
{
    private readonly CancellationToken _cancellationToken;
    private readonly IServiceScope _scope;
    private NpgsqlConnection? _connection;
    private readonly ILogger<ExperimentRunEnumerator> _logger;
    private readonly bool _onlyUpdates;

    public ExperimentRunEnumerator(CancellationToken cancellationToken, IServiceScope scope, bool onlyUpdates)
    {
        _cancellationToken = cancellationToken;
        _scope = scope;
        _onlyUpdates = onlyUpdates;
        _logger = _scope.ServiceProvider.GetRequiredService<ILogger<ExperimentRunEnumerator>>();
    }

    public async ValueTask<bool> MoveNextAsync()
    {
        if (_connection == null)
        {
            _connection = _scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();

            _logger.LogInformation("Checking subscriptions");

            var subscriptionCount = await _connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    $"""
                     SELECT COUNT(*) 
                     FROM rw_catalog.rw_subscriptions 
                     WHERE name = '{DbConsts.Orchestration.Subscriptions.SignalGeneratorExperimentRuns}'
                     """,
                    cancellationToken: _cancellationToken
                )
            );

            _logger.LogInformation("Subscriptions found: {subscriptionCount}", subscriptionCount);

            if (subscriptionCount == 0)
            {
                await _connection.ExecuteAsync(
                    new CommandDefinition(
                        $"""
                         CREATE SUBSCRIPTION {DbConsts.Orchestration.Schema}.{DbConsts.Orchestration.Subscriptions.SignalGeneratorExperimentRuns} 
                         FROM {DbConsts.Orchestration.Schema}.{DbConsts.Orchestration.Tables.SignalGeneratorExperimentRuns} 
                         WITH (retention = '7D')
                         """,
                        cancellationToken: _cancellationToken
                    )
                );
            }

            _logger.LogInformation("Creating cursor");

            await _connection.ExecuteScalarAsync(
                new CommandDefinition(
                    $"""
                     DECLARE {DbConsts.Orchestration.Subscriptions.SignalGeneratorExperimentRuns}_cursor 
                     SUBSCRIPTION CURSOR 
                     FOR {DbConsts.Orchestration.Schema}.{DbConsts.Orchestration.Subscriptions.SignalGeneratorExperimentRuns} FULL
                     """,
                    cancellationToken: _cancellationToken
                )
            );
        }

        while (!_cancellationToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Fetching next");
                
                var operation = await _connection.QuerySingleOrDefaultAsync<ExperimentOp>(
                    new CommandDefinition(
                        $"""
                         FETCH next 
                         FROM {DbConsts.Orchestration.Subscriptions.SignalGeneratorExperimentRuns}_cursor WITH (timeout = '30s');
                         """,
                        cancellationToken: _cancellationToken,
                        commandTimeout: 60 //Should be greater than cursor timeout to avoid broken connection state
                    )
                );

                if (operation == null)
                {
                    _logger.LogInformation("Fetch returned null");
                    continue;
                }

                _logger.LogInformation(
                    "Operation fetched: ({trialBathcId}:{operationOp})",
                    operation.Id,
                    operation.Op
                );

                if (_onlyUpdates && operation.Op != Operation.Insert)
                {
                    _logger.LogInformation("Skipping non insert operation");
                    continue;
                }
               
                Current = new SignalGeneratorExperimentRunEvent(
                    operation.Id,
                    Enum.Parse<SignalGeneratorExperimentRun.ExperimentStatus>(operation.Status)
                );

                return true;
            }
            catch (NpgsqlException e) when (e.InnerException?.GetType() == typeof(TimeoutException))
            {
                _logger.LogInformation("Fetch next timeout");
            }
        }
        
        return false;
    }

    public SignalGeneratorExperimentRunEvent Current { get; private set; } = null!;

    public async ValueTask DisposeAsync()
    {
        await CastAndDispose(_scope);

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }
}

public class ExperimentOp
{
    public required string Id { get; init; } 
    public required Operation Op { get; init; }
    public required string Status { get; init; }
    
    public override string ToString()
    {
        return
            $"{nameof(Id)}: {Id}, "
            + $"{nameof(Op)}: {Op}";
    }
}

public enum Operation
{
    Insert,
    UpdateInsert,
    Delete,
    UpdateDelete
}