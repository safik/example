using System.Collections.Immutable;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Orchestrator.Domain;

namespace Orchestrator.Application;

public interface IInitSignalGeneratorExperimentRunHandler
{
    Task<Result> HandleAsync(string experimentRunId);
}

public class InitSignalGeneratorExperimentRunHandler : IInitSignalGeneratorExperimentRunHandler
{
    private readonly ISignalGeneratorExperimentRunRepository _signalGeneratorExperimentRunRepository;
    private readonly IAlgorithmHyperparameterSpaceProvider _algorithmHyperparameterSpaceProvider;
    private readonly ITrialsGenerator _trialsGenerator;
    private readonly ILogger _logger;

    public InitSignalGeneratorExperimentRunHandler(
        ISignalGeneratorExperimentRunRepository signalGeneratorExperimentRunRepository,
        IAlgorithmHyperparameterSpaceProvider algorithmHyperparameterSpaceProvider,
        ITrialsGenerator trialsGenerator,
        ILogger<InitSignalGeneratorExperimentRunHandler> logger
    )
    {
        _signalGeneratorExperimentRunRepository = signalGeneratorExperimentRunRepository;
        _algorithmHyperparameterSpaceProvider = algorithmHyperparameterSpaceProvider;
        _trialsGenerator = trialsGenerator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(string experimentRunId)
    {
        var result =
            await _signalGeneratorExperimentRunRepository
                .GetAsync(experimentRunId)
                .Ensure(e => e.NumberOfTrials > 0, "NumberOfTrials must be greater than 0")
                .Map(e => (ExperimentRun: e, AlgorithmId: e.Hyperparameters.SignalGeneratorAlgorithmId))
                .Bind(async t =>
                    {
                        return await _algorithmHyperparameterSpaceProvider
                            .GetAsync(t.AlgorithmId)
                            .Map(space => (t.ExperimentRun, t.AlgorithmId, Space: space));
                    }
                )
                .Bind(async t =>
                    {
                        var experimentId = t.ExperimentRun.Id;

                        try
                        {
                            var trialsResult = await Enumerable
                                .Range(0, t.ExperimentRun.NumberOfTrials)
                                .ToAsyncEnumerable()
                                .AggregateAwaitAsync(
                                    Result.Success(ImmutableList<SignalGeneratorExperimentRun.SignalGeneratorTrial>.Empty),
                                    async (seed, i) =>
                                    {
                                        if (seed.IsFailure)
                                            return seed;

                                        var trialId = $"{experimentId}_{i}";

                                        return await _trialsGenerator
                                            .GenerateAsync(t.AlgorithmId, trialId, t.Space)
                                            .Map(hp =>
                                                {
                                                    var signalGeneratorTrial = new SignalGeneratorExperimentRun.SignalGeneratorTrial(trialId, hp);
                                                    
                                                    return seed.Value.Add(signalGeneratorTrial);
                                                }
                                            );
                                    }
                                );

                            return trialsResult
                                .Map(trials => (t.ExperimentRun, trials));
                        }
                        catch (Exception e)
                        {
                            Result.Failure(e.Message);
                        }

                        throw new InvalidOperationException();
                    }
                )
                .Map(t =>
                    {
                        t.ExperimentRun.Initialize(t.trials);
                        return t.ExperimentRun;
                    }
                )
                .Bind(e => _signalGeneratorExperimentRunRepository.SaveAsync(e))
                .TapError(err => _logger.LogError("Failed to process experiment: {experimentRunId}. {Error}", experimentRunId, err));

        return result;
    }
}
