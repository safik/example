using System.Text.Json;
using CSharpFunctionalExtensions;
using HyperparameterOptimization.Client;
using Orchestrator.Application;

namespace Orchestrator.Infrastructure;

public class TrialsGenerator : ITrialsGenerator
{
    private readonly HyperparameterOptimizationClient _hpoClient;

    public TrialsGenerator(HyperparameterOptimizationClient hpoClient)
    {
        _hpoClient = hpoClient;
    }

    public async Task<Result<ReadOnlyMemory<byte>>> GenerateAsync(
        string study,
        string signalGeneratorTrialId,
        ReadOnlyMemory<byte> algorithmHyperparameterSpace
    )
    {
        try
        {
            var doc = JsonDocument.Parse(algorithmHyperparameterSpace);

            var result = await _hpoClient.GenerateHyperparameters(
                study,
                signalGeneratorTrialId,
                doc.RootElement,
                CancellationToken.None
            );

            return Result.Success<ReadOnlyMemory<byte>>(JsonSerializer.SerializeToUtf8Bytes(result.Hyperparameters));
        }
        catch (Exception e)
        {
            return Result.Failure<ReadOnlyMemory<byte>>(e.Message);
        }
    }
}