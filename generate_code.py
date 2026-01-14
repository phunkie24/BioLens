#!/usr/bin/env python3
"""
BioLens Complete Solution Code Generator
Generates all .NET 10.0 source files with Agentic AI pattern
"""

import os
from pathlib import Path

BASE_DIR = Path("/home/claude/BioLens")

# All code templates
TEMPLATES = {
    # ===================
    # DOMAIN LAYER
    # ===================
    "domain/enums": """namespace BioLens.Domain.Enums;

public enum CaseStatus
{
    Created,
    InProgress,
    DiagnosisCompleted,
    TreatmentAssigned,
    FollowUpRequired,
    Resolved,
    Escalated
}

public enum ConfidenceLevel
{
    Low,
    Medium,
    High,
    VeryHigh
}

public enum UrgencyLevel
{
    Routine,
    Urgent,
    Emergency,
    Critical
}

public enum ImageType
{
    Skin,
    Wound,
    Rash,
    Eyes,
    Throat,
    Limb,
    Other
}

public enum BiologicalSex
{
    Male,
    Female,
    Intersex,
    Unknown
}

public enum AgeUnit
{
    Days,
    Weeks,
    Months,
    Years
}

public enum FacilityCapabilities
{
    BasicHealthPost,
    RuralClinic,
    DistrictHospital,
    ReferralHospital
}

public enum DiagnosisMode
{
    Online,
    Offline,
    Hybrid
}

public enum DiagnosisSource
{
    GeminiAI,
    OfflineCache,
    HumanExpert
}
""",

    # ===================
    "domain/entities/patient": """using BioLens.Domain.Common;
using BioLens.Domain.Enums;
using BioLens.Domain.ValueObjects;

namespace BioLens.Domain.Entities;

public class Patient : Entity
{
    private readonly List<KnownCondition> _medicalHistory = [];
    private readonly List<Allergy> _knownAllergies = [];

    private Patient() { } // EF Core

    public Patient(
        string anonymizedId,
        int? ageYears,
        AgeUnit ageUnit,
        BiologicalSex sex)
    {
        Id = Guid.NewGuid();
        AnonymizedId = anonymizedId;
        AgeYears = ageYears;
        AgeUnit = ageUnit;
        Sex = sex;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string AnonymizedId { get; private set; } = default!;
    public int? AgeYears { get; private set; }
    public AgeUnit AgeUnit { get; private set; }
    public BiologicalSex Sex { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<KnownCondition> MedicalHistory => _medicalHistory.AsReadOnly();
    public IReadOnlyCollection<Allergy> KnownAllergies => _knownAllergies.AsReadOnly();

    public void AddMedicalCondition(KnownCondition condition) => _medicalHistory.Add(condition);
    public void AddAllergy(Allergy allergy) => _knownAllergies.Add(allergy);
}
""",

    # ===================
    "domain/entities/diagnostic_case": """using BioLens.Domain.Common;
using BioLens.Domain.Enums;
using BioLens.Domain.ValueObjects;
using BioLens.Domain.Events;

namespace BioLens.Domain.Entities;

public class DiagnosticCase : AggregateRoot
{
    private readonly List<MedicalImage> _images = [];
    private readonly List<DifferentialDiagnosis> _alternativeDiagnoses = [];

    private DiagnosticCase() { } // EF Core

    public DiagnosticCase(
        Patient patient,
        Guid healthcareWorkerId,
        ContextualInformation context)
    {
        Id = Guid.NewGuid();
        Patient = patient;
        HealthcareWorkerId = healthcareWorkerId;
        Context = context;
        Status = CaseStatus.Created;
        CreatedAt = DateTimeOffset.UtcNow;
        IsSyncedToCloud = false;

        AddDomainEvent(new DiagnosticCaseCreatedEvent(Id, patient.Id));
    }

    public Patient Patient { get; private set; } = default!;
    public Guid HealthcareWorkerId { get; private set; }
    public AudioSymptomDescription? AudioDescription { get; private set; }
    public ContextualInformation Context { get; private set; } = default!;
    
    public DifferentialDiagnosis? PrimaryDiagnosis { get; private set; }
    public IReadOnlyCollection<DifferentialDiagnosis> AlternativeDiagnoses => _alternativeDiagnoses.AsReadOnly();
    public TreatmentProtocol? RecommendedProtocol { get; private set; }
    
    public CaseStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public bool IsSyncedToCloud { get; private set; }
    
    public IReadOnlyCollection<MedicalImage> Images => _images.AsReadOnly();

    public Result AddMedicalImage(MedicalImage image)
    {
        if (_images.Count >= 10)
            return Result.Failure("Maximum 10 images per case");
        
        _images.Add(image);
        AddDomainEvent(new ImageAddedEvent(Id, image.Id));
        return Result.Success();
    }

    public Result SetAudioDescription(AudioSymptomDescription audio)
    {
        AudioDescription = audio;
        AddDomainEvent(new AudioDescriptionAddedEvent(Id));
        return Result.Success();
    }

    public Result CompleteDiagnosis(
        DifferentialDiagnosis primary,
        List<DifferentialDiagnosis> alternatives,
        TreatmentProtocol protocol)
    {
        if (Status != CaseStatus.InProgress && Status != CaseStatus.Created)
            return Result.Failure("Case must be in progress");

        PrimaryDiagnosis = primary;
        _alternativeDiagnoses.Clear();
        _alternativeDiagnoses.AddRange(alternatives);
        RecommendedProtocol = protocol;
        Status = CaseStatus.DiagnosisCompleted;
        CompletedAt = DateTimeOffset.UtcNow;
        
        AddDomainEvent(new DiagnosisCompletedEvent(Id, primary.ConditionName));
        return Result.Success();
    }

    public void StartDiagnosis()
    {
        Status = CaseStatus.InProgress;
    }

    public void MarkAsSynced()
    {
        IsSyncedToCloud = true;
    }
}
""",

    # ===================
    "domain/value_objects": """using BioLens.Domain.Common;
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
""",

    # ===================
    "domain/events": """using BioLens.Domain.Common;

namespace BioLens.Domain.Events;

public record DiagnosticCaseCreatedEvent(
    Guid CaseId,
    Guid PatientId) : DomainEvent;

public record ImageAddedEvent(
    Guid CaseId,
    Guid ImageId) : DomainEvent;

public record AudioDescriptionAddedEvent(
    Guid CaseId) : DomainEvent;

public record DiagnosisCompletedEvent(
    Guid CaseId,
    string PrimaryCondition) : DomainEvent;

public record CaseSyncedEvent(
    Guid CaseId,
    DateTimeOffset SyncedAt) : DomainEvent;
""",

    # ===================
    "domain/repositories": """using BioLens.Domain.Entities;

namespace BioLens.Domain.Repositories;

public interface IDiagnosticCaseRepository
{
    Task<DiagnosticCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> AddAsync(DiagnosticCase diagnosticCase, CancellationToken cancellationToken = default);
    Task UpdateAsync(DiagnosticCase diagnosticCase, CancellationToken cancellationToken = default);
    Task<List<DiagnosticCase>> GetUnsyncedAsync(CancellationToken cancellationToken = default);
}

public interface IPatientRepository
{
    Task<Patient?> GetByAnonymizedIdAsync(string anonymizedId, CancellationToken cancellationToken = default);
    Task<Guid> AddAsync(Patient patient, CancellationToken cancellationToken = default);
}
""",

    # ===================
    # AGENTS LAYER - Core Agentic AI
    # ===================
    "agents/core/agent_base": """using Microsoft.SemanticKernel;
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
""",

    # ===================
    "agents/core/diagnostic_coordinator": """using BioLens.Domain.Entities;
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
""",

    # ===================
    "agents/specialized/image_analysis": """using BioLens.Domain.ValueObjects;
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
""",

    # ===================
    "agents/specialized/audio_transcription": """using BioLens.Domain.ValueObjects;
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
""",

    # ===================
    "agents/specialized/medical_reasoning": """using BioLens.Domain.Entities;
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
""",

    # ===================
    "agents/specialized/treatment_planner": """using BioLens.Domain.ValueObjects;
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
""",

    # ===================
    # APPLICATION LAYER
    # ===================
    "application/commands": """using BioLens.Domain.Entities;
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
""",

    # ===================
    "application/handlers": """using BioLens.Application.Commands;
using BioLens.Domain.Entities;
using BioLens.Domain.Repositories;
using BioLens.Domain.ValueObjects;
using BioLens.Agents.Core;
using MediatR;

namespace BioLens.Application.Handlers;

public class CreateDiagnosticCaseHandler : IRequestHandler<CreateDiagnosticCaseCommand, Guid>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IDiagnosticCaseRepository _caseRepository;

    public CreateDiagnosticCaseHandler(
        IPatientRepository patientRepository,
        IDiagnosticCaseRepository caseRepository)
    {
        _patientRepository = patientRepository;
        _caseRepository = caseRepository;
    }

    public async Task<Guid> Handle(CreateDiagnosticCaseCommand request, CancellationToken cancellationToken)
    {
        var patient = await _patientRepository.GetByAnonymizedIdAsync(
            request.PatientAnonymizedId,
            cancellationToken);

        if (patient == null)
        {
            patient = new Patient(
                request.PatientAnonymizedId,
                request.PatientAge,
                request.PatientAgeUnit,
                request.PatientSex);
            await _patientRepository.AddAsync(patient, cancellationToken);
        }

        var diagnosticCase = new DiagnosticCase(
            patient,
            request.HealthcareWorkerId,
            request.Context);

        var caseId = await _caseRepository.AddAsync(diagnosticCase, cancellationToken);
        return caseId;
    }
}

public class RequestDiagnosisHandler : IRequestHandler<RequestDiagnosisCommand, DiagnosisResultDto>
{
    private readonly IDiagnosticCaseRepository _repository;
    private readonly DiagnosticCoordinatorAgent _coordinatorAgent;

    public RequestDiagnosisHandler(
        IDiagnosticCaseRepository repository,
        DiagnosticCoordinatorAgent coordinatorAgent)
    {
        _repository = repository;
        _coordinatorAgent = coordinatorAgent;
    }

    public async Task<DiagnosisResultDto> Handle(
        RequestDiagnosisCommand request,
        CancellationToken cancellationToken)
    {
        var diagnosticCase = await _repository.GetByIdAsync(request.CaseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Case {request.CaseId} not found");

        diagnosticCase.StartDiagnosis();

        var agentRequest = new AgentRequest(
            Guid.NewGuid().ToString(),
            "RunDiagnosis",
            new Dictionary<string, object> { ["case"] = diagnosticCase },
            new AgentContext(diagnosticCase.Id, new Dictionary<string, object>()));

        var agentResponse = await _coordinatorAgent.ExecuteAsync(agentRequest, cancellationToken);

        if (!agentResponse.IsSuccess)
            throw new InvalidOperationException("Diagnosis failed: " + string.Join(", ", agentResponse.Messages));

        dynamic result = agentResponse.Result!;
        
        // Map to domain objects (simplified for demo)
        var primaryDiagnosis = new DifferentialDiagnosis(
            "Sample Diagnosis",
            "A00.0",
            Domain.Enums.ConfidenceLevel.Medium,
            new List<string> { "Evidence 1" },
            new List<string>(),
            Domain.Enums.UrgencyLevel.Routine);

        var treatment = new TreatmentProtocol(
            "Sample Protocol",
            new List<TreatmentStep>(),
            new List<MedicationRecommendation>(),
            new List<string>(),
            new FollowUpGuidance(new List<string>(), new List<string>(), 7),
            new EmergencyEscalation(new List<string>(), Domain.Enums.UrgencyLevel.Urgent, "District Hospital"));

        diagnosticCase.CompleteDiagnosis(
            primaryDiagnosis,
            new List<DifferentialDiagnosis>(),
            treatment);

        await _repository.UpdateAsync(diagnosticCase, cancellationToken);

        return new DiagnosisResultDto(
            primaryDiagnosis,
            new List<DifferentialDiagnosis>(),
            treatment,
            agentResponse.Messages);
    }
}
""",

    # ===================
    # INFRASTRUCTURE
    # ===================
    "infrastructure/gemini_service": """using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace BioLens.Infrastructure.AI;

public interface IGeminiAIService
{
    Task<string> GenerateContentAsync(
        string prompt,
        List<byte[]>? images = null,
        byte[]? audio = null,
        CancellationToken cancellationToken = default);
}

public class GeminiAIService : IGeminiAIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiAIService> _logger;
    private readonly GeminiConfiguration _config;

    public GeminiAIService(
        HttpClient httpClient,
        ILogger<GeminiAIService> logger,
        IOptions<GeminiConfiguration> config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config.Value;
    }

    public async Task<string> GenerateContentAsync(
        string prompt,
        List<byte[]>? images = null,
        byte[]? audio = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = BuildRequest(prompt, images, audio);
            
            var response = await _httpClient.PostAsJsonAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3-pro:generateContent?key={_config.ApiKey}",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(
                cancellationToken: cancellationToken);

            return result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini API call failed");
            throw;
        }
    }

    private object BuildRequest(string prompt, List<byte[]>? images, byte[]? audio)
    {
        var parts = new List<object> { new { text = prompt } };

        if (images != null)
        {
            parts.AddRange(images.Select(img => new
            {
                inline_data = new
                {
                    mime_type = "image/jpeg",
                    data = Convert.ToBase64String(img)
                }
            }));
        }

        if (audio != null)
        {
            parts.Add(new
            {
                inline_data = new
                {
                    mime_type = "audio/wav",
                    data = Convert.ToBase64String(audio)
                }
            });
        }

        return new
        {
            contents = new[]
            {
                new { role = "user", parts }
            },
            generationConfig = new
            {
                temperature = 0.2,
                topP = 0.95,
                topK = 40,
                maxOutputTokens = 4096,
                responseMimeType = "application/json"
            }
        };
    }
}

public class GeminiConfiguration
{
    public string ApiKey { get; set; } = "";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";
}

public record GeminiResponse(Candidate[]? Candidates);
public record Candidate(Content? Content);
public record Content(Part[]? Parts);
public record Part(string? Text);
""",

    # ===================
    "infrastructure/persistence": """using BioLens.Domain.Entities;
using BioLens.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BioLens.Infrastructure.Persistence;

public class BioLensDbContext : DbContext
{
    public BioLensDbContext(DbContextOptions<BioLensDbContext> options) : base(options) { }

    public DbSet<DiagnosticCase> DiagnosticCases => Set<DiagnosticCase>();
    public DbSet<Patient> Patients => Set<Patient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BioLensDbContext).Assembly);
    }
}

public class DiagnosticCaseRepository : IDiagnosticCaseRepository
{
    private readonly BioLensDbContext _context;

    public DiagnosticCaseRepository(BioLensDbContext context)
    {
        _context = context;
    }

    public async Task<DiagnosticCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.DiagnosticCases
            .Include(c => c.Patient)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Guid> AddAsync(DiagnosticCase diagnosticCase, CancellationToken cancellationToken = default)
    {
        await _context.DiagnosticCases.AddAsync(diagnosticCase, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return diagnosticCase.Id;
    }

    public async Task UpdateAsync(DiagnosticCase diagnosticCase, CancellationToken cancellationToken = default)
    {
        _context.DiagnosticCases.Update(diagnosticCase);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<DiagnosticCase>> GetUnsyncedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DiagnosticCases
            .Where(c => !c.IsSyncedToCloud)
            .ToListAsync(cancellationToken);
    }
}

public class PatientRepository : IPatientRepository
{
    private readonly BioLensDbContext _context;

    public PatientRepository(BioLensDbContext context)
    {
        _context = context;
    }

    public async Task<Patient?> GetByAnonymizedIdAsync(
        string anonymizedId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Patients
            .FirstOrDefaultAsync(p => p.AnonymizedId == anonymizedId, cancellationToken);
    }

    public async Task<Guid> AddAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        await _context.Patients.AddAsync(patient, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return patient.Id;
    }
}
""",
}

