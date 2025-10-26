using FluentMigrator;

namespace Orchestrator.Infrastructure.RisingWave.FluentMigrator.Migrations.Orchestration;

[Migration(2), Tags(DbConsts.Orchestration.Schema)]
public class Orchestration_Tables_2 : Migration
{
    public override void Up()
    {
        Execute.Sql(
            $"""
             CREATE TABLE IF NOT EXISTS {DbConsts.Orchestration.Schema}.{DbConsts.Orchestration.Tables.SignalGeneratorAlgorithms} 
             (
                 id varchar PRIMARY key,
                 hyperparameter_space jsonb NOT NULL,
                 algorithm_steps jsonb NOT NULL
             )
             """
        );
    }

    public override void Down()
    {
    }
}