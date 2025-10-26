using System.Net;
using System.Runtime.CompilerServices;
using CSharpFunctionalExtensions;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.Logging;
using static System.Threading.Tasks.ConfigureAwaitOptions;

namespace Orchestrator.Infrastructure.Kubernetes.Workflows;

public interface IKubernetesClient
{
    Task<Result> DeleteWorkflow(WorkflowV1 workflow, CancellationToken cancellationToken);
    Task<Result<WorkflowV1>> CreateWorkflow(WorkflowV1? workflow, CancellationToken cancellationToken);
    Task<Result<bool>> CheckIfExists(string workflowName, CancellationToken cancellationToken);
    IAsyncEnumerable<WorkflowV1> WorkflowFinishEvents(CancellationToken cancellationToken);

    Task<Result<V1ConfigMap>> CreateConfigMapAsync(V1ConfigMap configMap, CancellationToken cancellationToken);
}

public class KubernetesClient : IKubernetesClient
{
    private readonly IKubernetes _kubernetes;
    private readonly ILogger<KubernetesClient> _logger;
    
    public KubernetesClient(IKubernetes kubernetes, ILogger<KubernetesClient> logger)
    {
        _kubernetes = kubernetes;
        _logger = logger;
    }

    public async Task<Result> DeleteWorkflow(WorkflowV1 workflow, CancellationToken cancellationToken)
    {
        try
        {
            await _kubernetes.CustomObjects.DeleteNamespacedCustomObjectWithHttpMessagesAsync(
                "argoproj.io",
                "v1alpha1",
                KubernetesWorkflowConstants.ArgoNamespace,
                "workflows",
                workflow.Metadata.Name,
                cancellationToken: cancellationToken
            );
        }
        catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(e, "Workflow not found: {workflow}. Skipping", workflow);
            return Result.Success();
        }
        catch(Exception e)
        {
            return Result.Failure(e.Message);
        }
        
        return Result.Success();
    }

    public async Task<Result<V1ConfigMap>> CreateConfigMapAsync(
        V1ConfigMap configMap,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await _kubernetes.CoreV1.CreateNamespacedConfigMapAsync(
                body: configMap,
                namespaceParameter: KubernetesWorkflowConstants.ArgoNamespace,
                cancellationToken: cancellationToken
            );
        }
        catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.Conflict)
        {
            _logger.LogWarning(
                "ConfigMap {name} already exists in {namespace}. Skipping.",
                configMap.Metadata?.Name,
                KubernetesWorkflowConstants.ArgoNamespace
            );
        }
        catch (Exception e)
        {
            return Result.Failure<V1ConfigMap>(e.Message);
        }

        return Result.Success(configMap);
    }

    public async Task<Result<WorkflowV1>> CreateWorkflow(WorkflowV1? workflow, CancellationToken cancellationToken)
    {
        if (workflow == null)
            return null;
        
        try
        {
            await _kubernetes.CustomObjects.CreateNamespacedCustomObjectAsync(
                workflow,
                "argoproj.io",
                "v1alpha1",
                KubernetesWorkflowConstants.ArgoNamespace,
                "workflows",
                cancellationToken: cancellationToken
            );
        }
        catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.Conflict)
        {
            _logger.LogWarning("Skip conflict for batch: {workflowCrd}", workflow);
        }
        catch (Exception e)
        {
            return Result.Failure<WorkflowV1>(e.Message);
        }

        return Result.Success(workflow);
    }

    public async Task<Result<bool>> CheckIfExists(string workflowName, CancellationToken cancellationToken)
    {
        try
        {
            await _kubernetes.CustomObjects.GetNamespacedCustomObjectAsync(
                "argoproj.io",
                "v1alpha1",
                KubernetesWorkflowConstants.ArgoNamespace,
                "workflows",
                workflowName,
                cancellationToken: cancellationToken
            );
        }
        catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception e)
        {
            return Result.Failure<bool>(e.Message);
        }
        
        return true;
    }
    
    public async IAsyncEnumerable<WorkflowV1> WorkflowFinishEvents([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            Task<HttpOperationResponse<object>>? workflowList = null;
            
            while (workflowList == null && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    workflowList = _kubernetes.CustomObjects.ListNamespacedCustomObjectWithHttpMessagesAsync(
                        "argoproj.io",
                        "v1alpha1",
                        KubernetesWorkflowConstants.ArgoNamespace,
                        "workflows",
                        watch: true,
                        cancellationToken: cancellationToken
                    );
                }
                catch (Exception e)
                {
                    _logger.LogWarning("Unable to get workflow events: {error}. Retrying after cooldown", e.Message);
                    await Task.Delay(10000, cancellationToken).ConfigureAwait(SuppressThrowing);
                }
            }

            var asyncEnumerable = workflowList
                .WatchAsync<WorkflowV1, object>(e => _logger.LogError(e, e.Message))
                .AsAsyncEnumerableSafe()
                .WithCancellation(cancellationToken);

            await foreach (var (eventType, item) in asyncEnumerable)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (eventType is not (WatchEventType.Added or WatchEventType.Modified))
                    continue;

                yield return item;
            }
        }
    }
}