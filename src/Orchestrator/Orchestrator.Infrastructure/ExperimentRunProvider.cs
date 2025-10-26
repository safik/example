using Microsoft.Extensions.DependencyInjection;
using Orchestrator.Infrastructure.RisingWave;
using Orchestrator.Domain;

namespace Orchestrator.Infrastructure;

public interface IExperimentRunProvider
{
    IAsyncEnumerable<SignalGeneratorExperimentRunEvent> GetNewExperimentRuns(CancellationToken stoppingToken);
}

public record SignalGeneratorExperimentRunEvent(string Id, SignalGeneratorExperimentRun.ExperimentStatus ExperimentStatus);

public class ExperimentRunProvider : IExperimentRunProvider
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    

    public ExperimentRunProvider(
        IServiceScopeFactory serviceScopeFactory
        )
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public IAsyncEnumerable<SignalGeneratorExperimentRunEvent> GetNewExperimentRuns(CancellationToken stoppingToken)
    {
        var asyncEnumerable = AsyncEnumerable.Create(ct =>
            {
                var serviceScope = _serviceScopeFactory.CreateScope();
                
                return new ExperimentRunEnumerator(ct, serviceScope, false);
            }
        );

        return asyncEnumerable;
    }
}