using FluentMigrator;

namespace Orchestrator.Infrastructure.RisingWave.FluentMigrator.Migrations.SignalGenerators;

[Migration(2), Tags(DbConsts.SignalGenerators.Schema)]
public class Orders_Table_2 : Migration
{
    public override void Up()
    {
        Execute.Sql(
            $"""
             CREATE TABLE IF NOT EXISTS {DbConsts.SignalGenerators.Schema}.{DbConsts.SignalGenerators.Tables.Orders} 
             (
                 trial_Id varchar, 
                 period_date_local date, 
                 ticker varchar, 
                 open_time_local timestamp, 
                 is_long bool default false, 
                 created_at_utc timestamp, 
                 open_price real, 
                 close_time_local timestamp, 
                 close_price real, 
                 highest_price real, 
                 lowest_price real,
                 PRIMARY KEY (trial_id, period_date_local, ticker, open_time_local)
             );
             """
        );
    }

    public override void Down()
    {
    }
}