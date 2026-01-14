namespace BioLens.Domain.Enums;

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
