# GACS Developer Onboarding Guide

**Welcome to the Gated Access Compliance Suite (GACS) team!** 🎉

This guide will get you from zero to running the full stack on your local machine. No prior knowledge of our architecture required.

---

## 📋 Prerequisites (Install These First)

Before you start, make sure you have:

| Tool | Version | Why We Need It | Download |
|------|---------|----------------|----------|
| **.NET SDK** | 10.0 or later | Builds and runs all our code | [Download](https://dotnet.microsoft.com/download) |
| **Docker Desktop** | Latest | Runs databases and monitoring tools | [Download](https://www.docker.com/products/docker-desktop) |
| **Git** | Latest | Source control | [Download](https://git-scm.com/downloads) |
| **Visual Studio 2022** or **VS Code** | Latest | IDE for development | [VS Download](https://visualstudio.microsoft.com/) |
| **PowerShell 7** or **Git Bash** | Latest | Running scripts | Included with Windows/Git |

> 💡 **Tip:** After installing Docker, make sure it's running (you'll see a whale icon in your system tray). This is a common gotcha!

---

## 🚀 Quick Start (5 Minutes to Running)

### Step 1: Clone the Repository

```bash
git clone https://github.com/yourorg/GACS.git
cd GACS
```

### Step 2: Trust the SSL Certificate (One-Time)

Our local development uses HTTPS. Trust the certificate:

```bash
dotnet dev-certs https --trust
```

### Step 3: Start Everything

Navigate to the Aspire AppHost project and run:

```bash
cd aspire/GACS.AppHost
dotnet run
```

This will:
1. 🐳 Start Docker containers (SQL Server, Redis, Prometheus, Grafana, Loki)
2. 🔨 Build all .NET projects
3. 🚀 Launch all services
4. 🌐 Open the Aspire Dashboard in your browser

---

## 🎛️ The Aspire Dashboard — Your Control Center

Once everything starts, you'll see the **Aspire Dashboard**. Think of it as mission control for your local development.

### What You'll See

| Resource | Friendly Name | What It Does | Clickable URL |
|----------|---------------|--------------|---------------|
| `prometheus` | 📊 Prometheus — Metrics | Collects performance data from all services | Open Prometheus UI |
| `loki` | 📜 Loki — Log Aggregation | Gathers all application logs | Open Loki API |
| `grafana` | 📈 Grafana — Dashboards | Pretty charts and visualizations | Open Grafana (No login required) |
| `sql-server` | 🗄️ SQL Server — Main Database | Stores all application data | (Connection string only) |
| `gacsdb` | 📦 GACS Database | The actual database file | (Managed by SQL Server) |
| `redis` | ⚡ Redis — Cache & Real-time Store | Fast temporary storage | (Connection string only) |
| `azure-storage` | ☁️ Azure Storage (Emulator) | Local file/blob storage | (Emulator runs in background) |
| `identity-api` | 🔐 Identity API — Authentication & Users | Login, tokens, user management | 📖 API Documentation (Scalar) |
| `gateway` | 🚪 API Gateway — Frontend Entry Point | Routes requests | ✅ Health Check |
| `admin-portal` | 🖥️ Admin Portal — Management UI | The web interface | 🚀 Open Admin Portal |

### Understanding the Colors

- 🟢 **Green** — Service is healthy and running
- 🟡 **Yellow** — Service is starting up or has warnings
- 🔴 **Red** — Service failed to start (check logs)
- ⚪ **Gray** — Service is waiting for dependencies

---

## 🗺️ Architecture Overview (The Big Picture)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           YOUR BROWSER                                    │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────┐  │
│  │  Admin Portal   │  │    Grafana      │  │    API Documentation    │  │
│  │  (Blazor)       │  │  (Dashboards)   │  │    (Scalar/Swagger)     │  │
│  │  Port: 5162     │  │   Port: 3000    │  │       Port: 5211        │  │
│  └────────┬────────┘  └─────────────────┘  └─────────────────────────┘  │
└───────────┼─────────────────────────────────────────────────────────────┘
            │
            │ HTTP Requests
            ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         API GATEWAY (YARP)                              │
│                    Routes requests to correct service                   │
│                         Port: 5142                                      │
└─────────────────────────────────────────────────────────────────────────┘
            │
            ├──────────────────────┬──────────────────────┐
            │                      │                      │
            ▼                      ▼                      ▼
┌───────────────────┐  ┌───────────────────┐  ┌───────────────────┐
│   Identity API    │  │   Future APIs     │  │   Future APIs   │
│  (Authentication) │  │  (Visitor API,    │  │  (Compliance    │
│   Port: 5211      │  │   Gate API, etc.) │  │   API, etc.)    │
└─────────┬─────────┘  └───────────────────┘  └───────────────────┘
          │
          │ Uses
          ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        DATA & INFRASTRUCTURE                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌─────────────┐ │
│  │  SQL Server  │  │    Redis     │  │ Blob Storage │  │   Loki      │ │
│  │  (Data)      │  │  (Cache)     │  │  (Files)     │  │  (Logs)     │ │
│  └──────────────┘  └──────────────┘  └──────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
```

### Key Concepts for New Developers

**🔐 Identity API** — This is where users log in. It creates JWT tokens (like a digital ID card) that other services check to verify "who are you?"

**🚪 API Gateway** — Think of it as a reception desk. All requests go here first, and it directs them to the right department (service).

**🗄️ SQL Server** — Our primary database. Stores users, visitors, gates, audit logs — everything that needs to persist.

**⚡ Redis** — Super-fast temporary storage. We use it for things that change quickly or don't need to last forever (like session data or real-time locations).

---

## 🛠️ Common Development Tasks

### Checking if Services Are Healthy

Each service exposes a health check endpoint:

| Service | Health URL | What It Checks |
|---------|------------|----------------|
| Gateway | http://localhost:5142/health | Can reach downstream services |
| Identity API | http://localhost:5211/health | Database and Redis connectivity |
| Admin Portal | http://localhost:5162/health | Basic liveness |

### Viewing API Documentation

Our APIs use Scalar (a modern API docs tool):

1. Open Aspire Dashboard
2. Find **🔐 Identity API**
3. Click **📖 API Documentation (Scalar)**
4. You'll see all available endpoints, request/response schemas, and can test directly

### Reading Application Logs

We use **Grafana + Loki** for centralized logging:

1. Open Aspire Dashboard
2. Find **📈 Grafana**
3. Click **Open Grafana (No login required)**
4. Go to **Explore** in the left sidebar
5. Select **Loki** as the data source
6. Try these queries:
   - `{job="identity-api"}` — All logs from the Identity API
   - `{level="Error"}` — Only errors across all services
   - `{job="gateway"} |= "/api/auth"` — Gateway logs mentioning authentication

### Viewing Metrics and Performance

Use **Grafana + Prometheus** for metrics:

1. Open **📈 Grafana**
2. Go to **Explore**
3. Select **Prometheus** as the data source
4. Try these queries:
   - `up` — Shows which services are running (1 = up, 0 = down)
   - `http_requests_total` — Total HTTP requests
   - `process_cpu_seconds_total` — CPU usage

---

## 🔧 Troubleshooting (When Things Go Wrong)

### "Docker is not running"

**Problem:** You see an error about Docker not being available.  
**Solution:** Start Docker Desktop and wait for the whale icon to appear in your system tray.

### "Port is already in use"

**Problem:** Another program is using one of our ports (5142, 5211, etc.).  
**Solution:** Either close the other program, or change the port in `launchSettings.json` temporarily.

### "Service failed to start" (Red in Aspire Dashboard)

**Problem:** A service crashed during startup.  
**Solution:**
1. Click on the service in the Aspire Dashboard
2. Check the **Console** or **Structured Logs** tab
3. Look for the error message (usually near the bottom)
4. Common fixes:
   - Database connection issues → Make sure SQL Server container is green
   - Missing migrations → Run `dotnet ef database update` in the API project
   - Port conflicts → Change ports in launchSettings.json

### Can't Access Grafana / Prometheus

**Problem:** Browser says "This site can't be reached"  
**Solution:**
1. Check that Docker containers are running (whale icon in system tray)
2. In Aspire Dashboard, look for the **📈 Grafana** resource
3. Wait for it to turn green (containers take ~30 seconds to start)
4. Try clicking the URL again

### Database Connection Errors

**Problem:** API logs say "Cannot connect to SQL Server"  
**Solution:**
1. In Aspire Dashboard, check that **🗄️ SQL Server** is green
2. Check that **📦 GACS Database** was created
3. If needed, restart the AppHost: press `Ctrl+C` then `dotnet run` again

---

## 📝 Project Structure (Where Things Live)

```
GACS/
├── aspire/
│   └── GACS.AppHost/          ← Start here! (dotnet run)
│       ├── Program.cs         ← Defines all services
│       └── observability/     ← Prometheus, Grafana, Loki configs
│
├── src/
│   ├── GACS.Web.Admin/        ← 🖥️ Admin Portal (Blazor)
│   ├── GACS.Gateway/          ← 🚪 API Gateway
│   ├── GACS.IdentityTenant.Api/ ← 🔐 Authentication API
│   ├── GACS.Components/       ← 🧩 Shared UI components
│   └── GACS.Mobile.Guard/     ← 📱 Guard tablet app (MAUI)
│
├── docs/
│   ├── DEVELOPER-ONBOARDING.md ← 📖 You are here!
│   ├── FRONTEND-ARCHITECTURE.md
│   ├── BACKEND-ARCHITECTURE.md
│   ├── DATA-ARCHITECTURE.md
│   └── MASTER-ARCHITECTURE.md
│
└── GACS.slnx                  ← Solution file (open in VS)
```

---

## 📚 Documentation You Should Read

After getting the app running, read these in order:

1. **FRONTEND-ARCHITECTURE.md** — How we build UIs (Blazor, Fluent UI, design tokens)
2. **BACKEND-ARCHITECTURE.md** — API patterns, authentication, microservices
3. **DATA-ARCHITECTURE.md** — Database design, POPIA compliance, data retention
4. **MASTER-ARCHITECTURE.md** — High-level system overview

---

## 🧪 Your First Code Change (Practice)

Let's make a small change to verify everything works:

1. Open `src/GACS.Web.Admin/Pages/Home.razor`
2. Find the `<h1>` tag
3. Change the text to: `<h1>Welcome to GACS, [Your Name]!</h1>`
4. Save the file
5. The browser should auto-refresh (hot reload)
6. See your name on the page? ✅ Success!

Revert the change before committing:

```bash
git checkout src/GACS.Web.Admin/Pages/Home.razor
```

---

## 💬 Getting Help

Stuck? Here's how to get unstuck:

1. **Check the logs** — Aspire Dashboard > Click service > Console/Logs tabs
2. **Ask in Teams** — Post in #dev-help channel with:
   - What you were trying to do
   - The exact error message
   - What you've already tried
3. **Pair program** — Grab a senior dev for 15 minutes
4. **Read the architecture docs** — They're in `/docs`

---

## ✅ Onboarding Checklist

Before you start real work, complete these:

- [ ] Prerequisites installed (.NET 10, Docker, Git, IDE)
- [ ] Repository cloned and building
- [ ] Ran `dotnet dev-certs https --trust`
- [ ] Aspire Dashboard opens successfully
- [ ] All services show green (or yellow then green)
- [ ] Opened Admin Portal and saw the landing page
- [ ] Viewed API docs at Scalar endpoint
- [ ] Made a test code change and saw it hot-reload
- [ ] Read FRONTEND-ARCHITECTURE.md
- [ ] Read BACKEND-ARCHITECTURE.md (at least the overview)

**Welcome to the team!** 🚀

---

## 🔄 Quick Reference Commands

```bash
# Start everything
cd aspire/GACS.AppHost && dotnet run

# Stop everything (in the AppHost terminal)
Ctrl+C

# Reset everything (if things get weird)
docker system prune -f  # Removes all containers and volumes
dotnet run              # Fresh start

# View logs for a specific service
docker logs <container-name>

# Check which ports are in use (Windows)
netstat -ano | findstr :5142

# Check which ports are in use (PowerShell)
Get-NetTCPConnection -LocalPort 5142
```

---

*Last updated: 2026-05-25*  
*Questions? Ask in #dev-help or your onboarding buddy.*
