using FluentMigrator;
using FluentMigrator.Runner;

namespace Orchestrator.Infrastructure.RisingWave.FluentMigrator;

public class RisingWaveMigrationScopeHandler : IMigrationScopeManager
{
    private readonly IMigrationProcessor _processor;

    public RisingWaveMigrationScopeHandler(IMigrationProcessor processor)
    {
        _processor = processor;
    }

    public IMigrationScope? CurrentScope { get; private set; }

    public IMigrationScope BeginScope()
    {
        GuardAgainstActiveMigrationScope();
        CurrentScope = new TransactionalMigrationScope(_processor, () => CurrentScope = null);
        return CurrentScope;
    }

    public IMigrationScope CreateOrWrapMigrationScope(bool transactional = true)
    {
        return new NoOpMigrationScope();
    }

    private void GuardAgainstActiveMigrationScope()
    {
        if (HasActiveMigrationScope) throw new InvalidOperationException("The runner is already in an active migration scope.");
    }

    private bool HasActiveMigrationScope => CurrentScope is {IsActive: true};
}