using BioLens.Domain.Entities;
using BioLens.Domain.ValueObjects;
using BioLens.Domain.Enums;
using Microsoft.SemanticKernel;
using System.Text.Json;

namespace BioLens.Agents.Core;

/// <summary>
/// Core reasoning agent that generates differential diagnoses
/// Uses Chain-of-Thought reasoning
/// </summary>
public class MedicalReasoningAgent : BioLensAgent
{
    public MedicalReasoningAgent(Kernel kernel)
        : base(kernel, "MedicalReasoner", "Generates differential diagnoses using clinical reasoning")
    {
    }

    public override async Task<AgentResponse> ExecuteAsync(
        AgentRequest request,
        CancellationToken cancellationToken = default)
    {
        var imageFindings = request.Parameters["imageFindings"];
        var audioFindings = request.Parameters["audioFindings"];
        var patient = (Patient)request.Parameters["patient"];
        var context = (ContextualInformation)request.Parameters["context"];
        
        var prompt = BuildDiagnosticPrompt(imageFindings, audioFindings, patient, context);
        var diagnosisJson = await InvokePromptAsync(prompt, cancellationToken);
        
        var diagnosis = ParseDiagnosis(diagnosisJson);
        
        return new AgentResponse(
            request.RequestId,
            true,
            diagnosis,
            new List<string> { "Differential diagnosis generated" },
            new Dictionary<string, object>
            {
                ["reasoningApproach"] = "Chain-of-Thought",
                ["contextConsidered"] = true
            });
    }

    private string BuildDiagnosticPrompt(
        object imageFindings,
        object audioFindings,
        Patient patient,
        ContextualInformation context)
    {
        return $@"
You are an expert diagnostic physician. Use step-by-step clinical reasoning.

PATIENT INFORMATION:
- Age: {patient.AgeYears} {patient.AgeUnit}
- Sex: {patient.Sex}
- Known conditions: {JsonSerializer.Serialize(patient.MedicalHistory)}
- Known allergies: {JsonSerializer.Serialize(patient.KnownAllergies)}

GEOGRAPHIC CONTEXT:
- Region: {context.Region.Country}, {context.Region.Region}
- Endemic diseases: {string.Join(", ", context.LocalEndemicDiseases)}
- Facility level: {context.FacilityLevel}

IMAGE FINDINGS:
{JsonSerializer.Serialize(imageFindings)}

AUDIO FINDINGS (Symptoms):
{JsonSerializer.Serialize(audioFindings)}

REASONING PROCESS:
1. List all clinical findings
2. Group findings by system (dermatologic, respiratory, etc.)
3. Consider differential diagnoses (at least 3-5)
4. Apply clinical decision rules
5. Factor in endemic diseases and local epidemiology
6. Assign probability and urgency to each diagnosis

Return as JSON:
{{
  ""reasoningSteps"": ["".....""],
  ""primaryDiagnosis"": {{
    ""conditionName"": ""....."",
    ""icd10Code"": ""....."",
    ""confidence"": ""High|Medium|Low"",
    ""supportingEvidence"": ["".....""],
    ""warningFlags"": ["".....""],
    ""urgency"": ""Routine|Urgent|Emergency|Critical""
  }},
  ""alternativeDiagnoses"": [
    {{
      ""conditionName"": ""....."",
      ""icd10Code"": ""....."",
      ""confidence"": ""High|Medium|Low"",
      ""supportingEvidence"": ["".....""],
      ""warningFlags"": ["".....""],
      ""urgency"": ""Routine|Urgent|Emergency|Critical""
    }}
  ]
}}

CRITICAL: Base diagnosis on evidence. If uncertain, indicate lower confidence.
";
    }

    private object ParseDiagnosis(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<object>(json) ?? new { };
        }
        catch
        {
            return new { error = "Failed to parse diagnosis", raw = json };
        }
    }
}
