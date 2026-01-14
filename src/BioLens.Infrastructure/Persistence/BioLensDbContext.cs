using BioLens.Domain.Entities;
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
