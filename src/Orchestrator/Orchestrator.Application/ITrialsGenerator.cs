using CSharpFunctionalExtensions;

namespace Orchestrator.Application;

public interface ITrialsGenerator
{
    Task<Result<ReadOnlyMemory<byte>>> GenerateAsync(
        string study,
        string signalGeneratorTrialId,
        ReadOnlyMemory<byte> algorithmHyperparameterSpace
    );
}