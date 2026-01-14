using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace BioLens.Agents.Core;

/// <summary>
/// Base class for all BioLens agents
/// </summary>
public abstract class BioLensAgent
{
    protected readonly Kernel Kernel;
    protected readonly string AgentName;
    protected readonly string AgentDescription;

    protected BioLensAgent(Kernel kernel, string name, string description)
    {
        Kernel = kernel;
        AgentName = name;
        AgentDescription = description;
    }

    public abstract Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default);
    
    protected async Task<string> InvokePromptAsync(string prompt, CancellationToken cancellationToken)
    {
        var result = await Kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
        return result.ToString();
    }
}

/// <summary>
/// Agent request wrapper
/// </summary>
public record AgentRequest(
    string RequestId,
    string RequestType,
    Dictionary<string, object> Parameters,
    AgentContext Context);

/// <summary>
/// Agent response wrapper
/// </summary>
public record AgentResponse(
    string RequestId,
    bool IsSuccess,
    object? Result,
    List<string> Messages,
    Dictionary<string, object> Metadata);

/// <summary>
/// Context shared across agents
/// </summary>
public record AgentContext(
    Guid CaseId,
    Dictionary<string, object> SharedMemory);
