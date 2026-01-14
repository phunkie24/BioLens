using BioLens.Domain.Common;
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
