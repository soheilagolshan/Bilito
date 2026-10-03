# Bilito Backend V0

Bilito is a .NET 10 modular-monolith learning project. The backend includes passwordless mobile OTP authentication, Identity persistence, local SQL Server infrastructure, API plumbing, and test projects. Business modules remain future work.

## Technology

- .NET 10 and ASP.NET Core Web API
- C# with nullable reference types and implicit usings
- SQL Server 2022 in Docker Compose
- Entity Framework Core with custom passwordless identity infrastructure
- Swagger/OpenAPI, ProblemDetails, CORS, and health checks
- xUnit tests

## Structure

```text
src/
  Bilito.Api/                         Composition root
  Bilito.SharedKernel/                Minimal shared code
  Bilito.Database/                    Standalone FluentMigrator runner
  Modules/Identity/
    Bilito.Identity.Domain/            Domain layer
    Bilito.Identity.Application/       Feature/application layer
    Bilito.Identity.Infrastructure/   EF Core and Identity implementation
    Bilito.Identity.Contracts/        Public API contracts
tests/
  Bilito.UnitTests/
  Bilito.IntegrationTests/
  Bilito.ArchitectureTests/
docs/
  architecture/
  adr/
```

Dependency direction is `Domain -> SharedKernel`, `Application -> Domain/Contracts`, `Infrastructure -> Application/Domain`, and `Api -> Application/Infrastructure/Contracts`. `Bilito.Database` owns schema migrations and is independent from the API. The Domain layer does not know about ASP.NET Core, EF Core, or SQL Server. EF entities and Identity persistence types are not API contracts.

## Prerequisites

- .NET SDK 10
- Docker Desktop with Docker Compose

## Local configuration

Copy `.env.example` to `.env` and replace `CHANGE_ME` with a strong local SQL Server password. `.env` is ignored by Git. The API connection string is intentionally non-secret in `appsettings.json`; provide the SQL password through user secrets or an environment override:

```powershell
dotnet user-secrets init --project src/Bilito.Api
dotnet user-secrets set "ConnectionStrings:Database" "Server=localhost,1433;Database=BilitoIdentity;User Id=sa;Password=YOUR_LOCAL_PASSWORD;TrustServerCertificate=True;" --project src/Bilito.Api
```

Alternatively set `ConnectionStrings__Database` in the API or database-runner process environment.

## Database migrations

Entity Framework Core remains the runtime ORM for querying and persistence. FluentMigrator exclusively owns database schema creation and evolution. EF Core migrations are intentionally not used, and neither the API nor the DbContext calls `Database.Migrate()` or `EnsureCreated()`.

Migrations live in `src/Bilito.Database/Migrations`, organized by module. Migration IDs use UTC-style timestamps in `yyyyMMddHHmmss` form, for example `20260930120000`, to reduce collisions between developers. Identity migrations create the `identity` schema and the `Users`, `OtpChallenges`, and `RefreshTokens` tables.

Start SQL Server and apply pending migrations with:

```powershell
docker compose up -d
dotnet run --project src/Bilito.Database
dotnet run --project src/Bilito.Api
```

To add a schema change, create a descriptive timestamped class under the owning module, implement both `Up()` and a conservative `Down()`, then run the database project again. Do not run `dotnet ef migrations add`; EF migrations are not part of this architecture.

## Run SQL Server

```powershell
Copy-Item .env.example .env
# Edit .env and set SQL_PASSWORD first.
docker compose up -d
```

## Build and test

```powershell
dotnet restore
dotnet build
dotnet test
```

## Run the API

```powershell
dotnet run --project src/Bilito.Api
```

Swagger is available at `https://localhost:<port>/swagger` in Development. Health status is available at `/health`. The React frontend can use the configured `http://localhost:5173` origin and call the versioned routes under `/api/v1`.

This is a test/learning project and is not production-ready. Authorization policies, production SMS delivery, and business modules remain future work.

## Passwordless mobile authentication

Identity uses a unified passwordless mobile + OTP flow. Iranian mobile numbers are accepted as `09121234567`, `989121234567`, or `+989121234567` and stored canonically as `+989121234567`.

Endpoints:

