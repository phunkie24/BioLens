using BioLens.Domain.Entities;
using BioLens.Domain.ValueObjects;
using BioLens.Domain.Enums;
using MediatR;

namespace BioLens.Application.Commands;

public record CreateDiagnosticCaseCommand(
    string PatientAnonymizedId,
    int? PatientAge,
    AgeUnit PatientAgeUnit,
    BiologicalSex PatientSex,
    Guid HealthcareWorkerId,
    ContextualInformation Context) : IRequest<Guid>;

public record AddMedicalImageCommand(
    Guid CaseId,
    byte[] ImageData,
    ImageType Type,
    ImageMetadata Metadata) : IRequest<Result>;

public record AddAudioSymptomCommand(
    Guid CaseId,
    byte[] AudioData,
    string LanguageCode,
    int DurationSeconds,
    string? TranscribedText) : IRequest<Result>;

public record RequestDiagnosisCommand(
    Guid CaseId,
    DiagnosisMode Mode) : IRequest<DiagnosisResultDto>;

public record DiagnosisResultDto(
    DifferentialDiagnosis PrimaryDiagnosis,
    List<DifferentialDiagnosis> AlternativeDiagnoses,
    TreatmentProtocol TreatmentProtocol,
    List<string> ReasoningSteps);
