using CSharpFunctionalExtensions;
using k8s.Models;
using Orchestrator.Application;
using Orchestrator.Infrastructure.Kubernetes.Workflows;

namespace Orchestrator.Infrastructure.Kubernetes;

public class StartSignalGeneratorExperimentRunRunHandler : IStartSignalGeneratorExperimentRunHandler
{
    private readonly IWorkflowSpecFactory _workflowSpecFactory;
    private readonly IInfrastructureEnvVarsProvider _infrastructureEnvVarsProvider;
    private readonly IKubernetesClient _kubernetesClient;
    private readonly IAlgorithmStepsProvider _algorithmStepsProvider;
    private readonly ITrialsConfigMapFactory _trialsConfigMapFactory;
    private readonly ISignalGeneratorExperimentRunRepository _signalGeneratorExperimentRunRepository;

    public StartSignalGeneratorExperimentRunRunHandler(
        IWorkflowSpecFactory workflowSpecFactory,
        IInfrastructureEnvVarsProvider infrastructureEnvVarsProvider,
        IKubernetesClient kubernetesClient,
        IAlgorithmStepsProvider algorithmStepsProvider,
        ITrialsConfigMapFactory trialsConfigMapFactory,
        ISignalGeneratorExperimentRunRepository signalGeneratorExperimentRunRepository
    )
    {
        _workflowSpecFactory = workflowSpecFactory;
        _infrastructureEnvVarsProvider = infrastructureEnvVarsProvider;
        _kubernetesClient = kubernetesClient;
        _algorithmStepsProvider = algorithmStepsProvider;
        _trialsConfigMapFactory = trialsConfigMapFactory;
        _signalGeneratorExperimentRunRepository = signalGeneratorExperimentRunRepository;
    }

    public async Task<Result> HandleAsync(string experimentRunId, CancellationToken cancellationToken)
    {
        var workflowName = $"signal-generator-experiment-run-{experimentRunId}";
        var configMapName = $"{workflowName}-cm";

        var result =
            await _signalGeneratorExperimentRunRepository
                .GetAsync(experimentRunId)
                .Ensure(e => e.Trials is {Count: > 0}, "Trials must be provided")
                .Map(e => (e, workflowName, configMapName))
                .Bind(t =>
                    {
                        return _trialsConfigMapFactory
                            .Create(t.e.Trials!, t.configMapName)
                            .Map(cm => (t.e, t.workflowName, t.configMapName, cm));
                    }
                )
                .Bind(t =>
                    {
                        return _kubernetesClient
                            .CreateConfigMapAsync(t.cm, cancellationToken)
                            .Map(_ => (t.e, t.workflowName, t.configMapName));
                    }
                )
                .Bind(t =>
                    {
                        return _algorithmStepsProvider
                            .GetAsync(t.e.Hyperparameters.SignalGeneratorAlgorithmId)
                            .Map(steps => (t.e, t.workflowName, t.configMapName, steps));
                    }
                )
                .Bind(t =>
                    {
                        return _workflowSpecFactory
                            .Create(
                                t.e.Id,
                                t.steps,
                                t.e.TrainingEnd.AddDays(-(t.e.Hyperparameters.NumberOfWeeksDuration - 1) * 7),
                                t.e.TrainingEnd,
                                t.e.Hyperparameters.NumberOfWeeksHistoricalData,
                                t.e.NumberOfCpuCoresRequested,
                                t.e.Hyperparameters.TickersRaw,
                                t.e.Hyperparameters.TickersPreset,
                                _infrastructureEnvVarsProvider.GetVars().ToList(),
                                t.configMapName
                            )
                            .Map(spec => (t.e, t.workflowName, spec));
                    }
                )
                .Map(t => (
                        Workflow: new WorkflowV1
                        {
                            Kind = "Workflow",
                            ApiVersion = "argoproj.io/v1alpha1",
                            Metadata = new V1ObjectMeta
                            {
                                Name = t.workflowName,
                                Annotations = new Dictionary<string, string>
                                {
                                    {WorkflowAnnotations.SignalGeneratorExperimentRunId, t.e.Id}
                                }
                            },
                            Spec = t.spec
                        },
                        ExperimentRun: t.e
                    )
                )
                .Bind(t =>
                    {
                        return _kubernetesClient
                            .CreateWorkflow(t.Workflow, cancellationToken)
                            .Map(_ => t.ExperimentRun);
                    }
                )
                .Map(e =>
                    {
                        e.SetStarted();

                        return _signalGeneratorExperimentRunRepository.SaveAsync(e);
                    }
                )
                .Map(_ => Result.Success())
                .OnFailureCompensate(err => Result.Failure(err));

        return result;
    }
}