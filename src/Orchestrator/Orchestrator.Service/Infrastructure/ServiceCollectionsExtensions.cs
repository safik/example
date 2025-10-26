using HyperparameterOptimization.Client;
using NSubstitute;
using Orchestrator.Application;
using Orchestrator.Infrastructure;

namespace Orchestrator.Service.Infrastructure;

public static class ServiceCollectionsExtensions
{
    public static void AddInfrastructure(this IServiceCollection services, ConfigurationManager configuration)
    {
        
        var infrastructureSection = configuration
            .GetSection(InfrastructureOptions.Section);
        
        var infrastructureOptions = infrastructureSection
            .Get<InfrastructureOptions>();

        if (infrastructureOptions?.TrialsHPO?.Http?.Enabled == true)
        {
            services.AddTransient<ITrialsGenerator, TrialsGenerator>();
            services.AddScoped<IAlgorithmHyperparameterSpaceProvider, AlgorithmHyperparameterSpaceProvider>();
            
            services.AddHttpClient<HyperparameterOptimizationClient>(
                (_, client) =>
                {
                    client.BaseAddress = new Uri(infrastructureOptions.TrialsHPO.Http.Host);
                }
            );
        }
        else
        {
            services.AddTransient<ITrialsGenerator>(_ =>
                {
                    var providerStub = Substitute.For<ITrialsGenerator>();

                    providerStub
                        .GenerateAsync(
                            Arg.Any<string>(),
                            Arg.Any<string>(),
                            Arg.Any<ReadOnlyMemory<byte>>()
                        )
                        .Returns(_ => ReadOnlyMemory<byte>.Empty);

                    return providerStub;
                }
            );
        }

        
    }
}