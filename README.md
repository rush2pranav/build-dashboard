# BUILD DASHBOARD - CI/CD MONITOR

Built a CI/CD build monitoring dashboard with a WPF frontend and ASP.NET Core backend that tracks build status, history, step by step progress and performance metrics across multiple game development projects.

![Dashboard Overview](images/dashboard_overview.png)

## PROJECT OVERVIEW

many game studios run hundreds of builds daily across multiple projects that includes game clients, servers, tools, test suites. Tools team build internal dashboards to monitor these pipelines to spot failures quickly and track build health over time.

This project replicates such system and implements a full stack build monitoring system:
- **Backend API** that stores build history, simulates build execution and exposes query endpoints
- **WPF Dashboard** that displays real time metrics, build lists and step by step details
- **Sample data generator** that creates 60 days of realistic build history across 5 game projects

## FEATURES

### WPF Dashboard
- **Real time metric cards** - total builds, success rate, failures, running, and average duration
- **Build history list** with color-coded status (green/red/yellow) and project filtering
- **Build detail panel** showing commit info, branch, trigger type and step by step progress with right (green ticks) and wrong (red cross) icons
- **Trigger Build button** - starts a new simulated build that progresses through steps in real time
- **Auto-refresh** every 5 seconds with connection status indicator
- **Dark theme** inspired by GitHub's CI/CD interface

### REST API
- **Paginated build listing** with filtering by project and status
- **Build detail endpoint** with full step information
- **Dashboard summary** with success rates, trends and project breakdown
- **Trigger build endpoint** that simulates async build execution with step by step progression
- **Swagger documentation** for interactive API testing

### Build Simulation
- Realistic 60 day history with 2-8 builds per day across 5 projects
- 75% success / 18% failure / 7% cancelled distribution
- 6 build steps per job with cascading failure behavior (failed step -> remaining steps skipped)
- Realistic commit messages, branches, and trigger types

![Build Details](images/build_details.png)

# ARCHITECTURE

```
--------------------------------
│     WPF Dashboard (App)      │
│  - MVVM Pattern              │
│  - Auto refresh timer        │
│  - Data binding to API data  │
--------------------------------
               | HTTP (localhost:5050)
--------------------------------
│    ASP.NET Core API          │
│  - Minimal API endpoints     │
│  - Build simulation engine   │
│  - Swagger documentation     │
--------------------------------
               | Entity Framework Core
--------------------------------
│       SQLite Database        │
│  - BuildJobs table           │
│  - BuildSteps table          │
--------------------------------
--------------------------------
│    Shared Core Library       │
│  - Models (BuildJob, etc.)   │
│  - DbContext + seed data     │
--------------------------------
```

## TOOLS AND TECHNOLOGIES

- **C#/ .NET 8.0** - application framework
- **WPF / XAML** - desktop UI with MVVM pattern
- **ASP.NET Core Minimal APIs** - REST backend
- **Entity Framework Core + SQLite** - data persistence
- **Swagger / OpenAPI** - API documentation
- **Newtonsoft.Json** - HTTP response deserialization
- **Multi-project solution** - App, Api and Core library
