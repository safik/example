using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace Orchestrator.Infrastructure.Kubernetes.Workflows;

public interface IWorkflowSpecFactory
{
    Result<JsonElement> Create(
        string signalGeneratorExperimentRunId,
        IReadOnlyList<AlgorithmStep> steps,
        DateOnly start,
        DateOnly end,
        int numberOfWeeksHistoricalData,
        int numberOfCpuCoresRequested,
        string? tickersRaw,
        string? tickersPreset,
        IReadOnlyList<KeyValuePair<string, string>> environmentVariables,
        string configMapName
    );
}

public record AlgorithmStep(
    string Name,
    string Image,
    string? Version = null,
    IReadOnlyList<string>? Commands = null,
    IReadOnlyList<string>? Args = null
);

public class WorkflowSpecFactory : IWorkflowSpecFactory
{
    private readonly ITemplateFactory _templateFactory;

    public WorkflowSpecFactory(ITemplateFactory templateFactory)
    {
        _templateFactory = templateFactory;
    }

    public Result<JsonElement> Create(
        string signalGeneratorExperimentRunId,
        IReadOnlyList<AlgorithmStep> steps,
        DateOnly start,
        DateOnly end,
        int numberOfWeeksHistoricalData,
        int numberOfCpuCoresRequested,
        string? tickersRaw,
        string? tickersPreset,
        IReadOnlyList<KeyValuePair<string, string>> environmentVariables,
        string configMapName
    )
    {
        var dateRange = new DateInterval(start, end, DateIntervalType.Weekly);

        var workflow = new WorkflowSpec
        {
            Entrypoint = "loop-params",
            PodGC = new PodGC
            {
                Strategy = "OnWorkflowSuccess",
                DeleteDelayDuration = "30m"
            },
            Arguments = new Arguments
            {
                Parameters =
                [
                    new Parameter
                    {
                        Name = "date-list",
                        Value = new JsonArray(
                                dateRange
                                    .Select(d => new JsonObject
                                        {
                                            ["previousWeekEndDate"] = d.ToString("d")
                                        }
                                    )
                                    .ToArray<JsonNode?>()
                            )
                            .ToJsonString()
                    }
                ]
            },
            Templates =
            [
                _templateFactory.CreateFinalizeTemplate(signalGeneratorExperimentRunId),
                _templateFactory.CreateLoopTemplate(),
                _templateFactory.CreatePlanTemplate(steps),
                .._templateFactory.CreateStepTemplates(
                    steps,
                    environmentVariables,
                    tickersRaw,
                    tickersPreset,
                    numberOfWeeksHistoricalData,
                    numberOfCpuCoresRequested,
                    configMapName
                )
            ]
        };

        return JsonSerializer.SerializeToElement(
            workflow,
            new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            }
        );
    }
}