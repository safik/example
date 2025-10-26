namespace Orchestrator.Domain;

public class SignalGeneratorExperimentRun
{
    public SignalGeneratorExperimentRun(
        string id,
        int numberOfTrials,
        DateOnly trainingEnd,
        List<SignalGeneratorTrial> trials,
        ExperimentHyperparameters hyperparameters,
        ExperimentStatus experimentStatus,
        int numberOfCpuCoresRequested
    )
    {
        Id = id;
        NumberOfTrials = numberOfTrials;
        TrainingEnd = trainingEnd;
        Trials = trials;
        Hyperparameters = hyperparameters;
        Status = experimentStatus;
        NumberOfCpuCoresRequested = numberOfCpuCoresRequested;
    }

    public enum ExperimentStatus
    {
        None,
        Starting,
        Started,
        Finished
    }
    
    public record ExperimentHyperparameters(
        int NumberOfWeeksDuration,
        int NumberOfWeeksHistoricalData,
        string? TickersRaw,
        string? TickersPreset,
        string SignalGeneratorAlgorithmId
    );
    
    public record SignalGeneratorTrial(string Id, ReadOnlyMemory<byte> HyperparametersRaw);

    public string Id { get; private set; } 
    public int NumberOfTrials { get; private set; }
    public DateOnly TrainingEnd { get; private set; } 
    public IReadOnlyList<SignalGeneratorTrial>? Trials { get; private set; } 
    public ExperimentHyperparameters Hyperparameters { get; private set; } 
    public ExperimentStatus Status { get; private set; } 
    public int NumberOfCpuCoresRequested { get; private set; }

    public void Initialize(IReadOnlyList<SignalGeneratorTrial> signalGeneratorTrials)
    {
        Status = ExperimentStatus.Starting;
        Trials = signalGeneratorTrials;
    }

    public void SetStarted()
    {
        Status = ExperimentStatus.Started;
    }
}