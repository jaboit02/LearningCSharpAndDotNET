# Tech News Aggregator (Learning & Portfolio Project)

A learning project built to explore **C#**, **ASP.NET Core (.NET 10)**, **Entity Framework Core**, and workflow automation with **n8n**.

> **Note on Project Scope & AI Usage:**  
> This repository is strictly a **local development and learning environment**—it is not intended for production deployment without further security hardening (e.g., HTTPS, authentication, rate limiting).  
> Artificial Intelligence was actively used as an interactive tutor and programming assistant during development to improve understanding of core architectural concepts, best practices, and .NET conventions step by step.

---

## Architecture

```text
[RSS Feed] ──▶ [n8n Workflow (Docker)] ──POST /api/articles──▶ [.NET 10 Minimal API] ──▶ [SQLite (news.db)]
                     ▲
                     └──POST /webhook/sync-news── [.NET BackgroundService on Startup]
```

1. **Automated Feed Collection (n8n):** A scheduled n8n workflow fetches official public RSS/Atom feeds (e.g., Heise Online), filters relevant tech articles, and sends structured JSON payloads to the .NET backend.
2. **Backend API & Persistence (.NET 10 / EF Core):** An ASP.NET Core Minimal API endpoint receives incoming articles, checks for duplicates by URL, and persists them into a local **SQLite** database.
3. **Startup Sync (`BackgroundService`):** On application startup, a hosted background service automatically triggers n8n's webhook to fetch the latest articles immediately.
4. **Web Interface (Razor Pages):** A server-side rendered UI allows browsing, searching/filtering by keywords or source, and deleting stored articles.

## Tech Stack

- **Language & Framework:** C# / ASP.NET Core (.NET 10, Razor Pages & Minimal APIs)
- **Database & ORM:** SQLite with Entity Framework Core
- **Automation:** n8n (self-hosted locally via Docker Compose)
- **Data Source:** Official public RSS/Atom feeds

## Project Structure

```text
├── src/
│   └── NewsAggregator.Web/                         # ASP.NET Core web app, Minimal API & EF Core data layer
├── n8n/
│   ├── docker-compose.yml                          # Local Docker setup for n8n
│   └── manual_triggered_scheduled_Workflow.json    # Exportable n8n workflow definition
├── .gitignore
└── README.md
```

## Getting Started (Local Development)

### Prerequisites
- [.NET 10 SDK](https://microsoft.com)
- [Docker](https://docker.com)
- EF Core CLI Tools: `dotnet tool install --global dotnet-ef --version 10.*`

### 1. Start n8n
```bash
cd n8n
docker compose up -d
```
1. Open n8n locally at `http://localhost:5678`.
2. Import `n8n/manual_triggered_scheduled_Workflow.json` and **Publish / Activate** the workflow so the schedule and startup webhook (`/webhook/sync-news`) are active.

### 2. Database Setup & Web App
```bash
cd src/NewsAggregator.Web
dotnet ef database update
dotnet run
```
- **Homepage:** `http://127.0.0.1:5223`
- **API Documentation (Scalar):** `http://127.0.0.1:5223/scalar/v1`
