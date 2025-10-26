using CSharpFunctionalExtensions;
using Dapper;
using Npgsql;
using Orchestrator.Application;
using Orchestrator.Infrastructure.RisingWave.FluentMigrator;

namespace Orchestrator.Infrastructure;

public class AlgorithmHyperparameterSpaceProvider : IAlgorithmHyperparameterSpaceProvider
{
    private readonly NpgsqlConnection _connection;

    public AlgorithmHyperparameterSpaceProvider(NpgsqlConnection connection)
    {
        _connection = connection;
    }

    public async Task<Result<ReadOnlyMemory<byte>>> GetAsync(string signalGeneratorAlgorithmId)
    {
        try
        {
            var hyperparameterSpace = await _connection.QuerySingleOrDefaultAsync<string>(
                new CommandDefinition(
                    $"""
                     SELECT hyperparameter_space 
                     FROM {DbConsts.Orchestration.Schema}.{DbConsts.Orchestration.Tables.SignalGeneratorAlgorithms} 
                     WHERE id = @algorithmId
                     """,
                    new {algorithmId = signalGeneratorAlgorithmId},
                    cancellationToken: CancellationToken.None //TODO Pass through constructor
                )
            );

            return hyperparameterSpace is null
                ? ReadOnlyMemory<byte>.Empty
                : System.Text.Encoding.UTF8.GetBytes(hyperparameterSpace);
        }
        catch (Exception e)
        {
            return Result.Failure<ReadOnlyMemory<byte>>(e.Message);
        }
    }
}