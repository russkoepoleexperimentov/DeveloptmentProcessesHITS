# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build GoogleClassroom.sln

# Run (from repo root)
dotnet run --project GoogleClassroom/GoogleClassroom.csproj

# Run all tests
dotnet test GoogleClassroom.sln

# Run a single test project
dotnet test Tests/Tests.csproj

# Run a single test by name
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~YourTestMethodName"

# Add EF Core migration (from repo root)
dotnet ef migrations add MigrationName --project Infrastructure --startup-project GoogleClassroom

# Apply migrations manually (also runs automatically on startup)
dotnet ef database update --project Infrastructure --startup-project GoogleClassroom
```

**Prerequisites:** PostgreSQL on `localhost:5432` with user `postgres` / password `root`, database `GcDb`.

## Architecture

The solution uses **clean architecture** split across 6 projects:

- **GoogleClassroom** — ASP.NET Core 9 Web API. Controllers, middleware, Swagger config, DI registration (`Program.cs`), and EF migrations live here.
- **Application** — Business logic. Contains service interfaces (`Services/Interfaces/`) and implementations (`Services/Implementations/`), DTOs, AutoMapper profiles, and FluentValidation validators.
- **Domain** — Pure domain models in `Models/`. Entities inherit from `BaseEntity` or `BaseEntityWithId`. No infrastructure dependencies.
- **Infrastructure** — `GcDbContext` (EF Core + Npgsql). Lazy loading proxies are enabled.
- **Common** — Shared across all layers: custom exception hierarchy, enums (`CaptainSelectionMode`, `PostType`, `TaskType`), option classes (`JwtOptions`, etc.), and `ExceptionCatchMiddleware`.
- **Tests / IntegrationTests** — xUnit + Moq + FluentAssertions. Unit tests use EF Core InMemory database.

### Key patterns

**Strategy pattern — Captain selection** (`Application/Services/Implementations/CaptainVoting/`):  
`ICaptainAssignmentStrategy` has three implementations — `FirstMemberStrategy`, `TeacherFixedStrategy`, `VotingAndLotteryStrategy`. A factory (`ICaptainStrategyFactory`) selects the strategy based on `CaptainSelectionMode`.

**Exception handling:** Controllers throw typed exceptions from `Common/Exceptions/` (`NotFoundException`, `ForbiddenException`, `BadRequestException`, etc.). `ExceptionCatchMiddleware` catches these and returns the appropriate HTTP status.

**Auth flow:** JWT access tokens (40 min) + refresh tokens (7 days) stored as `RefreshToken` entities. Endpoints use `[Authorize]`; the current user's ID is read from the JWT claim via `HttpContext` extensions in `Common`.

**Posts:** `Post` has two concrete subtypes — `RegularPost` and `Assignment` — mapped using EF Core TPH/TPT. Teams get `TeamAssignment` separately.

**Migrations** run automatically on startup via `dbContext.Database.MigrateAsync()` in `Program.cs`.

### Dependency direction

```
GoogleClassroom → Application → Domain
                             → Common
              → Infrastructure → Domain
                              → Common
              → Common
Tests → Application → ...
```
