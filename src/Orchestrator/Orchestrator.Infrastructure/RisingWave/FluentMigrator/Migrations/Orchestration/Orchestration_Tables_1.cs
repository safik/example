using FluentMigrator;

namespace Orchestrator.Infrastructure.RisingWave.FluentMigrator.Migrations.Orchestration;

[Migration(1), Tags(DbConsts.Orchestration.Schema)]
public class Orchestration_Tables_1 : Migration
{
    public override void Up()
    {
        Execute.Sql(
            $"""
             CREATE TABLE IF NOT EXISTS {DbConsts.Orchestration.Schema}.{DbConsts.Orchestration.Tables.SignalGeneratorExperimentRuns} 
             (
                 id                    varchar PRIMARY key,
                 number_of_trials      int  NOT NULL,
                 training_end                 date NOT NULL,
                 status                varchar NOT NULL,
                 hyperparameters jsonb NOT NULL,
                 number_of_cpu_cores_requested      int  NOT NULL,
                 updated_at            timestamptz NOT NULL DEFAULT now()
             )
             """
        );
        
        Execute.Sql(
            $"""
             CREATE TABLE IF NOT EXISTS {DbConsts.Orchestration.Schema}.{DbConsts.Orchestration.Tables.SignalGeneratorTrials} 
             (
                 id                    varchar PRIMARY key,
                 signal_generator_experiment_run_id   text NOT NULL,
                 hyperparameters jsonb NULL
             )
             """
        );
    }

    public override void Down()
    {
    }
}