using BioLens.Domain.Common;
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
