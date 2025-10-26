using System.Text.Json.Serialization;

namespace Orchestrator.Infrastructure.Kubernetes.Workflows;

public class WorkflowSpec
{
    [JsonPropertyName("entrypoint")]
    public string? Entrypoint { get; set; }
    
    [JsonPropertyName("arguments")]
    public Arguments? Arguments { get; set; }

    [JsonPropertyName("templates")]
    public ICollection<Template>? Templates { get; set; }
    
    [JsonPropertyName("podGC")]
    public PodGC? PodGC { get; set; }
}

public class Arguments
{
    [JsonPropertyName("parameters")]
    public ICollection<Parameter>? Parameters { get; set; }
}

public class Step
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("template")]
    public string? Template { get; set; }

    [JsonPropertyName("arguments")]
    public Arguments? Arguments { get; set; }
    
    [JsonPropertyName("withItems")]
    public ICollection<string>? WithItems { get; set; }
    
    [JsonPropertyName("withParam")]
    public string? WithParam { get; set; }
}

public class ValueFrom
{
    [JsonPropertyName("path")]
    public string? Path { get; set; }
}

public class Parameter
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("valueFrom")]
    public ValueFrom? ValueFrom { get; set; }
    
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

public class Container
{
    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("resources")]
    public Resources? Resources { get; set; }

    [JsonPropertyName("volumeMounts")]
    public ICollection<VolumeMount>? VolumeMounts { get; set; }

    [JsonPropertyName("env")]
    public ICollection<Env>? Env { get; set; }
    
    [JsonPropertyName("command")]
    public ICollection<string>? Command { get; set; }
    
    [JsonPropertyName("args")]
    public ICollection<string>? Args { get; set; }
}

public class Resources
{
    [JsonPropertyName("limits")]
    public Limits? Limits { get; set; }
    
    [JsonPropertyName("requests")]
    public Limits? Requests { get; set; }
}

public class Limits
{
    [JsonPropertyName("cpu")]
    public string? Cpu { get; set; }
}

public class VolumeMount
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("mountPath")]
    public string? MountPath { get; set; }

    [JsonPropertyName("readOnly")]
    public bool ReadOnly { get; set; }
}

public class Env
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
    
    [JsonPropertyName("valueFrom")]
    public ValueRef? ValueFrom { get; set; }
}

public class ValueRef
{
    [JsonPropertyName("configMapKeyRef")]
    public ConfigMapValueRef? ConfigMapKeyRef { get; set; }
};

public class ConfigMapValueRef
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("key")]
    public required string Key { get; set; }
}

public class EmptyDir
{
}

public class Volume
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("emptyDir")]
    public EmptyDir? EmptyDir { get; set; }
    
    [JsonPropertyName("configMap")]
    public ConfigMap? ConfigMap { get; set; }
}

public class ConfigMap
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class Script
{
    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("command")]
    public ICollection<string>? Command { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("env")]
    public ICollection<Env>? Env { get; set; }
}

public class Template
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("parallelism")]
    public int? Parallelism { get; set; }
    
    [JsonPropertyName("inputs")]
    public Arguments? Inputs { get; set; }
    
    [JsonPropertyName("steps")]
    public ICollection<ICollection<Step>>? Steps { get; set; }

    [JsonPropertyName("outputs")]
    public Arguments? Outputs { get; set; }

    [JsonPropertyName("container")]
    public Container? Container { get; set; }

    [JsonPropertyName("volumes")]
    public ICollection<Volume>? Volumes { get; set; }

    [JsonPropertyName("script")]
    public Script? Script { get; set; }
}

public class PodGC
{
    [JsonPropertyName("strategy")]
    public string? Strategy { get; set; }
    
    [JsonPropertyName("deleteDelayDuration")]
    public string? DeleteDelayDuration { get; set; }
}