- `POST /api/v1/auth/otp/request`
- `POST /api/v1/auth/otp/verify`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout`
- `GET /api/v1/users/me`

OTP challenges are six digits, cryptographically generated, valid for two minutes by default, limited to five attempts, and protected by a 60-second resend cooldown. Only the OTP hash is persisted. A new challenge invalidates the previous active challenge for that mobile number.

In Development, `Otp:ExposeDevelopmentOtp` is enabled by `appsettings.Development.json`, so the request response includes `developmentOtp` for local frontend work. The implementation also checks the hosting environment, so this value cannot be exposed when the application runs in Production even if configuration is accidentally enabled. OTP values are not logged.

Successful verification returns a short-lived JWT access token and sets the refresh token in an HttpOnly cookie. Refresh tokens are cryptographically random, stored only as hashes, rotated on refresh, and revoked on logout. React requests that use refresh or logout must include credentials, for example `fetch(url, { credentials: 'include' })`. The configured CORS policy must include the frontend origin and never combines `AllowAnyOrigin` with credentials.

Required local secrets include the database connection password and JWT signing key. Set them without committing them:

```powershell
dotnet user-secrets init --project src/Bilito.Api
dotnet user-secrets set "ConnectionStrings:Database" "Server=localhost,1433;Database=BilitoIdentity;User Id=sa;Password=YOUR_LOCAL_PASSWORD;TrustServerCertificate=True;" --project src/Bilito.Api
dotnet user-secrets set "Authentication:Jwt:SigningKey" "YOUR_LONG_DEVELOPMENT_SIGNING_KEY" --project src/Bilito.Api
```

If no JWT signing key is configured while running in Development, the API generates a temporary cryptographically random key in memory. Access tokens become invalid after an API restart. Production requires `Authentication:Jwt:SigningKey` to be supplied through secrets or environment configuration.

Start SQL Server, run FluentMigrator, then start the API:

```powershell
docker compose up -d
dotnet run --project src/Bilito.Database
dotnet run --project src/Bilito.Api
```

Running `Bilito.Api` alone does not create tables. The API intentionally does not run migrations automatically. `Bilito.Database` must be run first. The target database must already exist and the configured SQL Server login must have permission to access it. For SQL Server Express, create the database once if necessary:

```sql
IF DB_ID(N'Bilito') IS NULL
    CREATE DATABASE [Bilito];
```

Then run `dotnet run --project src/Bilito.Database` again. The Identity migrations are kept separately under `src/Bilito.Database/Migrations/Identity` and create the `identity` schema followed by `Users`, `OtpChallenges`, and `RefreshTokens`. FluentMigrator records applied versions in its version table and only applies pending migrations.

See [ADR 0002](docs/adr/0002-passwordless-mobile-otp.md) for the authentication decision and its trade-offs.

## Bilito Backoffice

Bilito.Backoffice is an internal Blazor WebAssembly developer UI for manually exercising the API. React remains the primary external frontend. Backoffice communicates with `Bilito.Api` exclusively through HTTP and references only the public Identity contracts; it does not access application services, EF Core, SQL Server, or the database project.

The API base URL is configured in `src/Bilito.Backoffice/wwwroot/appsettings.json`:

```json
{
  "BilitoApi": {
    "BaseUrl": "https://localhost:7238/"
  }
}
```

The local workflow is:

```powershell
docker compose up -d
dotnet run --project src/Bilito.Database
dotnet run --project src/Bilito.Api
dotnet run --project src/Bilito.Backoffice
```

Use the HTTPS Backoffice URL `https://localhost:7296` when using the API HTTPS profile. The API Development CORS configuration allows the Backoffice origins `https://localhost:7296` and `http://localhost:5130` in addition to the React origin.

The Login page requests and verifies mobile OTPs, displays `developmentOtp` only when the API returns it, and then loads `/api/v1/users/me`. Refresh and logout use the HttpOnly refresh-token cookie with browser credentials. The access token is held only in scoped in-memory authentication state for this developer tool; it is attached centrally by a `DelegatingHandler` and is lost on a full browser reload. The UI never displays refresh tokens or server secrets, and redacts access tokens from the developer request inspector.
