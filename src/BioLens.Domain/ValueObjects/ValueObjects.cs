using BioLens.Domain.Common;
using BioLens.Domain.Enums;

namespace BioLens.Domain.ValueObjects;

public record MedicalImage(
    Guid Id,
    string LocalFilePath,
    string? CloudBlobUrl,
    ImageType Type,
    ImageMetadata Metadata,
    DateTimeOffset CapturedAt);

public record ImageMetadata(
    int Width,
    int Height,
    long FileSizeBytes,
    string DeviceModel);

public record AudioSymptomDescription(
    Guid Id,
    string LocalFilePath,
    string? CloudBlobUrl,
    string LanguageCode,
    int DurationSeconds,
    string? TranscribedText,
    DateTimeOffset RecordedAt);

public record DifferentialDiagnosis(
    string ConditionName,
    string ICD10Code,
    ConfidenceLevel Confidence,
    List<string> SupportingEvidence,
    List<string> WarningFlags,
    UrgencyLevel Urgency);

public record TreatmentStep(
    int StepNumber,
    string Instruction,
    int DurationMinutes,
    List<string> RequiredMaterials);

public record MedicationRecommendation(
    string MedicationName,
    string Dosage,
    string Frequency,
    int DurationDays,
    List<string> Contraindications);

public record FollowUpGuidance(
    List<string> ImprovementSigns,
    List<string> WorseningSigns,
    int FollowUpDays);

public record EmergencyEscalation(
    List<string> EscalationCriteria,
    UrgencyLevel EscalationUrgency,
    string RecommendedFacility);

public record TreatmentProtocol(
    string ProtocolName,
    List<TreatmentStep> Steps,
    List<MedicationRecommendation> Medications,
    List<string> Contraindications,
    FollowUpGuidance FollowUp,
    EmergencyEscalation EscalationCriteria);

public record GeographicRegion(
    string Country,
    string Region,
    string? District,
    double Latitude,
    double Longitude);

public record CulturalConsiderations(
    string PrimaryLanguage,
    List<string> CommonBeliefs,
    List<string> TreatmentPreferences);

public record ContextualInformation(
    GeographicRegion Region,
    List<string> AvailableMedications,
    List<string> LocalEndemicDiseases,
    FacilityCapabilities FacilityLevel,
    CulturalConsiderations CulturalContext);

public record KnownCondition(
    string ConditionName,
    string ICD10Code,
    DateTimeOffset DiagnosedDate,
    bool IsActive);

public record Allergy(
    string AllergenName,
    string Severity,
    string Reaction);
