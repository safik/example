using Microsoft.Extensions.Options;

namespace Orchestrator.Infrastructure.Kubernetes;

public interface IInfrastructureEnvVarsProvider
{
    IReadOnlyDictionary<string, string> GetVars();
}

public class InfrastructureEnvVarsProvider : IInfrastructureEnvVarsProvider
{
    private readonly IReadOnlyDictionary<string, string> _signalGeneratorEnvVarsOptions;
    
    public InfrastructureEnvVarsProvider(IOptions<SignalGeneratorEnvVarsOptions> signalGeneratorEnvVarsOptions)
    {
        _signalGeneratorEnvVarsOptions = signalGeneratorEnvVarsOptions.Value.Values
            .Select(s => s.Split('=', 2))
            .ToDictionary(kv => kv[0], kv => kv[1]);
    }

    public IReadOnlyDictionary<string, string> GetVars()
    {
        return _signalGeneratorEnvVarsOptions;
    }
}

public class SignalGeneratorEnvVarsOptions
{
    public const string Section = "SignalGeneratorsEnvVars";
    
    public required string[] Values { get; set; }
}