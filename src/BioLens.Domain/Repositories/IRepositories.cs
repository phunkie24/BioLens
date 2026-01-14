using BioLens.Domain.Entities;

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
