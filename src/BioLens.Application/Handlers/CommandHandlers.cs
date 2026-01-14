using BioLens.Application.Commands;
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
