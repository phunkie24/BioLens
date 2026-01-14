# BioLens - Agentic AI Diagnostic Field Assistant

🏥 **Enterprise-Grade Healthcare AI for Resource-Constrained Settings**

## 🎯 Gemini 3 Global Hackathon Submission

BioLens is an **offline-capable, agentic AI diagnostic assistant** designed specifically for healthcare workers in rural and low-resource settings. It leverages **Google's Gemini 3 API** with a novel **multi-agent architecture** to provide differential diagnoses and treatment protocols adapted to local resources.

---

## 🌟 Key Innovation: Agentic AI Design Pattern

Unlike traditional monolithic AI systems, BioLens uses **autonomous specialized agents** that collaborate using the **ReAct (Reasoning + Acting)** pattern.

### Why Agentic?

1. **Specialized Expertise**: Each agent focuses on one domain
2. **Autonomous Decision-Making**: Agents can self-correct and iterate  
3. **Transparent Reasoning**: Chain-of-Thought output shows conclusions
4. **Offline Resilience**: Agents operate independently with cached knowledge

---

## 🏗️ Architecture

**Clean Architecture + DDD + CQRS + Agentic AI**

```
BioLens/
├── BioLens.Domain/           # Core business logic
├── BioLens.Application/      # Use cases & CQRS
├── BioLens.Infrastructure/   # External services
└── BioLens.Agents/          # 🤖 Agentic AI Layer
```

---

## 🚀 Quick Start

```bash
# Restore packages
dotnet restore

# Build
dotnet build

# Run tests
dotnet test
```

---

## 🤖 Agent Architecture

1. **DiagnosticCoordinatorAgent** - Orchestrates workflow
2. **ImageAnalysisAgent** - Analyzes medical images
3. **AudioTranscriptionAgent** - Extracts symptoms from audio
4. **MedicalReasoningAgent** - Generates differential diagnoses
5. **TreatmentPlannerAgent** - Creates resource-aware protocols

---

## 🎯 Hackathon Alignment

- **Technical Execution (40%)**: Clean Architecture, Design Patterns, Production-Ready
- **Innovation (30%)**: Agentic AI, Context-Aware, Offline-First
- **Impact (20%)**: Serves 2+ billion people in healthcare deserts
- **Presentation (10%)**: Clear docs, demo-ready

---

**Built with ❤️ for the Gemini 3 Global Hackathon**
