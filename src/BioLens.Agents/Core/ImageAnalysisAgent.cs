using BioLens.Domain.ValueObjects;
using Microsoft.SemanticKernel;
using System.Text.Json;

namespace BioLens.Agents.Core;

/// <summary>
/// Specialized agent for analyzing medical images using Gemini 3 Vision
/// </summary>
public class ImageAnalysisAgent : BioLensAgent
{
    public ImageAnalysisAgent(Kernel kernel)
        : base(kernel, "ImageAnalyzer", "Analyzes medical images for diagnostic clues")
    {
    }

    public override async Task<AgentResponse> ExecuteAsync(
        AgentRequest request,
        CancellationToken cancellationToken = default)
    {
        var images = (IReadOnlyCollection<MedicalImage>)request.Parameters["images"];
        
        var prompt = BuildImageAnalysisPrompt(images);
        var analysisResult = await InvokePromptAsync(prompt, cancellationToken);
        
        var findings = ParseImageFindings(analysisResult);
        
        return new AgentResponse(
            request.RequestId,
            true,
            findings,
            new List<string> { $"Analyzed {images.Count} images" },
            new Dictionary<string, object>
            {
                ["imageCount"] = images.Count,
                ["rawResponse"] = analysisResult
            });
    }

    private string BuildImageAnalysisPrompt(IReadOnlyCollection<MedicalImage> images)
    {
        return $@"
You are an expert medical image analyst. Analyze the following {images.Count} medical images.

TASK:
1. Identify all visible symptoms, lesions, or abnormalities
2. Note color, texture, size, and location
3. Identify any warning signs or red flags
4. Suggest possible conditions (do NOT diagnose yet)

For each image, provide:
- Image type: {string.Join(", ", images.Select(i => i.Type))}
- Observed features
- Clinical significance
- Confidence level

Return as JSON:
{{
  ""findings"": [
    {{
      ""imageId"": ""guid"",
      ""observations"": ["".....""],
      ""suspectedConditions"": ["".....""],
      ""redFlags"": ["".....""],
      ""confidence"": ""High|Medium|Low""
    }}
  ],
  ""overallAssessment"": ""....""
}}
";
    }

    private object ParseImageFindings(string response)
    {
        try
        {
            return JsonSerializer.Deserialize<object>(response) ?? new { };
        }
        catch
        {
            return new { rawText = response };
        }
    }
}
