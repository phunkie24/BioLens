using BioLens.Domain.ValueObjects;
using Microsoft.SemanticKernel;

namespace BioLens.Agents.Core;

/// <summary>
/// Agent for transcribing and analyzing audio symptom descriptions
/// </summary>
public class AudioTranscriptionAgent : BioLensAgent
{
    public AudioTranscriptionAgent(Kernel kernel)
        : base(kernel, "AudioTranscriber", "Transcribes and extracts symptoms from audio")
    {
    }

    public override async Task<AgentResponse> ExecuteAsync(
        AgentRequest request,
        CancellationToken cancellationToken = default)
    {
        var audio = (AudioSymptomDescription)request.Parameters["audio"];
        
        var prompt = $@"
You are a medical scribe. The healthcare worker has recorded audio describing patient symptoms.

AUDIO DETAILS:
- Language: {audio.LanguageCode}
- Duration: {audio.DurationSeconds} seconds
- Transcribed text: {audio.TranscribedText ?? "[Not yet transcribed]"}

TASK:
1. Extract all mentioned symptoms
2. Note duration, severity, and progression
3. Identify critical information (onset time, triggers, etc.)
4. Flag any emergency indicators

Return as JSON:
{{
  ""symptoms"": [
    {{
      ""symptom"": ""....."",
      ""severity"": ""Mild|Moderate|Severe"",
      ""duration"": ""....."",
      ""onset"": "".....""
    }}
  ],
  ""emergencyFlags"": ["".....""],
  ""additionalInfo"": ""....."
}}
";
        
        var result = await InvokePromptAsync(prompt, cancellationToken);
        
        return new AgentResponse(
            request.RequestId,
            true,
            result,
            new List<string> { "Audio analyzed successfully" },
            new Dictionary<string, object> { ["language"] = audio.LanguageCode });
    }
}
