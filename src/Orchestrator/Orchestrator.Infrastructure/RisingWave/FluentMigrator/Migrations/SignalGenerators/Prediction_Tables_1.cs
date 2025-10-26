using FluentMigrator;

namespace Orchestrator.Infrastructure.RisingWave.FluentMigrator.Migrations.SignalGenerators;

[Migration(1), Tags(DbConsts.SignalGenerators.Schema)]
public class Prediction_Tables_1 : Migration
{
    public override void Up()
    {
        Execute.Sql(
            $"""
             CREATE TABLE IF NOT EXISTS {DbConsts.SignalGenerators.Schema}.{DbConsts.SignalGenerators.Tables.Periods} 
             (
                 trial_id varchar,
                 period_date_local date,
                 is_finished bool default false,
                 PRIMARY KEY (trial_id, period_date_local)
             );
             """
        );
        
        Execute.Sql(
            $"""
             CREATE TABLE IF NOT EXISTS {DbConsts.SignalGenerators.Schema}.{DbConsts.SignalGenerators.Tables.Phases} 
             (
                 trial_id varchar,
                 period_date_local date,
                 phase_name varchar,
                 is_finished bool default false,
                 PRIMARY KEY (trial_id, period_date_local, phase_name)
             );
             """
        );
    }

    public override void Down()
    {
    }
}