using System.Data;
using FluentMigrator;
using FluentMigrator.Expressions;
using FluentMigrator.Model;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Processors;
using FluentMigrator.Runner.Versioning;
using FluentMigrator.Runner.VersionTableInfo;
using Orchestrator.Infrastructure.RisingWave.FluentMigrator.Migrations;

namespace Orchestrator.Infrastructure.RisingWave.FluentMigrator;

public class RisingWaveVersionLoader : IVersionLoader
{
    private readonly IMigrationProcessor _processor;

    private bool _versionMigrationAlreadyRun;
    private IVersionInfo? _versionInfo;
    private IMigrationRunnerConventions Conventions { get; set; }

    public IVersionTableMetaData VersionTableMetaData { get; }

    public IMigrationRunner Runner { get; set; }
    public VersionSchemaMigration VersionSchemaMigration { get; }
    public IMigration VersionMigration { get; }
    public IMigration VersionUniqueMigration { get; }
    public IMigration VersionDescriptionMigration { get; }


    public RisingWaveVersionLoader(
        IProcessorAccessor processorAccessor,
        IMigrationRunnerConventions conventions,
        IVersionTableMetaData versionTableMetaData,
        IMigrationRunner runner
    )
    {
        _processor = processorAccessor.Processor;

        Runner = runner;

        Conventions = conventions;
        VersionTableMetaData = versionTableMetaData;
        VersionMigration = new VersionMigration(VersionTableMetaData);
        VersionSchemaMigration = new VersionSchemaMigration(VersionTableMetaData);
        VersionUniqueMigration = new VersionUniqueMigration(VersionTableMetaData);
        VersionDescriptionMigration = new VersionDescriptionMigration(VersionTableMetaData);

        LoadVersionInfo();
    }

    public void UpdateVersionInfo(long version)
    {
        UpdateVersionInfo(version, null);
    }

    public void UpdateVersionInfo(long version, string? description)
    {
        var dataExpression = new InsertDataExpression();
        dataExpression.Rows.Add(CreateVersionInfoInsertionData(version, description));
        dataExpression.TableName = VersionTableMetaData.TableName;
        dataExpression.SchemaName = VersionTableMetaData.SchemaName;

        dataExpression.ExecuteWith(_processor);
    }


    public IVersionTableMetaData GetVersionTableMetaData()
    {
        return VersionTableMetaData;
    }

    protected virtual InsertionDataDefinition CreateVersionInfoInsertionData(long version, string? description)
    {
        return new InsertionDataDefinition
        {
            new KeyValuePair<string, object>(VersionTableMetaData.ColumnName, version),
            new KeyValuePair<string, object>(VersionTableMetaData.AppliedOnColumnName, DateTime.UtcNow),
            new KeyValuePair<string, object>(VersionTableMetaData.DescriptionColumnName, description ?? throw new ArgumentNullException(nameof(description))),
        };
    }

    public IVersionInfo VersionInfo
    {
        get => _versionInfo ?? throw new InvalidOperationException();
        set => _versionInfo = value ?? throw new ArgumentException("Cannot set VersionInfo to null");
    }

    public bool AlreadyCreatedVersionSchema => true;

    public bool AlreadyCreatedVersionTable => _processor.TableExists(VersionTableMetaData.SchemaName, VersionTableMetaData.TableName);

    public bool OwnsVersionSchema => VersionTableMetaData.OwnsSchema;

    public void LoadVersionInfo()
    {
        if (!AlreadyCreatedVersionTable && !_versionMigrationAlreadyRun)
        {
            Runner.Up(new MigrationInit(VersionTableMetaData.SchemaName));
            _versionMigrationAlreadyRun = true;
        }
      
        _versionInfo = new VersionInfo();

        if (!AlreadyCreatedVersionTable) return;

        var dataSet = _processor.ReadTableData(VersionTableMetaData.SchemaName, VersionTableMetaData.TableName);
        foreach (DataRow row in dataSet.Tables[0].Rows)
        {
            _versionInfo.AddAppliedMigration(long.Parse(row[VersionTableMetaData.ColumnName].ToString() ?? throw new InvalidOperationException()));
        }
    }

    public void RemoveVersionTable()
    {
        var expression = new DeleteTableExpression
            {TableName = VersionTableMetaData.TableName, SchemaName = VersionTableMetaData.SchemaName};
        expression.ExecuteWith(_processor);

        if (OwnsVersionSchema && !string.IsNullOrEmpty(VersionTableMetaData.SchemaName))
        {
            var schemaExpression = new DeleteSchemaExpression {SchemaName = VersionTableMetaData.SchemaName};
            schemaExpression.ExecuteWith(_processor);
        }
    }

    public void DeleteVersion(long version)
    {
        var expression = new DeleteDataExpression
            {TableName = VersionTableMetaData.TableName, SchemaName = VersionTableMetaData.SchemaName};
        expression.Rows.Add(
            new DeletionDataDefinition
            {
                new KeyValuePair<string, object>(VersionTableMetaData.ColumnName, version)
            }
        );
        expression.ExecuteWith(_processor);
    }
}
