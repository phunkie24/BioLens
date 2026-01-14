using BioLens.Domain.ValueObjects;
using Microsoft.SemanticKernel;
using System.Text.Json;

namespace BioLens.Agents.Core;

/// <summary>
/// Agent that creates context-aware treatment protocols
/// </summary>
public class TreatmentPlannerAgent : BioLensAgent
{
    public TreatmentPlannerAgent(Kernel kernel)
        : base(kernel, "TreatmentPlanner", "Creates resource-aware treatment protocols")
    {
    }

    public override async Task<AgentResponse> ExecuteAsync(
        AgentRequest request,
        CancellationToken cancellationToken = default)
    {
        var diagnosis = request.Parameters["diagnosis"];
        var context = (ContextualInformation)request.Parameters["context"];
        
        var prompt = BuildTreatmentPrompt(diagnosis, context);
        var treatmentJson = await InvokePromptAsync(prompt, cancellationToken);
        
        var treatment = ParseTreatment(treatmentJson);
        
        return new AgentResponse(
            request.RequestId,
            true,
            treatment,
            new List<string> { "Treatment protocol created" },
            new Dictionary<string, object>
            {
                ["availableMedications"] = context.AvailableMedications.Count,
                ["facilityLevel"] = context.FacilityLevel.ToString()
            });
    }

    private string BuildTreatmentPrompt(object diagnosis, ContextualInformation context)
    {
        return $@"
You are creating a treatment protocol for a resource-constrained setting.

DIAGNOSIS:
{JsonSerializer.Serialize(diagnosis)}

AVAILABLE RESOURCES:
- Medications: {string.Join(", ", context.AvailableMedications)}
- Facility: {context.FacilityLevel}
- Cultural context: {context.CulturalContext.PrimaryLanguage}

REQUIREMENTS:
1. Use ONLY available medications
2. Provide step-by-step treatment instructions
3. Include dosing appropriate for patient age/weight
4. List contraindications
5. Define follow-up criteria
6. Specify escalation triggers

Return as JSON:
{{
  ""protocolName"": ""....."",
  ""steps"": [
    {{
      ""stepNumber"": 1,
      ""instruction"": ""....."",
      ""durationMinutes"": 30,
      ""requiredMaterials"": ["".....""]
    }}
  ],
  ""medications"": [
    {{
      ""medicationName"": ""....."",
      ""dosage"": ""....."",
      ""frequency"": ""....."",
      ""durationDays"": 7,
      ""contraindications"": ["".....""]
    }}
  ],
  ""contraindications"": ["".....""],
  ""followUp"": {{
    ""improvementSigns"": ["".....""],
    ""worseningSigns"": ["".....""],
    ""followUpDays"": 3
  }},
  ""escalationCriteria"": {{
    ""escalationCriteria"": ["".....""],
    ""escalationUrgency"": ""Urgent|Emergency"",
    ""recommendedFacility"": ""....."
  }}
}}

CRITICAL: Patient safety first. Only recommend treatments appropriate for setting.
";
    }

    private object ParseTreatment(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<object>(json) ?? new { };
        }
        catch
        {
            return new { error = "Failed to parse treatment", raw = json };
        }
    }
}
