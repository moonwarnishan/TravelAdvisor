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

3. [Documentation](#3-documentation)
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

Docker is required to run Redis (the caching database).

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
└── src/
    ├── TravelAdvisor.Api/
    ├── TravelAdvisor.Application/
    ├── TravelAdvisor.Domain/
    └── TravelAdvisor.Infrastructure/
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

### Step 1: Start Redis Database

Open a terminal and navigate to the project root folder (where `docker-compose.yml` is located):

```bash
docker-compose up -d
```

**What this does:**
- Downloads the Redis image (first time only)
- Starts a Redis container named `traveladvisor-redis`
- Redis will be available at `localhost:6379`

**Verify Redis is running:**
```bash
docker ps
```

You should see:
```
CONTAINER ID   IMAGE          STATUS         PORTS                    NAMES
xxxxxxxxxxxx   redis:alpine   Up X seconds   0.0.0.0:6379->6379/tcp   traveladvisor-redis
```

### Step 2: Start the API

Open a **new terminal window** (keep Redis running in the background):

```bash
cd src/TravelAdvisor.Api
dotnet run
```

**Expected output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5155
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

**Note:** The port might be different (e.g., 5000, 5155, etc.). Use whatever port is shown in your terminal.

### Step 3: Access the Application

Open your web browser and go to:

- **Swagger UI (API Documentation):** http://localhost:5155
- **Hangfire Dashboard (Background Jobs):** http://localhost:5155/hangfire

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
| count | integer | No | Number of districts to return (default: 10, max: 50) |

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
    "travelDate": "2026-01-28"
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
| currentLatitude | double | Yes | Your current latitude (-90 to 90) |
| currentLongitude | double | Yes | Your current longitude (-180 to 180) |
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
    "travelDate": "2026-01-29"
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

**Example Response (Not Recommended):**
```json
{
  "success": true,
  "data": {
    "recommendation": "Not Recommended",
    "reason": "Your destination is hotter and has worse air quality than your current location. It's better to stay where you are."
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

### Stop Redis
```bash
docker-compose down
```

### Stop Redis and remove data
```bash
docker-compose down -v
```

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| "Docker command not found" | Install Docker Desktop from https://docker.com and make sure it's running |
| "Port 6379 is already in use" | Run `docker stop traveladvisor-redis && docker rm traveladvisor-redis` then `docker-compose up -d` |
| "Port 5155 is already in use" | Stop the other application or change port in `launchSettings.json` |
| "dotnet: command not found" | Install .NET 10.0 SDK from https://dotnet.microsoft.com/download/dotnet/10.0 |
| "Build failed with errors" | Run `dotnet restore` then `dotnet build` |
| "Redis connection failed" | Run `docker ps` to check if Redis is running, if not run `docker-compose up -d` |
| API returns empty data | Wait 15 seconds for cache warming job to complete |

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
│         (Redis, HTTP Clients, Hangfire, External APIs)      │
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
├── docker-compose.yml                 # Docker configuration for Redis
├── README.md                          # This documentation
├── TravelAdvisor.sln                  # Solution file
└── src/
    ├── TravelAdvisor.Domain/          # Core business entities
    │   ├── Common/
    │   │   └── Constants.cs           # Application-wide constants
    │   ├── Entities/
    │   │   └── District.cs            # District entity
    │   └── Exceptions/                # Custom domain exceptions
    │
    ├── TravelAdvisor.Application/     # Business logic layer
    │   ├── Common/
    │   │   ├── Interfaces/            # Service contracts
    │   │   ├── Models/                # Response models (ApiResponse)
    │   │   └── Validators/            # FluentValidation validators
    │   ├── DTOs/                      # Data Transfer Objects
    │   └── Features/                  # CQRS Queries & Handlers
    │       ├── TopDistricts/
    │       └── TravelRecommendation/
    │
    ├── TravelAdvisor.Infrastructure/  # External services layer
    │   ├── BackgroundJobs/            # Hangfire jobs
    │   ├── Caching/                   # Redis cache service
    │   ├── Configuration/             # Settings classes
    │   ├── ExternalApis/              # HTTP clients for external APIs
    │   └── Mapping/                   # AutoMapper profiles
    │
    └── TravelAdvisor.Api/             # Web API layer
        ├── Controllers/               # API endpoints
        ├── Middleware/                # Global exception handler
        └── Program.cs                 # Application entry point
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

### Example Flow

1. **Controller** receives HTTP request
2. **Controller** creates a Query object
3. **MediatR** dispatches Query to appropriate Handler
4. **Handler** processes business logic and returns result
5. **Controller** wraps result in ApiResponse

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

### Caching Strategy

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Cache Warming Job                             │
│                    (Runs every 15 minutes)                          │
├─────────────────────────────────────────────────────────────────────┤
│  1. Fetch all 64 districts from external API                        │
│  2. Fetch weather data for all districts (batch API call)           │
│  3. Fetch air quality data for all districts (batch API call)       │
│  4. Store everything in Redis cache                                 │
│  5. Pre-compute and cache top 10 ranking                            │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         API Request                                  │
├─────────────────────────────────────────────────────────────────────┤
│  1. Check Redis cache for data                                      │
│  2. If found → Return instantly (< 50ms)                            │
│  3. If not found → Fetch from external API → Cache → Return         │
└─────────────────────────────────────────────────────────────────────┘
```

---

# 3. Documentation

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
| Asp.Versioning.Mvc.ApiExplorer | 8.1.1 | Swagger integration for versioning |
| Hangfire.AspNetCore | 1.8.22 | Background job dashboard UI |
| Microsoft.AspNetCore.OpenApi | 10.0.1 | OpenAPI/Swagger support |
| Swashbuckle.AspNetCore | 10.1.0 | Swagger UI generator |

#### Application Layer
| Package | Version | Description |
|---------|---------|-------------|
| AutoMapper.Extensions.Microsoft.DependencyInjection | 12.0.1 | Object-to-object mapping |
| FluentValidation.DependencyInjectionExtensions | 12.1.1 | Request validation |
| MediatR | 14.0.0 | Mediator pattern for CQRS |

#### Infrastructure Layer
| Package | Version | Description |
|---------|---------|-------------|
| Hangfire.Core | 1.8.22 | Background job processing |
| Hangfire.Redis.StackExchange | 1.12.0 | Redis storage for Hangfire |
| Microsoft.Extensions.Http | 10.0.2 | HTTP client factory |
| Microsoft.Extensions.Options.ConfigurationExtensions | 10.0.2 | Strongly-typed configuration |
| StackExchange.Redis | 2.10.1 | Redis client library |

### External Services

| Service | Description |
|---------|-------------|
| Redis | In-memory cache database |
| Open-Meteo Weather API | Free weather forecast data |
| Open-Meteo Air Quality API | Free air quality data |

---

## Why These Technologies

### Redis

**What is it?**
Redis is an in-memory data store that acts as a super-fast database for temporary data.

**Why we use it:**
| Reason | Explanation |
|--------|-------------|
| **Speed** | Data stored in RAM, reads/writes in microseconds |
| **Performance** | API responses go from ~2 seconds to ~50 milliseconds |
| **Reduced API Calls** | External weather APIs have rate limits; caching prevents hitting them repeatedly |
| **Hangfire Storage** | Stores background job data reliably |

**How it helps:**
```
Without Redis:  User Request → External API (2000ms) → Response
With Redis:     User Request → Redis Cache (5ms) → Response
```

---

### Hangfire

**What is it?**
Hangfire is a library for running background tasks without blocking the main application.

**Why we use it:**
| Reason | Explanation |
|--------|-------------|
| **Cache Warming** | Pre-fetches weather data every 15 minutes before users request it |
| **Non-Blocking** | Heavy tasks run in background, API stays responsive |
| **Reliability** | Failed jobs are automatically retried |
| **Monitoring** | Built-in dashboard at `/hangfire` shows job status |

**How it helps:**
```
Traditional:    User waits for data fetch (slow first request)
With Hangfire:  Data already cached before user asks (always fast)
```

---

### FluentValidation

**What is it?**
A library for building strongly-typed validation rules for request objects.

**Why we use it:**
| Reason | Explanation |
|--------|-------------|
| **Clean Code** | Validation rules separated from business logic |
| **Readable** | Rules read like English sentences |
| **Reusable** | Same validator used across different endpoints |
| **Detailed Errors** | Returns specific error messages to users |

**Example:**
```csharp
RuleFor(x => x.CurrentLatitude)
    .InclusiveBetween(-90, 90)
    .WithMessage("Latitude must be between -90 and 90");
```

**How it helps:**
```
Without Validation:  Invalid data causes crashes deep in code
With Validation:     Invalid data caught early with clear error messages
```

---

### MediatR

**What is it?**
A library implementing the Mediator pattern, which decouples request senders from handlers.

**Why we use it:**
| Reason | Explanation |
|--------|-------------|
| **Decoupling** | Controllers don't know about handler implementations |
| **Single Responsibility** | Each handler does exactly one thing |
| **Testability** | Handlers can be unit tested without HTTP context |
| **Pipeline Behaviors** | Can add cross-cutting concerns (logging, validation) |

**How it helps:**
```
Without MediatR:  Controller → Service → Repository (tightly coupled)
With MediatR:     Controller → MediatR → Handler (loosely coupled)
```

---

### AutoMapper

**What is it?**
A library that automatically maps properties from one object to another.

**Why we use it:**
| Reason | Explanation |
|--------|-------------|
| **Less Boilerplate** | No manual property-by-property copying |
| **Consistency** | Mapping rules defined once, used everywhere |
| **Maintainability** | Adding new properties doesn't require updating multiple files |

**Example:**
```
API Response Model:     DistrictApiModel { lat, long, name }
                              ↓ AutoMapper
Domain Entity:          District { Latitude, Longitude, Name }
```

---

### Swashbuckle (Swagger)

**What is it?**
A tool that generates interactive API documentation from your code.

**Why we use it:**
| Reason | Explanation |
|--------|-------------|
| **Auto-Documentation** | API docs generated from code annotations |
| **Interactive Testing** | Test API endpoints directly in browser |
| **No Postman Needed** | Built-in request/response testing |
| **Always Up-to-Date** | Documentation updates when code changes |

**Access at:** `http://localhost:5155/` (Swagger UI)

---

### API Versioning

**What is it?**
A way to maintain multiple versions of an API simultaneously.

**Why we use it:**
| Reason | Explanation |
|--------|-------------|
| **Backward Compatibility** | Old clients continue working when API changes |
| **Gradual Migration** | Users can migrate to new version at their pace |
| **Clear URLs** | Version visible in URL: `/api/v1/travel/...` |

---

## Configuration Reference

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Hangfire": "Information"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  },
  "ApiSettings": {
    "DistrictsUrl": "https://raw.githubusercontent.com/strativ-dev/technical-screening-test/main/bd-districts.json",
    "WeatherApiBaseUrl": "https://api.open-meteo.com/v1/forecast",
    "AirQualityApiBaseUrl": "https://air-quality-api.open-meteo.com/v1/air-quality"
  },
  "CacheSettings": {
    "DefaultExpirationMinutes": 15,
    "DistrictsCacheHours": 24,
    "WeatherCacheMinutes": 30,
    "AirQualityCacheMinutes": 30,
    "CacheWarmingCronSchedule": "*/15 * * * *"
  }
}
```

### Configuration Options

| Setting | Description | Default |
|---------|-------------|---------|
| `ConnectionStrings:Redis` | Redis server connection string | `localhost:6379` |
| `ApiSettings:DistrictsUrl` | URL to fetch Bangladesh districts | GitHub raw URL |
| `ApiSettings:WeatherApiBaseUrl` | Open-Meteo weather API base URL | `https://api.open-meteo.com/v1/forecast` |
| `ApiSettings:AirQualityApiBaseUrl` | Open-Meteo air quality API base URL | `https://air-quality-api.open-meteo.com/v1/air-quality` |
| `CacheSettings:DefaultExpirationMinutes` | Default cache expiration | 15 minutes |
| `CacheSettings:DistrictsCacheHours` | How long to cache district list | 24 hours |
| `CacheSettings:WeatherCacheMinutes` | How long to cache weather data | 30 minutes |
| `CacheSettings:AirQualityCacheMinutes` | How long to cache air quality data | 30 minutes |
| `CacheSettings:CacheWarmingCronSchedule` | How often to run cache warming job | Every 15 minutes |

### Cron Schedule Format

The `CacheWarmingCronSchedule` uses standard cron format:

```
┌───────────── minute (0 - 59)
│ ┌───────────── hour (0 - 23)
│ │ ┌───────────── day of month (1 - 31)
│ │ │ ┌───────────── month (1 - 12)
│ │ │ │ ┌───────────── day of week (0 - 6)
│ │ │ │ │
│ │ │ │ │
* * * * *
```

**Examples:**
| Schedule | Meaning |
|----------|---------|
| `*/15 * * * *` | Every 15 minutes |
| `0 * * * *` | Every hour |
| `0 0 * * *` | Every day at midnight |
| `0 */6 * * *` | Every 6 hours |

---

## Support

If you encounter any issues:

1. Check the **Hangfire Dashboard** at `/hangfire` for background job errors
2. Check the **application logs** in the terminal
3. Check **Redis logs**: `docker logs traveladvisor-redis`
4. Verify **Redis is running**: `docker ps`
5. Verify **.NET version**: `dotnet --version` (should be 10.0.x)
