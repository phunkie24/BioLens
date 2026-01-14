using BioLens.Domain.Common;

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
