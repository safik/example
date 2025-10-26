using Dapper;
using k8s;
using Npgsql;
using Orchestrator.Application;
using Orchestrator.Infrastructure;
using Orchestrator.Infrastructure.Kubernetes;
using Orchestrator.Infrastructure.Kubernetes.Workflows;
using Orchestrator.Infrastructure.RisingWave.FluentMigrator;
using Orchestrator.Service.Infrastructure;

namespace Orchestrator.Service;

public class Program
{
    public static void Main(string[] args)
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton(
            _ => NpgsqlDataSource.Create(
                builder.Configuration.GetConnectionString("Default") ?? throw new InvalidOperationException()
            )
        );

        builder.Services.AddScoped(sp =>
            {
                var ds = sp.GetRequiredService<NpgsqlDataSource>();
                return ds.OpenConnection(); 
            }
        );

        builder.Services.AddControllers();
        builder.Services.AddHostedService<RisingWaveMigrator>();
        builder.Services.AddHostedService<SignalGeneratorExperimentRunEventsHandler>();
        builder.Services.AddSingleton<ISchemaMigrator, SchemaMigrator>();
        builder.Services.AddSingleton<IExperimentRunProvider, ExperimentRunProvider>();
        builder.Services.AddScoped<IInitSignalGeneratorExperimentRunHandler, InitSignalGeneratorExperimentRunHandler>();
        builder.Services.AddScoped<ISignalGeneratorExperimentRunRepository, SignalGeneratorExperimentRunRepository>();
        builder.Services.AddScoped<IStartSignalGeneratorExperimentRunHandler, StartSignalGeneratorExperimentRunRunHandler>();
        builder.Services.AddSingleton<IWorkflowSpecFactory, WorkflowSpecFactory>();
        builder.Services.AddSingleton<ITemplateFactory, TemplateFactory>();
        builder.Services.AddSingleton<IInfrastructureEnvVarsProvider, InfrastructureEnvVarsProvider>();
        builder.Services.AddSingleton<IKubernetesClient, KubernetesClient>();
        builder.Services.AddScoped<IAlgorithmStepsProvider, AlgorithmStepsProvider>();
        builder.Services.AddScoped<ITrialsConfigMapFactory, TrialsConfigMapFactory>();
        builder.Services.AddInfrastructure(builder.Configuration);

        builder.Services.Configure<SignalGeneratorEnvVarsOptions>(builder.Configuration.GetSection(SignalGeneratorEnvVarsOptions.Section));
        
        builder.Services.AddSingleton<IKubernetes>(
            _ =>
            {
                var config = new KubernetesClientConfiguration()
                {
                    ClientCertificateFilePath = "user1.crt",
                    ClientKeyFilePath = "user1.key",
                    SkipTlsVerify = true,
                    Host = "https://192.168.0.201:6443"
                };

                return new Kubernetes(config);
            }
        );

        builder.Services.AddSingleton(
            typeof(CancellationToken),
            sp => sp.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping
        );
        
        var app = builder.Build();

        app.MapControllers();

        app.Run();
    }
    
}