using CSharpFunctionalExtensions;

namespace Orchestrator.Application;

public interface IAlgorithmHyperparameterSpaceProvider
{
    Task<Result<ReadOnlyMemory<byte>>> GetAsync(string signalGeneratorAlgorithmId);
}