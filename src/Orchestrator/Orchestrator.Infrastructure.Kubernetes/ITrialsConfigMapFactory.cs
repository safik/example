using System.Text.Json;
using CSharpFunctionalExtensions;
using k8s.Models;
using Orchestrator.Domain;

namespace Orchestrator.Infrastructure.Kubernetes;

public interface ITrialsConfigMapFactory
{
    Result<V1ConfigMap> Create(IReadOnlyList<SignalGeneratorExperimentRun.SignalGeneratorTrial> trials, string configMapName);
}

public class TrialsConfigMapFactory : ITrialsConfigMapFactory
{
    public Result<V1ConfigMap> Create(IReadOnlyList<SignalGeneratorExperimentRun.SignalGeneratorTrial> trials, string configMapName)
    {
        if (trials.Count == 0)
            return Result.Failure<V1ConfigMap>("Trials list is empty.");

        try
        {
            var payload = new
            {
                Trials = trials
                    .Select(t => new
                        {
                            Id = t.Id,
                            Hyperparameters = JsonDocument.Parse(t.HyperparametersRaw).RootElement
                        }
                    )
                    .ToList()
            };

            var cm = new V1ConfigMap
            {
                Metadata = new V1ObjectMeta
                {
                    Name = configMapName,
                    NamespaceProperty = KubernetesWorkflowConstants.ArgoNamespace
                },
                Data = new Dictionary<string, string>
                {
                    [KubernetesWorkflowConstants.TrialsConfigMapFileName] = JsonSerializer.Serialize(payload)
                }
            };

            return Result.Success(cm);
        }
        catch (JsonException e)
        {
            return Result.Failure<V1ConfigMap>($"Invalid HyperparametersRaw JSON: {e.Message}");
        }
        catch (Exception e)
        {
            return Result.Failure<V1ConfigMap>(e.Message);
        }
    }
}