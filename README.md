# Travel Advisor API

A REST API that helps users make travel decisions based on weather and air quality data for districts in Bangladesh.

---

## Table of Contents

1. [Project Setup](#1-project-setup)
   - [Prerequisites](#prerequisites)
   - [Installation](#installation)
   - [Running the Application](#running-the-application)
   - [API Endpoints](#api-endpoints)
   - [Stopping the Application](#stopping-the-application)
   - [Troubleshooting](#troubleshooting)

2. [Architecture Details](#2-architecture-details)
   - [Clean Architecture](#clean-architecture)
   - [Project Structure](#project-structure)
   - [CQRS Pattern](#cqrs-pattern)
   - [Data Flow](#data-flow)

3. [Testing](#3-testing)
   - [Running Tests](#running-tests)
   - [Test Coverage](#test-coverage)
   - [Test Structure](#test-structure)

4. [Logging & Monitoring](#4-logging--monitoring)
   - [Serilog](#serilog)
   - [Seq Log Viewer](#seq-log-viewer)
   - [Log Configuration](#log-configuration)

5. [Documentation](#5-documentation)
   - [Technologies Used](#technologies-used)
   - [Why These Technologies](#why-these-technologies)
   - [Configuration Reference](#configuration-reference)

---

# 1. Project Setup

## Prerequisites

Before you begin, ensure you have the following installed on your machine:

### 1.1 .NET 10.0 SDK

**Check if .NET is installed:**
```bash
dotnet --version
```

If not installed, download from: https://dotnet.microsoft.com/download/dotnet/10.0

**For macOS (using Homebrew):**
```bash
brew install dotnet-sdk
```

**For Windows:**
1. Go to https://dotnet.microsoft.com/download/dotnet/10.0
2. Click "Download .NET SDK x64" (or arm64 for Apple Silicon)
3. Run the installer and follow the prompts

**For Linux (Ubuntu/Debian):**
```bash
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0
```

### 1.2 Docker Desktop

Docker is required to run Redis (caching), PostgreSQL (database), and Seq (log viewer).

**For macOS:**
1. Go to https://www.docker.com/products/docker-desktop/
2. Click "Download for Mac"
3. Open the downloaded `.dmg` file
4. Drag Docker to your Applications folder
5. Open Docker from Applications
6. Wait for Docker to start (you'll see the whale icon in your menu bar)

**For Windows:**
1. Go to https://www.docker.com/products/docker-desktop/
2. Click "Download for Windows"
3. Run the installer
4. Follow the installation wizard
5. Restart your computer if prompted
6. Open Docker Desktop from the Start menu

**For Linux:**
```bash
sudo apt-get update
sudo apt-get install docker.io docker-compose
sudo systemctl start docker
sudo systemctl enable docker
```

**Verify Docker is running:**
```bash
docker --version
```
You should see something like: `Docker version 24.0.0, build ...`

---

## Installation

### Step 1: Get the Source Code

**Option A - Clone from Git:**
```bash
git clone <repository-url>
cd TravelAdvisor
```

**Option B - Extract from ZIP:**
1. Extract the ZIP file to a folder
2. Open Terminal (macOS/Linux) or Command Prompt (Windows)
3. Navigate to the extracted folder:
```bash
cd path/to/TravelAdvisor
```

### Step 2: Verify Project Structure

Make sure you're in the correct directory. You should see these files:
```
TravelAdvisor/
├── docker-compose.yml
├── README.md
├── TravelAdvisor.sln
├── coverlet.runsettings
├── src/
│   ├── TravelAdvisor.Api/
│   ├── TravelAdvisor.Application/
│   ├── TravelAdvisor.Domain/
│   └── TravelAdvisor.Infrastructure/
└── tests/
    └── TravelAdvisor.Tests/
```

### Step 3: Restore NuGet Packages

This downloads all the required libraries:
```bash
dotnet restore
```

**Expected output:**
```
Determining projects to restore...
Restored /path/to/TravelAdvisor.Domain.csproj
Restored /path/to/TravelAdvisor.Application.csproj
Restored /path/to/TravelAdvisor.Infrastructure.csproj
Restored /path/to/TravelAdvisor.Api.csproj
Restored /path/to/TravelAdvisor.Tests.csproj
```

### Step 4: Build the Project

```bash
dotnet build
```

**Expected output:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

If you see errors, make sure you have .NET 10.0 SDK installed.

---

## Running the Application

### Step 1: Start Docker Services (Redis, PostgreSQL & Seq)

Open a terminal and navigate to the project root folder (where `docker-compose.yml` is located):

```bash
docker-compose up -d
```

**What this does:**
- Downloads Redis, PostgreSQL, and Seq images (first time only)
- Starts a Redis container named `traveladvisor-redis` at `localhost:6379`
- Starts a PostgreSQL container named `traveladvisor-postgres` at `localhost:5432`
- Starts a Seq container named `traveladvisor-seq` at `localhost:5341`
- Creates persistent volumes for data storage

**Verify services are running:**
```bash
docker ps
```

You should see:
```
CONTAINER ID   IMAGE                STATUS         PORTS                    NAMES
xxxxxxxxxxxx   redis:alpine         Up X seconds   0.0.0.0:6379->6379/tcp   traveladvisor-redis
xxxxxxxxxxxx   postgres:16-alpine   Up X seconds   0.0.0.0:5432->5432/tcp   traveladvisor-postgres
xxxxxxxxxxxx   datalust/seq:2024.1  Up X seconds   0.0.0.0:5341->80/tcp     traveladvisor-seq
```

### Step 2: Start the API

Open a **new terminal window** (keep Docker services running in the background):

```bash
cd src/TravelAdvisor.Api
dotnet run
```

**What happens on startup:**
1. Database migrations run automatically (creates tables in PostgreSQL)
2. District sync job runs (fetches 64 Bangladesh districts and stores in DB)
3. Cache warming job runs (pre-loads all data into Redis)
4. API starts accepting requests (all data ready, instant responses)

**Expected output:**
```
[14:30:45 INF] Starting TravelAdvisor API
[14:30:46 INF] Starting district sync job
[14:30:47 INF] District sync completed. Added 64 districts.
[14:30:48 INF] Cache warmup completed successfully
[14:30:48 INF] Now listening on: http://localhost:5155
```

**Note:** The port might be different (e.g., 5000, 5155, etc.). Use whatever port is shown in your terminal.

### Step 3: Access the Application

Open your web browser and go to:

| Service | URL | Description |
|---------|-----|-------------|
| **Swagger UI** | http://localhost:5155 | API Documentation & Testing |
| **Hangfire Dashboard** | http://localhost:5155/hangfire | Background Jobs Monitor |
| **Seq Log Viewer** | http://localhost:5341 | Structured Log Viewer |

Replace `5155` with your actual port number if different.

---

## API Endpoints

### 1. Get Top Districts

Returns the top 10 coolest districts with the best air quality.

**Endpoint:**
```
GET /api/v1/Travel/top-districts?count=10
```

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| count | integer | No | Number of districts to return (default: 10, max: 64) |

**Example Request:**
```bash
curl http://localhost:5155/api/v1/Travel/top-districts?count=10
```

**Example Response:**
```json
{
  "success": true,
  "data": {
    "districts": [
      {
        "rank": 1,
        "name": "Cox's Bazar",
        "latitude": 21.4272,
        "longitude": 92.0058,
        "temperatureAt2pm": 24.5,
        "pm25Level": 35.2
      }
    ],
    "generatedAt": "2026-01-28T14:00:00Z",
    "forecastPeriod": "2026-01-28 to 2026-02-03"
  },
  "message": null,
  "errors": null,
  "statusCode": 200
}
```

### 2. Get Travel Recommendation

Compares your current location with a destination and provides a travel recommendation.

**Endpoint:**
```
POST /api/v1/Travel/recommendation
```

**Request Body:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| currentLatitude | double | Yes | Your current latitude (20.5 to 26.7 - Bangladesh bounds) |
| currentLongitude | double | Yes | Your current longitude (88.0 to 92.7 - Bangladesh bounds) |
| destinationDistrict | string | Yes | Name of destination district in Bangladesh |
| travelDate | string | Yes | Travel date (YYYY-MM-DD, within next 7 days) |

**Example Request:**
```bash
curl -X POST http://localhost:5155/api/v1/Travel/recommendation \
  -H "Content-Type: application/json" \
  -d '{
    "currentLatitude": 23.7115,
    "currentLongitude": 90.4111,
    "destinationDistrict": "Cox'\''s Bazar",
    "travelDate": "2026-01-31"
  }'
```

**Example Response (Recommended):**
```json
{
  "success": true,
  "data": {
    "recommendation": "Recommended",
    "reason": "Your destination is 3°C cooler and has significantly better air quality. Enjoy your trip!",
    "currentLocation": {
      "name": "Dhaka",
      "latitude": 23.7115,
      "longitude": 90.4111,
      "temperatureAt2pm": 28.5,
      "pm25Level": 85.3
    },
    "destination": {
      "name": "Cox's Bazar",
      "latitude": 21.4272,
      "longitude": 92.0058,
      "temperatureAt2pm": 25.5,
      "pm25Level": 42.1
    }
  },
  "message": null,
  "errors": null,
  "statusCode": 200
}
```

---

## Stopping the Application

### Stop the API
Press `Ctrl+C` in the terminal where the API is running.

### Stop Docker Services
```bash
docker-compose down
```

### Stop Docker Services and remove all data
```bash
docker-compose down -v
```
**Warning:** This removes all cached data, logs, and the district database. On next startup, districts will be re-fetched from the external API.

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| "Docker command not found" | Install Docker Desktop from https://docker.com and make sure it's running |
| "Port 6379 is already in use" | Run `docker stop traveladvisor-redis && docker rm traveladvisor-redis` then `docker-compose up -d` |
| "Port 5432 is already in use" | Run `docker stop traveladvisor-postgres && docker rm traveladvisor-postgres` then `docker-compose up -d` |
| "Port 5155 is already in use" | Stop the other application or change port in `launchSettings.json` |
| "dotnet: command not found" | Install .NET 10.0 SDK from https://dotnet.microsoft.com/download/dotnet/10.0 |
| "Build failed with errors" | Run `dotnet restore` then `dotnet build` |
| "Redis connection failed" | Run `docker ps` to check if Redis is running, if not run `docker-compose up -d` |
| "PostgreSQL connection refused" | Run `docker ps` to check if PostgreSQL is running, if not run `docker-compose up -d` |
| "Seq keeps restarting" | On Apple Silicon, use `platform: linux/amd64` in docker-compose.yml |
| API returns empty data | Wait for startup to complete (district sync + cache warming) |
| "Latitude/Longitude validation error" | Coordinates must be within Bangladesh bounds (Lat: 20.5-26.7, Long: 88.0-92.7) |

---

# 2. Architecture Details

## Clean Architecture

This project follows **Clean Architecture** (also known as Onion Architecture), which organizes code into layers with strict dependency rules.

```
┌─────────────────────────────────────────────────────────────┐
│                        API Layer                             │
│              (Controllers, Middleware, Configuration)        │
├─────────────────────────────────────────────────────────────┤
│                   Infrastructure Layer                       │
│    (PostgreSQL, Redis, HTTP Clients, Hangfire, EF Core)     │
├─────────────────────────────────────────────────────────────┤
│                    Application Layer                         │
│            (CQRS Handlers, DTOs, Validators, Services)      │
├─────────────────────────────────────────────────────────────┤
│                      Domain Layer                            │
│              (Entities, Interfaces, Constants)               │
└─────────────────────────────────────────────────────────────┘
```

### Dependency Rule

Dependencies flow **inward only**:
- API → Infrastructure → Application → Domain
- Inner layers know nothing about outer layers
- Domain layer has zero dependencies

### Benefits

| Benefit | Description |
|---------|-------------|
| **Testability** | Business logic can be tested without databases or external services |
| **Maintainability** | Changes in one layer don't affect other layers |
| **Flexibility** | Easy to swap implementations (e.g., Redis to Memcached) |
| **Separation of Concerns** | Each layer has a single responsibility |

---

## Project Structure

```
TravelAdvisor/
├── docker-compose.yml                 # Docker configuration
├── coverlet.runsettings               # Code coverage configuration
├── README.md                          # This documentation
├── TravelAdvisor.sln                  # Solution file
├── src/
│   ├── TravelAdvisor.Domain/          # Core business entities
│   │   ├── Common/
│   │   │   └── Constants.cs           # Application-wide constants
│   │   ├── Entities/
│   │   │   └── District.cs            # District entity
│   │   └── Exceptions/                # Custom domain exceptions
│   │       ├── BadRequestException.cs
│   │       ├── DomainException.cs
│   │       ├── ExternalServiceException.cs
│   │       ├── NotFoundException.cs
│   │       └── ValidationException.cs
│   │
│   ├── TravelAdvisor.Application/     # Business logic layer
│   │   ├── Common/
│   │   │   ├── Interfaces/            # Service contracts
│   │   │   ├── Models/                # Response models (ApiResponse)
│   │   │   └── Validators/            # FluentValidation validators
│   │   ├── DTOs/                      # Data Transfer Objects
│   │   └── Features/                  # CQRS Queries & Handlers
│   │       ├── TopDistricts/
│   │       └── TravelRecommendation/
│   │
│   ├── TravelAdvisor.Infrastructure/  # External services layer
│   │   ├── BackgroundJobs/            # Hangfire jobs
│   │   │   ├── CacheWarmingJob.cs
│   │   │   └── DistrictSyncJob.cs
│   │   ├── Caching/                   # Redis cache service
│   │   ├── Configuration/             # Settings classes
│   │   ├── ExternalApis/              # HTTP clients for external APIs
│   │   ├── Mapping/                   # AutoMapper profiles
│   │   ├── Migrations/                # EF Core database migrations
│   │   └── Persistence/               # DbContext
│   │
│   └── TravelAdvisor.Api/             # Web API layer
│       ├── Controllers/               # API endpoints
│       ├── Middleware/                # Global exception handler
│       ├── appsettings.json           # Configuration
│       └── Program.cs                 # Application entry point
│
└── tests/
    └── TravelAdvisor.Tests/           # Unit tests
        ├── BackgroundJobs/            # Background job tests
        ├── Common/                    # Test helpers
        ├── Controllers/               # Controller tests
        ├── Exceptions/                # Exception tests
        ├── Handlers/                  # Query handler tests
        ├── Middleware/                # Middleware tests
        ├── Services/                  # Service tests
        └── Validators/                # Validator tests
```

---

## CQRS Pattern

**CQRS (Command Query Responsibility Segregation)** separates read operations (Queries) from write operations (Commands).

### How It Works in This Project

```
┌──────────────┐     ┌──────────────┐     ┌──────────────────────┐
│  Controller  │ ──► │   MediatR    │ ──► │   Query Handler      │
│              │     │  (Mediator)  │     │                      │
│ POST /travel │     │              │     │ GetTravelRecommend-  │
│              │     │  Dispatches  │     │ ationQueryHandler    │
└──────────────┘     │  to Handler  │     └──────────────────────┘
                     └──────────────┘
```

### Benefits

| Benefit | Description |
|---------|-------------|
| **Single Responsibility** | Each handler does one thing |
| **Testability** | Handlers can be unit tested in isolation |
| **Scalability** | Queries and commands can be optimized separately |
| **Maintainability** | Easy to find and modify specific features |

---

## Data Flow

### Request Flow

```
┌────────┐    ┌────────────┐    ┌───────────┐    ┌─────────────┐    ┌───────────┐
│ Client │───►│ Controller │───►│  MediatR  │───►│   Handler   │───►│  Services │
└────────┘    └────────────┘    └───────────┘    └─────────────┘    └───────────┘
                                                                           │
                                                                           ▼
┌────────┐    ┌────────────┐    ┌───────────┐    ┌─────────────┐    ┌───────────┐
│ Client │◄───│ Controller │◄───│  MediatR  │◄───│   Handler   │◄───│   Cache   │
└────────┘    └────────────┘    └───────────┘    └─────────────┘    └───────────┘
```

### Data Storage Strategy

```
┌─────────────────────────────────────────────────────────────────────┐
│                    District Sync Job (Monthly)                       │
├─────────────────────────────────────────────────────────────────────┤
│  1. Fetch all 64 districts from external API                        │
│  2. Store districts in PostgreSQL database                          │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                  Cache Warming Job (Every 15 min)                    │
├─────────────────────────────────────────────────────────────────────┤
│  1. Read districts from PostgreSQL                                  │
│  2. Fetch weather data for all districts (batch API call)           │
│  3. Fetch air quality data for all districts (batch API call)       │
│  4. Store everything in Redis cache                                 │
│  5. Pre-compute and cache top districts ranking                     │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         API Request                                  │
├─────────────────────────────────────────────────────────────────────┤
│  1. Read from Redis cache only (< 50ms response)                    │
│  2. All data pre-loaded by background jobs                          │
│  3. No external API calls during user requests                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

# 3. Testing

## Running Tests

### Run All Tests
```bash
dotnet test
```

**Expected output:**
```
Passed!  - Failed: 0, Passed: 88, Skipped: 0, Total: 88, Duration: 250 ms
```

### Run Tests with Detailed Output
```bash
dotnet test --verbosity normal
```

### Run Tests with Code Coverage
```bash
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings
```

Coverage reports will be generated in `TestResults/` folder.

### Generate HTML Coverage Report
```bash
# Install report generator (one time)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator -reports:./TestResults/**/coverage.cobertura.xml -targetdir:./CoverageReport -reporttypes:Html

# Open report
open CoverageReport/index.html   # macOS
start CoverageReport/index.html  # Windows
```

---

## Test Coverage

| Category | Tests | Description |
|----------|-------|-------------|
| **Validators** | 13 | Request validation (coordinates, dates, count limits) |
| **Handlers** | 9 | Query handler logic (caching, ranking, recommendations) |
| **Services** | 19 | DistrictService, WeatherService, AirQualityService |
| **Controllers** | 6 | API endpoint tests |
| **Middleware** | 8 | Global exception handler tests |
| **Exceptions** | 9 | Domain exception tests |
| **BackgroundJobs** | 10 | CacheWarmingJob, DistrictSyncJob tests |
| **Total** | **88** | |

### Coverage Exclusions

The following are excluded from coverage reports (configured in `coverlet.runsettings`):
- `Program.cs` - Entry point
- `*.DTOs.*` - Simple data transfer objects
- `*.Models.*` - Simple data models
- `*.Entities.*` - Entity classes
- `*.Configuration.*` - Settings classes
- `*.Migrations.*` - EF Core migrations
- `*MappingProfile` - AutoMapper profiles

---

## Test Structure

```
tests/TravelAdvisor.Tests/
├── BackgroundJobs/
│   ├── CacheWarmingJobTests.cs      # Cache warming tests
│   └── DistrictSyncJobTests.cs      # District sync tests
├── Common/
│   └── MockHttpMessageHandler.cs    # HTTP mocking helper
├── Controllers/
│   └── TravelControllerTests.cs     # Controller tests
├── Exceptions/
│   └── DomainExceptionTests.cs      # Exception tests
├── Handlers/
│   ├── GetTopDistrictsQueryHandlerTests.cs
│   └── GetTravelRecommendationQueryHandlerTests.cs
├── Middleware/
│   └── GlobalExceptionHandlerTests.cs
├── Services/
│   ├── AirQualityServiceTests.cs
│   ├── DistrictServiceTests.cs
│   └── WeatherServiceTests.cs
└── Validators/
    ├── GetTopDistrictsRequestValidatorTests.cs
    └── TravelRecommendationRequestValidatorTests.cs
```

### Test Packages

| Package | Description |
|---------|-------------|
| **xUnit** | Test framework |
| **Moq** | Mocking library |
| **FluentAssertions** | Assertion library |
| **Microsoft.EntityFrameworkCore.InMemory** | In-memory database for testing |
| **coverlet.collector** | Code coverage collection |

---

# 4. Logging & Monitoring

## Serilog

The application uses **Serilog** for structured logging with multiple sinks.

### Features

| Feature | Description |
|---------|-------------|
| **Console Logging** | Colored, formatted console output |
| **File Logging** | Rolling daily logs in `Logs/` folder |
| **Seq Integration** | Structured log viewer with search & filtering |
| **Request Logging** | HTTP method, path, status code, duration |
| **Enrichers** | Machine name, environment, thread ID |

### Log Levels

| Level | Usage |
|-------|-------|
| **Debug** | Detailed debugging information |
| **Information** | General operational events |
| **Warning** | Validation errors, potential issues |
| **Error** | Exceptions, failures |
| **Fatal** | Application crashes |

### Log Output Example

**Console:**
```
[14:30:45 INF] Starting TravelAdvisor API
[14:30:46 INF] HTTP GET /api/v1/travel/top-districts responded 200 in 45.23 ms
[14:30:47 WRN] Validation failed: Latitude must be within Bangladesh bounds
[14:30:48 ERR] External service 'WeatherAPI' failed
```

**File:** `Logs/traveladvisor-2026-01-30.log`
```
2026-01-30 14:30:45.123 +06:00 [INF] Starting TravelAdvisor API
2026-01-30 14:30:46.456 +06:00 [INF] HTTP GET /api/v1/travel/top-districts responded 200 in 45.23 ms
```

---

## Seq Log Viewer

**Seq** is a centralized log viewer with powerful search and filtering capabilities.

### Accessing Seq

1. Start Docker services: `docker-compose up -d`
2. Open browser: http://localhost:5341

### Features

| Feature | Description |
|---------|-------------|
| **Real-time Logs** | See logs as they happen |
| **Full-text Search** | Search across all log properties |
| **Filtering** | Filter by level, source, time range |
| **SQL-like Queries** | `@Level = 'Error' and RequestPath like '/api%'` |
| **Dashboards** | Create custom dashboards |
| **Alerts** | Set up alerts for specific conditions |

### Example Queries

```sql
-- All errors
@Level = 'Error'

-- Validation failures
@Message like '%Validation failed%'

-- Slow requests (> 1 second)
Elapsed > 1000

-- Specific endpoint
RequestPath = '/api/v1/Travel/top-districts'

-- Errors in the last hour
@Level = 'Error' and @Timestamp > Now() - 1h
```

---

## Log Configuration

### Production (appsettings.json)

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "Hangfire": "Information"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "Logs/traveladvisor-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7
        }
      }
    ]
  }
}
```

### Development (appsettings.Development.json)

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    },
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File" },
      {
        "Name": "Seq",
        "Args": { "serverUrl": "http://localhost:5341" }
      }
    ]
  }
}
```

### Log File Retention

| Environment | Retention | Location |
|-------------|-----------|----------|
| Production | 7 days | `Logs/traveladvisor-{date}.log` |
| Development | 3 days | `Logs/traveladvisor-debug-{date}.log` |

---

# 5. Documentation

## Technologies Used

### Framework & Runtime

| Technology | Version | Description |
|------------|---------|-------------|
| .NET | 10.0 | Cross-platform runtime and SDK |
| ASP.NET Core | 10.0 | Web API framework |
| C# | 13.0 | Programming language |

### NuGet Packages

#### API Layer
| Package | Version | Description |
|---------|---------|-------------|
| Asp.Versioning.Mvc | 8.1.1 | API versioning support |
| Hangfire.AspNetCore | 1.8.22 | Background job dashboard |
| Serilog.AspNetCore | 10.0.0 | Structured logging |
| Serilog.Sinks.Seq | 9.0.0 | Seq log sink |
| Swashbuckle.AspNetCore | 10.1.0 | Swagger UI |

#### Application Layer
| Package | Version | Description |
|---------|---------|-------------|
| AutoMapper | 12.0.1 | Object mapping |
| FluentValidation | 12.1.1 | Request validation |
| MediatR | 14.0.0 | CQRS mediator |

#### Infrastructure Layer
| Package | Version | Description |
|---------|---------|-------------|
| Hangfire.Redis.StackExchange | 1.12.0 | Redis storage for Hangfire |
| Microsoft.EntityFrameworkCore | 10.0.0 | ORM |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.0 | PostgreSQL provider |
| StackExchange.Redis | 2.10.1 | Redis client |
| Polly | 8.x | Resilience policies |

#### Test Layer
| Package | Version | Description |
|---------|---------|-------------|
| xUnit | 2.9.3 | Test framework |
| Moq | 4.20.72 | Mocking library |
| FluentAssertions | 8.8.0 | Assertion library |
| coverlet.collector | 6.0.4 | Code coverage |

### External Services

| Service | Port | Description |
|---------|------|-------------|
| PostgreSQL | 5432 | Relational database |
| Redis | 6379 | In-memory cache |
| Seq | 5341 | Log viewer |
| Open-Meteo Weather API | - | Weather forecasts |
| Open-Meteo Air Quality API | - | Air quality data |

---

## Why These Technologies

### Serilog

**What is it?**
A diagnostic logging library for .NET with structured logging support.

**Why we use it:**
| Reason | Explanation |
|--------|-------------|
| **Structured Logging** | Log properties, not just strings |
| **Multiple Sinks** | Console, File, Seq, and more |
| **Performance** | Asynchronous, non-blocking |
| **Filtering** | Fine-grained control over log levels |

### Seq

**What is it?**
A centralized log server with powerful search capabilities.

**Why we use it:**
| Reason | Explanation |
|--------|-------------|
| **Search** | Full-text search across all logs |
| **Filtering** | SQL-like query language |
| **Real-time** | Live log streaming |
| **Free** | Free for single-user development |

### xUnit + Moq + FluentAssertions

**Why this testing stack:**
| Tool | Benefit |
|------|---------|
| **xUnit** | Modern, extensible, parallel test execution |
| **Moq** | Easy mocking of dependencies |
| **FluentAssertions** | Readable assertions: `result.Should().Be(expected)` |

---

## Configuration Reference

### appsettings.json

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File", "Serilog.Sinks.Seq"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Hangfire": "Information"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "Logs/traveladvisor-.log" } }
    ]
  },
  "ConnectionStrings": {
    "Redis": "localhost:6379",
    "PostgreSQL": "Host=localhost;Port=5432;Database=traveladvisor;..."
  },
  "ApiSettings": {
    "DistrictsUrl": "https://raw.githubusercontent.com/.../bd-districts.json",
    "WeatherApiBaseUrl": "https://api.open-meteo.com/v1/forecast",
    "AirQualityApiBaseUrl": "https://air-quality-api.open-meteo.com/v1/air-quality"
  },
  "CacheSettings": {
    "DefaultExpirationMinutes": 15,
    "WeatherCacheMinutes": 30,
    "CacheWarmingCronSchedule": "*/15 * * * *",
    "DistrictSyncCronSchedule": "0 0 1 * *"
  }
}
```

### Docker Compose Services

| Service | Image | Port | Purpose |
|---------|-------|------|---------|
| redis | redis:alpine | 6379 | Caching |
| postgres | postgres:16-alpine | 5432 | Database |
| seq | datalust/seq:2024.1 | 5341 | Log viewer |

---

## Support

If you encounter any issues:

1. Check **Seq** at http://localhost:5341 for error logs
2. Check the **Hangfire Dashboard** at `/hangfire` for background job errors
3. Check **application logs** in the terminal or `Logs/` folder
4. Run **tests** to verify setup: `dotnet test`
5. Check **Docker services**: `docker ps`
6. Check **container logs**: `docker logs traveladvisor-redis`

### Useful Commands

```bash
# View all logs
docker-compose logs -f

# View API logs only
docker logs traveladvisor-seq

# Check service health
curl http://localhost:5155/health

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Reset everything
docker-compose down -v && docker-compose up -d
```
