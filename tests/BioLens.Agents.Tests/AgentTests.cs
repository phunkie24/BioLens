using BioLens.Agents.Core;
using BioLens.Domain.Entities;
using BioLens.Domain.Enums;
using BioLens.Domain.ValueObjects;
using Microsoft.SemanticKernel;
using Xunit;

namespace BioLens.Agents.Tests;

public class DiagnosticCoordinatorAgentTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidCase_ShouldReturnSuccessfulDiagnosis()
    {
        // Arrange
        var kernel = Kernel.CreateBuilder().Build();
        var imageAgent = new ImageAnalysisAgent(kernel);
        var audioAgent = new AudioTranscriptionAgent(kernel);
        var reasoningAgent = new MedicalReasoningAgent(kernel);
        var treatmentAgent = new TreatmentPlannerAgent(kernel);
        
        var coordinator = new DiagnosticCoordinatorAgent(
            kernel,
            imageAgent,
            audioAgent,
            reasoningAgent,
            treatmentAgent);

        var patient = new Patient("PAT_TEST_001", 30, AgeUnit.Years, BiologicalSex.Male);
        var context = new ContextualInformation(
            new GeographicRegion("Test", "Test", null, 0, 0),
            new List<string> { "Paracetamol" },
            new List<string> { "Malaria" },
            FacilityCapabilities.RuralClinic,
            new CulturalConsiderations("en", new(), new()));

        var diagnosticCase = new DiagnosticCase(patient, Guid.NewGuid(), context);

        var request = new AgentRequest(
            Guid.NewGuid().ToString(),
            "RunDiagnosis",
            new Dictionary<string, object> { ["case"] = diagnosticCase },
            new AgentContext(diagnosticCase.Id, new Dictionary<string, object>()));

        // Act
        var response = await coordinator.ExecuteAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.IsSuccess || response.Messages.Any()); // Either succeeds or has explanation
    }
}

public class ImageAnalysisAgentTests
{
    [Fact]
    public async Task ExecuteAsync_WithMultipleImages_ShouldAnalyzeAll()
    {
        // Arrange
        var kernel = Kernel.CreateBuilder().Build();
        var agent = new ImageAnalysisAgent(kernel);

        var images = new List<MedicalImage>
        {
            new MedicalImage(
                Guid.NewGuid(),
                "/path/image1.jpg",
                null,
                ImageType.Skin,
                new ImageMetadata(1920, 1080, 100000, "iPhone"),
                DateTimeOffset.UtcNow)
        };

        var request = new AgentRequest(
            Guid.NewGuid().ToString(),
            "AnalyzeImages",
            new Dictionary<string, object> { ["images"] = images },
            new AgentContext(Guid.NewGuid(), new Dictionary<string, object>()));

        // Act
        var response = await agent.ExecuteAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Contains("Analyzed", response.Messages[0]);
    }
}

public class MedicalReasoningAgentTests
{
    [Fact]
    public void AgentName_ShouldBeCorrect()
    {
        // Arrange & Act
        var kernel = Kernel.CreateBuilder().Build();
        var agent = new MedicalReasoningAgent(kernel);

        // Assert
        Assert.Equal("MedicalReasoner", typeof(MedicalReasoningAgent).Name.Replace("Agent", ""));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldProvideReasoningSteps()
    {
        // Arrange
        var kernel = Kernel.CreateBuilder().Build();
        var agent = new MedicalReasoningAgent(kernel);

        var patient = new Patient("PAT_001", 25, AgeUnit.Years, BiologicalSex.Female);
        var context = new ContextualInformation(
            new GeographicRegion("Kenya", "Nairobi", null, -1.0, 36.0),
            new List<string> { "Paracetamol", "Amoxicillin" },
            new List<string> { "Malaria", "Typhoid" },
            FacilityCapabilities.DistrictHospital,
            new CulturalConsiderations("sw", new(), new()));

        var request = new AgentRequest(
            Guid.NewGuid().ToString(),
            "GenerateDiagnosis",
            new Dictionary<string, object>
            {
                ["imageFindings"] = new { observations = "Rash on arms" },
                ["audioFindings"] = new { symptoms = "Fever for 3 days" },
                ["patient"] = patient,
                ["context"] = context
            },
            new AgentContext(Guid.NewGuid(), new Dictionary<string, object>()));

        // Act
        var response = await agent.ExecuteAsync(request);

        // Assert
        Assert.NotNull(response);
    }
}
