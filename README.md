# NexusAI — Enterprise Multi-Agent AI Orchestration Platform

[![CI](https://github.com/nishanthrjn/NexusAI/actions/workflows/ci.yml/badge.svg)](https://github.com/nishanthrjn/NexusAI/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com)
[![Semantic Kernel](https://img.shields.io/badge/Semantic%20Kernel-1.45-blue)](https://github.com/microsoft/semantic-kernel)

> An enterprise-grade multi-agent AI orchestration platform built in C#/.NET 10.
> Specialist AI agents collaborate autonomously to research, analyse, and synthesise
> complex information — powered by Microsoft Semantic Kernel and local LLMs via Ollama.

---

## Architecture
```text
User Prompt (POST /api/sessions)
↓
CoordinatorAgent — decomposes into specialist subtasks
↓
┌─────────────┬──────────────┬─────────────┐
│ WebSearch   │   Document   │  Analysis   │
│   Agent     │    Agent     │   Agent     │
│             │              │             │
│ Researches  │ Extracts &   │ Finds       │
│ current     │ structures   │ patterns &  │
│ information │ documents    │ insights    │
└─────────────┴──────────────┴─────────────┘
↓
ReportAgent — synthesises all findings
↓
Structured final report with citations
↓
PostgreSQL — full audit trail of every agent decision
SignalR — streams live agent output to browser
```
## Key Features

- **Multi-agent orchestration** — Coordinator decomposes complex prompts intelligently
- **Specialist agents** — WebSearch, Document, Analysis, Report agents with distinct roles
- **Real-time streaming** — SignalR hub broadcasts live agent output token by token
- **Full persistence** — Every task, message, and result stored in PostgreSQL
- **Extensible** — Add new specialist agents by implementing `IAgent`
- **Production-ready** — Polly retry, structured logging, EF Core migrations, CI

## Tech Stack
```text
| Layer | Technology |
|-------|-----------|
| Language | C# 12 / .NET 10 |
| AI Framework | Microsoft Semantic Kernel 1.45 |
| LLM | Ollama (llama3.2) — runs locally |
| Database | PostgreSQL + EF Core 9 |
| Real-time | ASP.NET Core SignalR |
| API | ASP.NET Core Minimal API + Scalar UI |
| Testing | xUnit — 7 tests |
| CI | GitHub Actions |
```
## Getting Started

### Prerequisites

- .NET 10 SDK
- Docker
- Ollama with llama3.2: `ollama pull llama3.2`

### Run locally

```bash
# Start PostgreSQL
docker-compose -f infra/docker-compose.yml up -d

# Set environment variables
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=nexusai;Username=nexusai;Password=nexusai_dev"
export Ollama__Endpoint="http://localhost:11434"
export Ollama__ChatModel="llama3.2"

# Run
dotnet run --project src/NexusAI.Api
```

### API Usage

```bash
# Start a multi-agent session
curl -X POST http://localhost:5000/api/sessions \
  -H "Content-Type: application/json" \
  -d '{"prompt":"Analyse the risks of AI adoption in financial services"}'

# Poll for results
curl http://localhost:5000/api/sessions/{sessionId}

# Get a specific task result
curl http://localhost:5000/api/sessions/{sessionId}/tasks/{taskId}
```

Scalar UI: `http://localhost:5000/scalar/v1`

## Project Structure
```text
NexusAI/
├── src/
│   ├── NexusAI.Domain/          — Entities, interfaces, enums
│   ├── NexusAI.Core/            — Agent engine, orchestrator
│   │   └── Agents/              — CoordinatorAgent, WebSearchAgent,
│   │                              DocumentAgent, AnalysisAgent, ReportAgent
│   ├── NexusAI.Infrastructure/  — PostgreSQL, EF Core, repositories
│   └── NexusAI.Api/             — REST endpoints, SignalR hub
├── tests/
│   └── NexusAI.Tests/           — 7 xUnit tests
└── infra/
└── docker-compose.yml       — PostgreSQL on port 5433
```

## Author

**Nishanth Rajan** — Senior .NET Engineer → AI Engineer
Hannover, Germany | EU Blue Card