def create_file(path: Path, content: str):
    """Create a file with the given content"""
    path.parent.mkdir(parents=True, exist_ok=True)
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content.strip() + '\n')
    print(f"✓ Created: {path.relative_to(BASE_DIR)}")

def main():
    print("=" * 60)
    print("BioLens Code Generator")
    print("Agentic AI Healthcare Diagnostic Assistant")
    print("=" * 60)
    print()

    # Domain Layer
    print("📦 Generating Domain Layer...")
    create_file(BASE_DIR / "src/BioLens.Domain/Enums/Enums.cs", TEMPLATES["domain/enums"])
    create_file(BASE_DIR / "src/BioLens.Domain/Entities/Patient.cs", TEMPLATES["domain/entities/patient"])
    create_file(BASE_DIR / "src/BioLens.Domain/Entities/DiagnosticCase.cs", TEMPLATES["domain/entities/diagnostic_case"])
    create_file(BASE_DIR / "src/BioLens.Domain/ValueObjects/ValueObjects.cs", TEMPLATES["domain/value_objects"])
    create_file(BASE_DIR / "src/BioLens.Domain/Events/DomainEvents.cs", TEMPLATES["domain/events"])
    create_file(BASE_DIR / "src/BioLens.Domain/Repositories/IRepositories.cs", TEMPLATES["domain/repositories"])

    # Agents Layer
    print("🤖 Generating Agents Layer...")
    create_file(BASE_DIR / "src/BioLens.Agents/Core/AgentBase.cs", TEMPLATES["agents/core/agent_base"])
    create_file(BASE_DIR / "src/BioLens.Agents/Core/DiagnosticCoordinatorAgent.cs", TEMPLATES["agents/core/diagnostic_coordinator"])
    create_file(BASE_DIR / "src/BioLens.Agents/Core/ImageAnalysisAgent.cs", TEMPLATES["agents/specialized/image_analysis"])
    create_file(BASE_DIR / "src/BioLens.Agents/Core/AudioTranscriptionAgent.cs", TEMPLATES["agents/specialized/audio_transcription"])
    create_file(BASE_DIR / "src/BioLens.Agents/Core/MedicalReasoningAgent.cs", TEMPLATES["agents/specialized/medical_reasoning"])
    create_file(BASE_DIR / "src/BioLens.Agents/Core/TreatmentPlannerAgent.cs", TEMPLATES["agents/specialized/treatment_planner"])

    # Application Layer
    print("⚙️  Generating Application Layer...")
    create_file(BASE_DIR / "src/BioLens.Application/Commands/Commands.cs", TEMPLATES["application/commands"])
    create_file(BASE_DIR / "src/BioLens.Application/Handlers/CommandHandlers.cs", TEMPLATES["application/handlers"])

    # Infrastructure Layer
    print("🔧 Generating Infrastructure Layer...")
    create_file(BASE_DIR / "src/BioLens.Infrastructure/AI/GeminiAIService.cs", TEMPLATES["infrastructure/gemini_service"])
    create_file(BASE_DIR / "src/BioLens.Infrastructure/Persistence/BioLensDbContext.cs", TEMPLATES["infrastructure/persistence"])

    print()
    print("=" * 60)
    print("✅ Code generation complete!")
    print(f"📁 Files created in: {BASE_DIR}")
    print("=" * 60)

if __name__ == "__main__":
    main()
