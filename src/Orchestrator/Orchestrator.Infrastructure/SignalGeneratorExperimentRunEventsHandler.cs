using CSharpFunctionalExtensions;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Orchestrator.Application;
using Orchestrator.Infrastructure.RisingWave.FluentMigrator;
using Orchestrator.Domain;
using static System.Threading.Tasks.ConfigureAwaitOptions;

namespace Orchestrator.Infrastructure;

public class SignalGeneratorExperimentRunEventsHandler : BackgroundService
{
    private readonly IExperimentRunProvider _experimentRunProvider;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger _logger;

    public SignalGeneratorExperimentRunEventsHandler(
        ILogger<SignalGeneratorExperimentRunEventsHandler> logger,
        IExperimentRunProvider experimentRunProvider,
        IServiceScopeFactory serviceScopeFactory
    )
    {
        _logger = logger;
        _experimentRunProvider = experimentRunProvider;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var experimentRunsEvents = _experimentRunProvider.GetNewExperimentRuns(stoppingToken);
                
                await foreach (var experimentRunEvent in experimentRunsEvents)
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Start processing {experimentRun}", experimentRunEvent);
                        
                        if (stoppingToken.IsCancellationRequested)
                            return;

                        using var scope = _serviceScopeFactory.CreateScope();

                        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();

                        var currentStatus = await connection.QuerySingleAsync<string>(
                            new CommandDefinition(
                                $"""
                                 SELECT status
                                 FROM {DbConsts.Orchestration.Schema}.{DbConsts.Orchestration.Tables.SignalGeneratorExperimentRuns}
                                 WHERE id = @experimentId
                                 """,
                                new
                                {
                                    experimentId = experimentRunEvent.Id
                                },
                                cancellationToken: stoppingToken
                            )
                        );

                        var result = currentStatus switch
                        {
                            nameof(SignalGeneratorExperimentRun.ExperimentStatus.None) => await scope
                                .ServiceProvider
                                .GetRequiredService<IInitSignalGeneratorExperimentRunHandler>()
                                .HandleAsync(experimentRunEvent.Id),
                            nameof(SignalGeneratorExperimentRun.ExperimentStatus.Starting) => await scope
                                .ServiceProvider
                                .GetRequiredService<IStartSignalGeneratorExperimentRunHandler>()
                                .HandleAsync(experimentRunEvent.Id, stoppingToken),
                            nameof(SignalGeneratorExperimentRun.ExperimentStatus.Started) => Result.Success(),
                            nameof(SignalGeneratorExperimentRun.ExperimentStatus.Finished) => Result.Success(),
                            _ => throw new IndexOutOfRangeException()
                        };

                        if (result.IsSuccess)
                            break;
                        
                        _logger.LogError("Failed to process {experiment}. Waiting to retry", experimentRunEvent);
                            
                        await Task.Delay(10000, stoppingToken).ConfigureAwait(SuppressThrowing);
                    }
                }
            }
            catch(Exception e)
            {
                _logger.LogError(e, e.Message);
                await Task.Delay(10000, stoppingToken).ConfigureAwait(SuppressThrowing);
            }
        }
    }
}