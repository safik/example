using System.Text.Json;
using System.Text.Json.Serialization;
using k8s;
using k8s.Models;

namespace Orchestrator.Infrastructure.Kubernetes.Workflows;

public abstract class CustomResource : KubernetesObject, IMetadata<V1ObjectMeta>
{
    [JsonPropertyName("metadata")]
    public V1ObjectMeta Metadata { get; set; }
}

public class WorkflowV1 : CustomResource
{
    [JsonPropertyName("spec")]
    public JsonElement Spec { get; set; }
        
    [JsonPropertyName("status")]
    public WorkflowV1Status? Status { get; set; }
}

public class WorkflowV1Status
{
    [JsonPropertyName("nodes")]
    public JsonElement Nodes { get; set; }
    
    [JsonPropertyName("phase")]
    public string Phase { get; set; }
}