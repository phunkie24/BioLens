using BioLens.Domain.Entities;
using BioLens.Domain.Enums;
using BioLens.Domain.ValueObjects;
using Xunit;

namespace BioLens.Domain.Tests;

public class DiagnosticCaseTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithCorrectStatus()
    {
        // Arrange
        var patient = new Patient("PAT_001", 30, AgeUnit.Years, BiologicalSex.Male);
        var context = CreateTestContext();

        // Act
        var diagnosticCase = new DiagnosticCase(patient, Guid.NewGuid(), context);

        // Assert
        Assert.Equal(CaseStatus.Created, diagnosticCase.Status);
        Assert.NotEqual(Guid.Empty, diagnosticCase.Id);
        Assert.False(diagnosticCase.IsSyncedToCloud);
    }

    [Fact]
    public void AddMedicalImage_WhenUnder10Images_ShouldSucceed()
    {
        // Arrange
        var diagnosticCase = CreateTestCase();
        var image = new MedicalImage(
            Guid.NewGuid(),
            "/test.jpg",
            null,
            ImageType.Skin,
            new ImageMetadata(1920, 1080, 100000, "Test"),
            DateTimeOffset.UtcNow);

        // Act
        var result = diagnosticCase.AddMedicalImage(image);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(diagnosticCase.Images);
    }

    [Fact]
    public void AddMedicalImage_WhenExceeds10_ShouldFail()
    {
        // Arrange
        var diagnosticCase = CreateTestCase();
        
        for (int i = 0; i < 10; i++)
        {
            diagnosticCase.AddMedicalImage(new MedicalImage(
                Guid.NewGuid(), $"/img{i}.jpg", null, ImageType.Skin,
                new ImageMetadata(100, 100, 1000, "Test"), DateTimeOffset.UtcNow));
        }

        var extraImage = new MedicalImage(
            Guid.NewGuid(), "/extra.jpg", null, ImageType.Skin,
            new ImageMetadata(100, 100, 1000, "Test"), DateTimeOffset.UtcNow);

        // Act
        var result = diagnosticCase.AddMedicalImage(extraImage);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Maximum 10 images", result.Error);
    }

    [Fact]
    public void CompleteDiagnosis_WhenCaseInProgress_ShouldSucceed()
    {
        // Arrange
        var diagnosticCase = CreateTestCase();
        diagnosticCase.StartDiagnosis();

        var primaryDiagnosis = new DifferentialDiagnosis(
            "Malaria",
            "B50.9",
            ConfidenceLevel.High,
            new List<string> { "Fever", "Chills" },
            new List<string>(),
            UrgencyLevel.Urgent);

        var treatment = new TreatmentProtocol(
            "Malaria Treatment",
            new List<TreatmentStep>(),
            new List<MedicationRecommendation>(),
            new List<string>(),
            new FollowUpGuidance(new(), new(), 7),
            new EmergencyEscalation(new(), UrgencyLevel.Emergency, "Hospital"));

        // Act
        var result = diagnosticCase.CompleteDiagnosis(
            primaryDiagnosis,
            new List<DifferentialDiagnosis>(),
            treatment);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(CaseStatus.DiagnosisCompleted, diagnosticCase.Status);
        Assert.NotNull(diagnosticCase.PrimaryDiagnosis);
    }

    [Fact]
    public void DomainEvents_ShouldBeTracked()
    {
        // Arrange & Act
        var patient = new Patient("PAT_001", 30, AgeUnit.Years, BiologicalSex.Male);
        var context = CreateTestContext();
        var diagnosticCase = new DiagnosticCase(patient, Guid.NewGuid(), context);

        // Assert
        Assert.NotEmpty(diagnosticCase.DomainEvents);
        Assert.Single(diagnosticCase.DomainEvents);
    }

    private DiagnosticCase CreateTestCase()
    {
        var patient = new Patient("TEST_PAT", 30, AgeUnit.Years, BiologicalSex.Male);
        var context = CreateTestContext();
        return new DiagnosticCase(patient, Guid.NewGuid(), context);
    }

    private ContextualInformation CreateTestContext()
    {
        return new ContextualInformation(
            new GeographicRegion("Test", "Test", null, 0, 0),
            new List<string> { "Paracetamol" },
            new List<string> { "Malaria" },
            FacilityCapabilities.RuralClinic,
            new CulturalConsiderations("en", new(), new()));
    }
}

public class PatientTests
{
    [Fact]
    public void Constructor_ShouldCreateValidPatient()
    {
        // Act
        var patient = new Patient("PAT_001", 35, AgeUnit.Years, BiologicalSex.Female);

        // Assert
        Assert.Equal("PAT_001", patient.AnonymizedId);
        Assert.Equal(35, patient.AgeYears);
        Assert.Equal(AgeUnit.Years, patient.AgeUnit);
        Assert.Equal(BiologicalSex.Female, patient.Sex);
    }

    [Fact]
    public void AddMedicalCondition_ShouldAddToHistory()
    {
        // Arrange
        var patient = new Patient("PAT_001", 35, AgeUnit.Years, BiologicalSex.Female);
        var condition = new KnownCondition(
            "Diabetes Type 2",
            "E11.9",
            DateTimeOffset.UtcNow.AddYears(-2),
            true);

        // Act
        patient.AddMedicalCondition(condition);

        // Assert
        Assert.Single(patient.MedicalHistory);
    }
}
