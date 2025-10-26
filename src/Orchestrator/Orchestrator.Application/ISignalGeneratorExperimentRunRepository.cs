using CSharpFunctionalExtensions;
using Orchestrator.Domain;

namespace Orchestrator.Application;

public interface ISignalGeneratorExperimentRunRepository
{
    Task<Result<SignalGeneratorExperimentRun>> SaveAsync(SignalGeneratorExperimentRun experimentRun);
    Task<Result<SignalGeneratorExperimentRun>> GetAsync(string experimentRunId);
}