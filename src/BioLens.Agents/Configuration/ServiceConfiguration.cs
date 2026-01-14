using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using BioLens.Agents.Core;
using BioLens.Infrastructure.AI;

namespace BioLens.Agents.Configuration;

public static class AgentServiceCollectionExtensions
{
    public static IServiceCollection AddBioLensAgents(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure Semantic Kernel
        var kernel = Kernel.CreateBuilder()
            .AddGemini3ChatCompletion(
                configuration["Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini API Key not configured"))
            .Build();

        services.AddSingleton(kernel);

        // Register all agents
        services.AddScoped<ImageAnalysisAgent>();
        services.AddScoped<AudioTranscriptionAgent>();
        services.AddScoped<MedicalReasoningAgent>();
        services.AddScoped<TreatmentPlannerAgent>();
        services.AddScoped<DiagnosticCoordinatorAgent>();

        // Register Gemini service
        services.AddHttpClient<IGeminiAIService, GeminiAIService>();
        services.Configure<GeminiConfiguration>(configuration.GetSection("Gemini"));

        return services;
    }
}

public static class KernelBuilderExtensions
{
    public static IKernelBuilder AddGemini3ChatCompletion(
        this IKernelBuilder builder,
        string apiKey)
    {
        // For actual implementation, use the appropriate Semantic Kernel connector
        // This is a placeholder - in production you'd use:
        // builder.AddGoogleAIGeminiChatCompletion("gemini-3-pro", apiKey);
        
        return builder;
    }
}
