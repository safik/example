namespace Orchestrator.Infrastructure.Kubernetes;

internal class KubernetesWorkflowConstants
{
    public const string ArgoNamespace = "argo";
    public const string TrialsConfigMapFileName = "trials.json";
    public const string TrialsConfigMapVolumeName = "trials-cm";
    public const string TrialsConfigMapMountPath = "/config/trials";
}