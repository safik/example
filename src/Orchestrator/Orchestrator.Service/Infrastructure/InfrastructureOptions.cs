namespace Orchestrator.Service.Infrastructure;

public class InfrastructureOptions
{
    public const string Section = "Infrastructure";

    public TrialsHPOOptions? TrialsHPO { get; init; }
}

public class TrialsHPOOptions
{
    public static string Section = "TrialsHPO";
    
    public HttpOptions? Http { get; init; }

    public class HttpOptions
    {
        public const string Section = "Http";

        public required bool Enabled { get; init; }
        
        public required string Host { get; init; }
    }
}