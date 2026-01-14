using BioLens.Domain.Entities;
using BioLens.Domain.ValueObjects;
using Microsoft.SemanticKernel;

namespace BioLens.Agents.Core;

/// <summary>
/// Coordinator agent that orchestrates the diagnostic workflow
/// Uses Chain of Thought and ReAct pattern
/// </summary>
public class DiagnosticCoordinatorAgent : BioLensAgent
{
    private readonly ImageAnalysisAgent _imageAgent;
    private readonly AudioTranscriptionAgent _audioAgent;
    private readonly MedicalReasoningAgent _reasoningAgent;
    private readonly TreatmentPlannerAgent _treatmentAgent;

    public DiagnosticCoordinatorAgent(
        Kernel kernel,
        ImageAnalysisAgent imageAgent,
        AudioTranscriptionAgent audioAgent,
        MedicalReasoningAgent reasoningAgent,
        TreatmentPlannerAgent treatmentAgent)
        : base(kernel, "DiagnosticCoordinator", "Orchestrates multi-agent diagnostic workflow")
    {
        _imageAgent = imageAgent;
        _audioAgent = audioAgent;
        _reasoningAgent = reasoningAgent;
        _treatmentAgent = treatmentAgent;
    }

    public override async Task<AgentResponse> ExecuteAsync(
        AgentRequest request,
        CancellationToken cancellationToken = default)
    {
        var diagnosticCase = (DiagnosticCase)request.Parameters["case"];
        var messages = new List<string>();
        
        try
        {
            // Step 1: Analyze images
            messages.Add("🔍 Analyzing medical images...");
            var imageAnalysis = await _imageAgent.ExecuteAsync(
                new AgentRequest(
                    request.RequestId,
                    "AnalyzeImages",
                    new Dictionary<string, object> { ["images"] = diagnosticCase.Images },
                    request.Context),
                cancellationToken);

            // Step 2: Transcribe and analyze audio
            messages.Add("🎤 Processing audio symptoms...");
            var audioAnalysis = await _audioAgent.ExecuteAsync(
                new AgentRequest(
                    request.RequestId,
                    "TranscribeAudio",
                    new Dictionary<string, object> { ["audio"] = diagnosticCase.AudioDescription! },
                    request.Context),
                cancellationToken);

            // Step 3: Medical reasoning and differential diagnosis
            messages.Add("🧠 Generating differential diagnosis...");
            var diagnosis = await _reasoningAgent.ExecuteAsync(
                new AgentRequest(
                    request.RequestId,
                    "GenerateDiagnosis",
                    new Dictionary<string, object>
                    {
                        ["imageFindings"] = imageAnalysis.Result!,
                        ["audioFindings"] = audioAnalysis.Result!,
                        ["patient"] = diagnosticCase.Patient,
                        ["context"] = diagnosticCase.Context
                    },
                    request.Context),
                cancellationToken);

            // Step 4: Generate treatment protocol
            messages.Add("💊 Creating treatment protocol...");
            var treatment = await _treatmentAgent.ExecuteAsync(
                new AgentRequest(
                    request.RequestId,
                    "CreateTreatmentPlan",
                    new Dictionary<string, object>
                    {
                        ["diagnosis"] = diagnosis.Result!,
                        ["context"] = diagnosticCase.Context
                    },
                    request.Context),
                cancellationToken);

            messages.Add("✅ Diagnostic workflow completed");

            return new AgentResponse(
                request.RequestId,
                true,
                new
                {
                    Diagnosis = diagnosis.Result,
                    Treatment = treatment.Result
                },
                messages,
                new Dictionary<string, object>
                {
                    ["completedAt"] = DateTimeOffset.UtcNow,
                    ["agentsInvolved"] = new[] { "Image", "Audio", "Reasoning", "Treatment" }
                });
        }
        catch (Exception ex)
        {
            messages.Add($"❌ Error: {ex.Message}");
            return new AgentResponse(
                request.RequestId,
                false,
                null,
                messages,
                new Dictionary<string, object> { ["error"] = ex.ToString() });
        }
    }
}
