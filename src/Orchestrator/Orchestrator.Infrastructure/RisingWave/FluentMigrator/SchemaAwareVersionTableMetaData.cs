using FluentMigrator.Runner.VersionTableInfo;

namespace Orchestrator.Infrastructure.RisingWave.FluentMigrator;

public class SchemaAwareVersionTableMetaData : IVersionTableMetaData
{

    public SchemaAwareVersionTableMetaData(string schema)
    {
        SchemaName = schema;
    }

    public string SchemaName { get; }
    
    public virtual string TableName => MigrationConsts.VersionTableName;
    public virtual string ColumnName => MigrationConsts.ColumnName;
    public virtual string AppliedOnColumnName => MigrationConsts.AppliedOnColumnName;
    public bool CreateWithPrimaryKey => false;
    public virtual string DescriptionColumnName => MigrationConsts.DescriptionColumnName;
    public virtual string UniqueIndexName => throw new NotImplementedException();
    public bool OwnsSchema => false;
}