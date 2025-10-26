namespace Orchestrator.Infrastructure.RisingWave.FluentMigrator;

public class DbConsts
{
    public class Orchestration
    {
        public const string Schema = "orchestration";
        
        public class Tables
        {
            public const string SignalGeneratorExperimentRuns = "signal_generator_experiment_runs";
            public const string SignalGeneratorTrials = "signal_generator_trials";
            public const string SignalGeneratorAlgorithms = "signal_generator_algorithms";
        }
        
        public class Subscriptions
        {
            public const string SignalGeneratorExperimentRuns = "signal_generator_experiment_runs_subscription";
        }

    }

    public class SignalGenerators
    {
        public const string Schema = "signal_generators";
        
        public class Tables
        {
            public const string Orders = "orders";
            public const string Periods = "periods";
            public const string Phases = "phases";
        }
    }
}