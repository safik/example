using System.Text.Json;
using CSharpFunctionalExtensions;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using Orchestrator.Infrastructure.Kubernetes.Workflows;
using Orchestrator.Infrastructure.RisingWave.FluentMigrator;

namespace Orchestrator.Infrastructure.Kubernetes;

public interface IAlgorithmStepsProvider
{
    Task<Result<IReadOnlyList<AlgorithmStep>>> GetAsync(string signalGeneratorAlgorithmId);
}

public class AlgorithmStepsProvider : IAlgorithmStepsProvider
{
    private readonly NpgsqlConnection _connection;
    private readonly ILogger<AlgorithmStepsProvider> _logger;
    private readonly CancellationToken _cancellationToken;

    public AlgorithmStepsProvider(
        NpgsqlConnection connection,
        ILogger<AlgorithmStepsProvider> logger,
        CancellationToken cancellationToken
    )
    {
        _connection = connection;
        _logger = logger;
        _cancellationToken = cancellationToken;
    }

    public async Task<Result<IReadOnlyList<AlgorithmStep>>> GetAsync(string signalGeneratorAlgorithmId)
    {
        try
        {
            var algorithmStepsJson = await _connection.QuerySingleOrDefaultAsync<string?>(
                new CommandDefinition(
                    $"""
                     SELECT algorithm_steps 
                     FROM {DbConsts.Orchestration.Schema}.{DbConsts.Orchestration.Tables.SignalGeneratorAlgorithms} 
                     WHERE id = @algorithmId
                     """,
                    new {algorithmId = signalGeneratorAlgorithmId},
                    cancellationToken: _cancellationToken
                )
            );

            if (string.IsNullOrEmpty(algorithmStepsJson))
            {
                throw new InvalidOperationException();
            }

            var algorithmSteps = JsonSerializer.Deserialize<IReadOnlyList<AlgorithmStep>>(algorithmStepsJson);
            return Result.Success(algorithmSteps ?? throw new InvalidOperationException());

        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Error obtaining algorithm steps for {signalGeneratorAlgorithmId}: {message}",
                signalGeneratorAlgorithmId,
                e.Message
            );
            
            return Result.Failure<IReadOnlyList<AlgorithmStep>>(e.Message);
        }
    }
}