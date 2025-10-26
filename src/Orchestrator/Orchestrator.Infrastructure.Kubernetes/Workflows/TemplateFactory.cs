using Orchestrator.Infrastructure.RisingWave.FluentMigrator;

namespace Orchestrator.Infrastructure.Kubernetes.Workflows;

public interface ITemplateFactory
{
    Template CreateLoopTemplate();
    Template CreatePlanTemplate(IReadOnlyList<AlgorithmStep> algorithmSteps);
    
    IReadOnlyList<Template> CreateStepTemplates(
        IReadOnlyList<AlgorithmStep> algorithmSteps,
        IReadOnlyList<KeyValuePair<string, string>> environmentVariables,
        string? tickersRaw,
        string? tickersPreset,
        int numberOfWeeksHistoricalData,
        int numberOfCpuCoresRequested,
        string configMapName
    );
    
    Template CreateFinalizeTemplate(string signalGeneratorExperimentRunId);
}

public class TemplateFactory : ITemplateFactory
{
    public Template CreateFinalizeTemplate(string signalGeneratorExperimentRunId)
    {
        return new Template
        {
            Name = TemplateNames.Finalize,
            Container = new Container
            {
                Image = "postgres:15",
                Command = ["sh", "-c"],
                Env = [
                    new Env{Name = "PGHOST", Value = "risingwave.risingwave.svc.cluster.local"},
                    new Env{Name = "PGPORT", Value = "4567"},
                    new Env{Name = "PGDATABASE", Value = "dev"},
                    new Env{Name = "PGUSER", Value = "root"},
                    new Env{Name = "PGPASSWORD", Value = "root"},
                ],
                Args = [
                    $"psql -c \""
                    + $"UPDATE {DbConsts.Orchestration.Schema}.{DbConsts.Orchestration.Tables.SignalGeneratorExperimentRuns} "
                    + $"SET status = 'Finished' "
                    + $"where id = '{signalGeneratorExperimentRunId}'\""
                ]
            }
        };
    }
    
    public Template CreateLoopTemplate()
    {
        return new Template
        {
            Name = "loop-params",
            Parallelism = 1,
            Inputs = new Arguments
            {
                Parameters =
                [
                    new Parameter
                    {
                        Name = "date-list"
                    }
                ]
            },
            Steps =
            [
                [
                    new Step
                    {
                        Name = "plan",
                        Template = "plan",
                        Arguments = new Arguments
                        {
                            Parameters =
                            [
                                new Parameter
                                {
                                    Name = "previousWeekEndDate",
                                    Value = "{{item.previousWeekEndDate}}"
                                },
                            ]
                        },
                        WithParam = "{{inputs.parameters.date-list}}"
                    },
                    new Step
                    {
                        Name = TemplateNames.Finalize,
                        Template = TemplateNames.Finalize
                    }
                ]
            ]
        };
    }

    public Template CreatePlanTemplate(IReadOnlyList<AlgorithmStep> algorithmSteps)
    {
        var steps = algorithmSteps
            .Select(
                a => new[]
                {
                    new Step
                    {
                        Arguments = new Arguments
                        {
                            Parameters =
                            [
                                new Parameter
                                {
                                    Name = "previousWeekEndDate",
                                    Value = "{{inputs.parameters.previousWeekEndDate}}"
                                }
                            ]
                        },
                        Name = a.Name,
                        Template = a.Name
                    }
                }
            )
            .ToArray();

        return new Template
        {
            Name = "plan",
            Parallelism = 1,
            Inputs = new Arguments
            {
                Parameters =
                [
                    new Parameter
                    {
                        Name = "previousWeekEndDate"
                    }
                ]
            },
            Steps = steps

        };
    }

    public IReadOnlyList<Template> CreateStepTemplates(
        IReadOnlyList<AlgorithmStep> algorithmSteps,
        IReadOnlyList<KeyValuePair<string, string>> environmentVariables,
        string? tickersRaw,
        string? tickersPreset,
        int numberOfWeeksHistoricalData,
        int numberOfCpuCoresRequested,
        string configMapName
    )
    {
        return algorithmSteps
            .Select(a => new Template
                {
                    Name = a.Name,
                    Volumes = new List<Volume>
                    {
                        new()
                        {
                            Name = KubernetesWorkflowConstants.TrialsConfigMapVolumeName,
                            ConfigMap = new ConfigMap
                            {
                                Name = configMapName
                            }
                        }
                    },
                    Container = new Container
                    {
                        Image = $"{a.Image}:{a.Version ?? "latest"}",
                        VolumeMounts = new List<VolumeMount>
                        {
                            new()
                            {
                                Name = KubernetesWorkflowConstants.TrialsConfigMapVolumeName,
                                MountPath = KubernetesWorkflowConstants.TrialsConfigMapMountPath,
                                ReadOnly = true
                            }
                        },
                        Env =
                        [
                            new Env
                            {
                                Name = "Experiment__PreviousPeriodEndDateLocal",
                                Value = "{{inputs.parameters.previousWeekEndDate}}"
                            },
                            new Env
                            {
                                Name = "Experiment__TickersRaw",
                                Value = tickersRaw
                            },
                            new Env
                            {
                                Name = "Experiment__TickersPreset",
                                Value = tickersPreset
                            },
                            new Env
                            {
                                Name = "Experiment__Training__NumberOfWeeksHistoricalData",
                                Value = numberOfWeeksHistoricalData.ToString()
                            },
                            new Env
                            {
                                Name = "TRIALS_CONFIG_LOCATION",
                                Value
                                    = $"{KubernetesWorkflowConstants.TrialsConfigMapMountPath}"
                                      + $"/{KubernetesWorkflowConstants.TrialsConfigMapFileName}"
                            },
                            ..environmentVariables.Select(ev => new Env
                                {
                                    Name = ev.Key,
                                    Value = ev.Value
                                }
                            )
                        ],
                        Command = a.Commands?.ToList(),
                        Args = a.Args?.ToList(),
                        Resources = new Resources
                        {
                            Requests = new Limits
                            {
                                Cpu = numberOfCpuCoresRequested.ToString()
                            }
                        }
                    },
                    Inputs = new Arguments
                    {
                        Parameters =
                        [
                            new Parameter
                            {
                                Name = "previousWeekEndDate"
                            }
                        ]
                    }
                }
            )
            .ToList();
    }
}