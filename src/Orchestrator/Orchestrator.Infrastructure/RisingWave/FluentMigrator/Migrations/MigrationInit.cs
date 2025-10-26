using FluentMigrator;

namespace Orchestrator.Infrastructure.RisingWave.FluentMigrator.Migrations;

public class MigrationInit : Migration
{
    private readonly string _schema;
    
    public MigrationInit(string schema)
    {
        _schema = schema;
    }
    
    public override void Up()
    {
        Execute.Sql($"CREATE SCHEMA IF NOT EXISTS {_schema}");
        
        Execute.Sql(
            $"""
             CREATE TABLE IF NOT EXISTS {_schema}.{MigrationConsts.VersionTableName}
             (
                 {MigrationConsts.ColumnName}     bigint,
                 {MigrationConsts.AppliedOnColumnName}   timestamp,
                 {MigrationConsts.DescriptionColumnName} varchar
             )
             """
        );
    }

    public override void Down()
    {
    }
}