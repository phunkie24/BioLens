using BioLens.Application.Commands;
using BioLens.Domain.Enums;
using BioLens.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BioLens.Example;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("🏥 BioLens - Agentic AI Diagnostic Assistant");
        Console.WriteLine("=============================================\n");

        var host = CreateHostBuilder(args).Build();
        
        var mediator = host.Services.GetRequiredService<IMediator>();
        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        try
        {
            await RunExampleDiagnosisAsync(mediator, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Example failed");
            Console.WriteLine($"\n❌ Error: {ex.Message}");
        }
    }

    private static async Task RunExampleDiagnosisAsync(IMediator mediator, ILogger logger)
    {
        Console.WriteLine("📋 Creating diagnostic case...\n");

        // Step 1: Create diagnostic case
        var createCaseCommand = new CreateDiagnosticCaseCommand(
            PatientAnonymizedId: "PAT_DEMO_001",
            PatientAge: 35,
            PatientAgeUnit: AgeUnit.Years,
            PatientSex: BiologicalSex.Female,
            HealthcareWorkerId: Guid.NewGuid(),
            Context: new ContextualInformation(
                Region: new GeographicRegion(
                    Country: "Kenya",
                    Region: "Rift Valley",
                    District: "Narok",
                    Latitude: -1.0880,
                    Longitude: 35.8710),
                AvailableMedications: new List<string>
                {
                    "Paracetamol", "Ibuprofen", "Amoxicillin",
                    "Metronidazole", "ORS", "Artemether-Lumefantrine"
                },
                LocalEndemicDiseases: new List<string>
                {
                    "Malaria", "Typhoid", "Dengue Fever"
                },
                FacilityLevel: FacilityCapabilities.RuralClinic,
                CulturalContext: new CulturalConsiderations(
                    PrimaryLanguage: "Swahili",
                    CommonBeliefs: new List<string> { "Traditional medicine respected" },
                    TreatmentPreferences: new List<string> { "Prefers oral medications" })
            )
        );

        var caseId = await mediator.Send(createCaseCommand);
        Console.WriteLine($"✓ Case created: {caseId}\n");

        // Step 2: Add medical images (simulated)
        Console.WriteLine("📸 Adding medical images...");
        
        var imageCommand = new AddMedicalImageCommand(
            CaseId: caseId,
            ImageData: SimulateImage("skin_lesion_1.jpg"),
            Type: ImageType.Skin,
            Metadata: new ImageMetadata(
                Width: 1920,
                Height: 1080,
                FileSizeBytes: 245000,
                DeviceModel: "iPhone 14 Pro")
        );

        await mediator.Send(imageCommand);
        Console.WriteLine("  ✓ Image 1 added: Skin lesion (frontal view)");

        // Step 3: Add audio symptoms (simulated)
        Console.WriteLine("\n🎤 Adding audio symptom description...");
        
        var audioCommand = new AddAudioSymptomCommand(
            CaseId: caseId,
            AudioData: SimulateAudio("symptoms.wav"),
            LanguageCode: "sw", // Swahili
            DurationSeconds: 45,
            TranscribedText: "Mgonjwa analalamika joto kali, kichwa kuuma, na maumivu ya mwili kwa siku tatu. Pia ana hamu ya kutapika."
            // Translation: "Patient reports high fever, headache, and body aches for three days. Also has nausea."
        );

        await mediator.Send(audioCommand);
        Console.WriteLine("  ✓ Audio transcribed: Fever, headache, body aches, nausea (3 days)\n");

        // Step 4: Request diagnosis (triggers agent workflow)
        Console.WriteLine("🤖 Activating AI agents for diagnosis...\n");
        Console.WriteLine("  → ImageAnalysisAgent: Analyzing skin lesions...");
        await Task.Delay(1000); // Simulate processing
        Console.WriteLine("  → AudioTranscriptionAgent: Extracting symptoms...");
        await Task.Delay(800);
        Console.WriteLine("  → MedicalReasoningAgent: Generating differential diagnosis...");
        await Task.Delay(1500);
        Console.WriteLine("  → TreatmentPlannerAgent: Creating treatment protocol...\n");
        await Task.Delay(1200);

        var diagnosisCommand = new RequestDiagnosisCommand(caseId, DiagnosisMode.Online);
        var result = await mediator.Send(diagnosisCommand);

        // Display results
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("                    DIAGNOSIS RESULTS                       ");
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        Console.WriteLine($"PRIMARY DIAGNOSIS:");
        Console.WriteLine($"  Condition: {result.PrimaryDiagnosis.ConditionName}");
        Console.WriteLine($"  ICD-10: {result.PrimaryDiagnosis.ICD10Code}");
        Console.WriteLine($"  Confidence: {result.PrimaryDiagnosis.Confidence}");
        Console.WriteLine($"  Urgency: {result.PrimaryDiagnosis.Urgency}\n");

        if (result.AlternativeDiagnoses.Any())
        {
            Console.WriteLine("ALTERNATIVE DIAGNOSES:");
            foreach (var alt in result.AlternativeDiagnoses.Take(3))
            {
                Console.WriteLine($"  • {alt.ConditionName} ({alt.Confidence} confidence)");
            }
            Console.WriteLine();
        }

        Console.WriteLine($"TREATMENT PROTOCOL: {result.TreatmentProtocol.ProtocolName}");
        
        if (result.TreatmentProtocol.Medications.Any())
        {
            Console.WriteLine("\n  Medications:");
            foreach (var med in result.TreatmentProtocol.Medications)
            {
                Console.WriteLine($"    • {med.MedicationName}: {med.Dosage} every {med.Frequency} for {med.DurationDays} days");
            }
        }

        if (result.ReasoningSteps.Any())
        {
            Console.WriteLine("\n  Agent Reasoning Steps:");
            foreach (var step in result.ReasoningSteps)
            {
                Console.WriteLine($"    {step}");
            }
        }

        Console.WriteLine("\n═══════════════════════════════════════════════════════════");
        Console.WriteLine("\n✅ Diagnostic workflow completed successfully!");
        Console.WriteLine("\n💡 This demonstrates the Agentic AI approach where specialized");
        Console.WriteLine("   agents collaborate autonomously to solve complex tasks.\n");
    }

    private static byte[] SimulateImage(string filename)
    {
        // In production, this would load actual image data
        return new byte[245000]; // Simulated 245KB image
    }

    private static byte[] SimulateAudio(string filename)
    {
        // In production, this would load actual audio data
        return new byte[180000]; // Simulated 180KB audio
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Add MediatR
                services.AddMediatR(cfg =>
                    cfg.RegisterServicesFromAssembly(typeof(CreateDiagnosticCaseCommand).Assembly));

                // Add application services
                // services.AddBioLensAgents(context.Configuration);
                // services.AddBioLensInfrastructure(context.Configuration);

                // For demo purposes, we'll use mock implementations
                Console.WriteLine("ℹ️  Note: This is a demo. Real Gemini 3 API integration requires configuration.\n");
            });
}
