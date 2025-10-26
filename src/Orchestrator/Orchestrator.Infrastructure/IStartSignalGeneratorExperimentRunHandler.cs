using CSharpFunctionalExtensions;

namespace Orchestrator.Infrastructure;

public interface IStartSignalGeneratorExperimentRunHandler
{
    Task<Result> HandleAsync(string experimentRunId, CancellationToken cancellationToken);
}