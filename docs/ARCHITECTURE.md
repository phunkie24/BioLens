# BioLens Architecture Documentation

## Agentic AI Design Pattern

### Overview

BioLens implements a **multi-agent system** where specialized autonomous agents collaborate to solve complex diagnostic workflows. This is fundamentally different from traditional monolithic AI approaches.

### Agent Types

#### 1. Coordinator Agents
- **DiagnosticCoordinatorAgent**: Orchestrates the entire workflow
- Implements: State Machine + Chain of Responsibility patterns
- Decides: Which agents to invoke and in what order

#### 2. Specialist Agents
- **ImageAnalysisAgent**: Computer vision analysis
- **AudioTranscriptionAgent**: Speech-to-text + symptom extraction
- **MedicalReasoningAgent**: Clinical decision-making
- **TreatmentPlannerAgent**: Resource-aware protocol generation

### Agent Communication

```
Request → Coordinator Agent
           ↓
      [Determines Strategy]
           ↓
      Specialist Agent 1 → Returns Findings
           ↓
      Specialist Agent 2 → Returns Findings
           ↓
      [Aggregates Results]
           ↓
      Final Response
```

### ReAct Pattern Implementation

Each agent follows the ReAct (Reasoning + Acting) cycle:

1. **Reason**: Analyze the input and context
2. **Act**: Make API calls, query databases, invoke tools
3. **Observe**: Evaluate the results
4. **Iterate**: Repeat if needed

### Chain-of-Thought

Agents expose their reasoning process:

```json
{
  "reasoningSteps": [
    "Step 1: Identified 3 distinct skin lesions",
    "Step 2: Correlated with fever symptoms from audio",
    "Step 3: Considered endemic diseases in East Africa",
    "Step 4: Applied diagnostic criteria for Dengue fever",
    "Step 5: Confidence level: High (85%)"
  ]
}
```

---

## Clean Architecture Layers

### Domain Layer (Core)
- **Zero dependencies** on external frameworks
- Contains: Entities, Value Objects, Domain Events, Repository Interfaces
- Enforces: Business rules and invariants

### Application Layer
- **Depends only on Domain**
- Contains: Use cases (Commands/Queries), DTOs, Handlers
- Implements: CQRS pattern with MediatR

### Infrastructure Layer
- **Implements Domain interfaces**
- Contains: Database, External APIs, File storage
- Gemini 3 integration lives here

### Agents Layer (Novel)
- **Sits between Application and Infrastructure**
- Contains: Agent implementations, Orchestration logic
- Leverages: Microsoft Semantic Kernel

---

## Design Patterns Used

| Pattern | Location | Purpose |
|---------|----------|---------|
| **Repository** | Domain/Repositories | Data access abstraction |
| **CQRS** | Application | Separate reads/writes |
| **Mediator** | Application | Decoupled command handling |
| **Strategy** | Agents | Online vs Offline processing |
| **Factory** | Infrastructure | Creating AI clients |
| **Observer** | Domain | Domain events |
| **Circuit Breaker** | Infrastructure | API resilience |
| **Retry** | Infrastructure | Transient fault handling |

---

## Gemini 3 Integration Points

### 1. Image Analysis
```csharp
POST /v1/models/gemini-3-pro:generateContent
{
  "contents": [{
    "role": "user",
    "parts": [
      { "text": "Analyze these medical images..." },
      { "inline_data": { "mime_type": "image/jpeg", "data": "..." } }
    ]
  }]
}
```

### 2. Audio Processing
```csharp
{
  "parts": [
    { "inline_data": { "mime_type": "audio/wav", "data": "..." } }
  ]
}
```

### 3. Structured Output
```csharp
{
  "generationConfig": {
    "responseMimeType": "application/json",
    "responseSchema": { /* JSON Schema */ }
  }
}
```

---

## Offline-First Strategy

### Three-Tier Approach

1. **Online Mode** (Default)
   - Full Gemini 3 API access
   - Real-time multimodal analysis
   - Latest medical knowledge

2. **Hybrid Mode**
   - Use cached responses for common patterns
   - Fall back to API for complex cases
   - Batch sync when online

3. **Offline Mode**
   - ML.NET models for common conditions
   - Rule-based diagnostic trees
   - Deferred sync queue

### Sync Architecture

```
[Mobile Device]
     ↓ (HTTPS)
[Edge Gateway] ← Caches responses
     ↓
[Cloud Storage] ← Historical data for ML training
```

---

## Security & Compliance

### HIPAA Compliance

- ✅ Data encryption at rest (SQLCipher)
- ✅ Data encryption in transit (TLS 1.3)
- ✅ Patient data anonymization
- ✅ Audit logging
- ✅ Access control (role-based)

### Data Privacy

- No PHI sent to Gemini API without anonymization
- Images stripped of metadata (EXIF)
- Patient IDs are hashed UUIDs

---

## Performance Considerations

### Latency Targets

| Operation | Target | Strategy |
|-----------|--------|----------|
| Image upload | <2s | Compression + async |
| Image analysis | <5s | Parallel processing |
| Audio transcription | <3s | Streaming API |
| Full diagnosis | <20s | Agent parallelization |

### Scalability

- **Horizontal scaling**: Stateless agents
- **Caching**: Redis for frequent queries
- **CDN**: Image/audio storage
- **Database**: Sharding by region

---

## Testing Strategy

### Unit Tests
- Domain entities and value objects
- Agent behavior (mocked AI responses)
- Repository implementations

### Integration Tests
- Full diagnostic workflow
- Database operations
- API integrations (test doubles)

### End-to-End Tests
- Real Gemini API calls (staging environment)
- Mobile app UI testing
- Offline mode scenarios

---

## Deployment Architecture

```
                [Internet]
                    ↓
            [Azure Front Door]
                    ↓
          [App Service (API)]
         /                    \
[Azure SQL Database]    [Blob Storage]
        ↓
  [Sync Queue]
        ↓
  [Mobile Clients]
```

### Infrastructure as Code

```hcl
# Terraform
resource "azurerm_app_service" "biolens_api" {
  name     = "biolens-api"
  location = "East US"
  
  site_config {
    dotnet_framework_version = "v10.0"
    always_on = true
  }
}
```

---

## Future Enhancements

1. **More Specialist Agents**
   - Pediatric specialist
   - Dermatology expert
   - Mental health counselor

2. **Agent Learning**
   - Fine-tune agents based on clinician feedback
   - Transfer learning from similar cases

3. **Multi-Agent Consensus**
   - Multiple reasoning agents "vote" on diagnosis
   - Confidence weighted by agent expertise

4. **Explainable AI**
   - Visual heatmaps showing what the model "sees"
   - Counterfactual explanations

---

**Last Updated**: 2025-01-14
