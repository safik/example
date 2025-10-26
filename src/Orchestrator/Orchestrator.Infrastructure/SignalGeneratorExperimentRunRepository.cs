using System.Text;
using System.Text.Json;
using CSharpFunctionalExtensions;
using Dapper;
using Npgsql;
using Orchestrator.Application;
using Orchestrator.Infrastructure.RisingWave.FluentMigrator;
using Orchestrator.Domain;

namespace Orchestrator.Infrastructure;

public class SignalGeneratorExperimentRunRepository : ISignalGeneratorExperimentRunRepository
{
    private readonly CancellationToken _cancellationToken = CancellationToken.None; //TODO Inject from constructor
    private readonly NpgsqlConnection _connection;

    public SignalGeneratorExperimentRunRepository(
        NpgsqlConnection connection
    )
    {
        _connection = connection;
    }

    public async Task<Result<SignalGeneratorExperimentRun>> SaveAsync(SignalGeneratorExperimentRun experimentRun)
    {
        try
        {
            if (experimentRun.Trials is null || experimentRun.Trials.Count == 0)
                throw new InvalidOperationException("Trials must not be null/empty.");

            await _connection.ExecuteAsync(
                new CommandDefinition(
                    $"""
                     UPDATE {DbConsts.Orchestration.Schema}.{DbConsts.Orchestration.Tables.SignalGeneratorExperimentRuns}
                     SET
                         number_of_trials = @number_of_trials, 
                         "status" = @status,
                         training_end = @training_end::date,
                         hyperparameters = @hyperparameters::jsonb,
                         number_of_cpu_cores_requested = @number_of_cpu_cores_requested,
                         updated_at = now()
                     WHERE id = @experiment_id;
                     """,
                    new
                    {
                        experiment_id = experimentRun.Id,
                        number_of_trials = experimentRun.NumberOfTrials,
                        hyperparameters = JsonSerializer.Serialize(experimentRun.Hyperparameters, JsonSerializerOptions.Web),
                        status = experimentRun.Status.ToString(),
                        training_end = experimentRun.TrainingEnd.ToString("O"),
                        number_of_cpu_cores_requested = experimentRun.NumberOfCpuCoresRequested
                    },
                    cancellationToken: _cancellationToken
                )
            );

            await _connection.ExecuteAsync(
                new CommandDefinition(
                    $"""
                     INSERT INTO {DbConsts.Orchestration.Schema}.{DbConsts.Orchestration.Tables.SignalGeneratorTrials}
                         (id, signal_generator_experiment_run_id, hyperparameters)
                     SELECT
                       @trial_id,
                       @experiment_id,
                       CASE WHEN @hp_text IS NULL THEN NULL ELSE @hp_text::jsonb END
                     WHERE NOT EXISTS (
                       SELECT 1 FROM orchestration.signal_generator_trials t WHERE t.id = @trial_id
                     );
                     """,
                    experimentRun.Trials.Select(t => new
                        {
                            experiment_id = experimentRun.Id,
                            trial_id = t.Id,
                            hp_text = t.HyperparametersRaw.IsEmpty
                                ? null
                                : Encoding.UTF8.GetString(t.HyperparametersRaw.Span)
                        }
                    ),
                    cancellationToken: _cancellationToken
                )
            );
        }
        catch (Exception e)
        {
            return Result.Failure<SignalGeneratorExperimentRun>(e.Message);
        }

        return Result.Success(experimentRun);
    }

    public async Task<Result<SignalGeneratorExperimentRun>> GetAsync(string experimentRunId)
    {
        var experimentRun = await _connection.QuerySingleOrDefaultAsync<SignalGeneratorExperimentRunDto>(
            new CommandDefinition(
                $"""
                 SELECT number_of_trials, training_end, hyperparameters, number_of_cpu_cores_requested, status
                 FROM {DbConsts.Orchestration.Schema}.{DbConsts.Orchestration.Tables.SignalGeneratorExperimentRuns}
                 WHERE id = @experiment_run_id
                 """,
                new {experiment_run_id = experimentRunId},
                cancellationToken: _cancellationToken
            )
        );
        
        var trials = await _connection.QueryAsync<(string, string)>(
            new CommandDefinition(
                $"""
                 SELECT id, hyperparameters
                 FROM {DbConsts.Orchestration.Schema}.{DbConsts.Orchestration.Tables.SignalGeneratorTrials}
                 WHERE signal_generator_experiment_run_id = @experiment_id
                 """,
                new {experiment_id = experimentRunId},
                cancellationToken: _cancellationToken
            )
        );
        
        return new SignalGeneratorExperimentRun(
            experimentRunId,
            experimentRun.NumberOfTrials,
            DateOnly.FromDateTime(experimentRun.TrainingEnd), 
            trials
                .Select(t =>
                    {
                        return new SignalGeneratorExperimentRun.SignalGeneratorTrial(
                            t.Item1,
                            Encoding.UTF8.GetBytes(t.Item2)
                        );
                    }
                )
                .ToList(),
            JsonSerializer.Deserialize<SignalGeneratorExperimentRun.ExperimentHyperparameters>(experimentRun.Hyperparameters, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException(),
            Enum.Parse<SignalGeneratorExperimentRun.ExperimentStatus>(experimentRun.Status),
            experimentRun.NumberOfCpuCoresRequested
        );
    }
    
    public class SignalGeneratorExperimentRunDto
    {
        public required int NumberOfTrials { get; init; }
        public required DateTime TrainingEnd { get; init; }
        public required string Hyperparameters { get; init; }
        public required int NumberOfCpuCoresRequested { get; init; }
        public required string Status { get; init; }
    }
}