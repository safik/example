using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HyperparameterOptimization.Client;

public class HyperparameterOptimizationClient
{
    private readonly HttpClient _httpClient;

    public HyperparameterOptimizationClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CreateTrialResultContract> GenerateHyperparameters(
        string study,
        string trialId,
        JsonElement hyperparameterSpace,
        CancellationToken cancellationToken
    )
    {
        var result = await _httpClient.PostAsJsonAsync(
            $"studies/{study}/trials/{trialId}",
            hyperparameterSpace,
            cancellationToken
        );

        result.EnsureSuccessStatusCode();

        return await result.Content.ReadFromJsonAsync<CreateTrialResultContract>(cancellationToken)
               ?? throw new InvalidOperationException();
    }
}

public record CreateTrialResultContract(string TrialId, JsonObject Hyperparameters);
