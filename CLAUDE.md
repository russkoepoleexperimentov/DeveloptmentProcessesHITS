# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Build entire solution
dotnet build GoogleClassroom.sln

# Run the API (requires PostgreSQL running on localhost:5432)
dotnet run --project GoogleClassroom/GoogleClassroom.csproj

# Run all tests
dotnet test GoogleClassroom.sln

# Run a specific test project
dotnet test Tests/Tests.csproj
dotnet test IntegrationTests/IntegrationTests.csproj

# Run a single test by name
dotnet test --filter "FullyQualifiedName~MethodName"

# Add a new EF Core migration
dotnet ef migrations add MigrationName --project Infrastructure --startup-project GoogleClassroom

# Apply migrations manually (also runs automatically on startup)
dotnet ef database update --project Infrastructure --startup-project GoogleClassroom
```

## Database

PostgreSQL is required locally:
- Host: `localhost:5432`
- User: `postgres` / Password: `root`
- Database: `GcDb`

Migrations are applied automatically on startup via `context.Database.Migrate()` in `Program.cs`. Unit tests use EF Core InMemory provider.

## Architecture

Six-layer clean architecture. Dependency direction: `GoogleClassroom → Application → Domain`, `GoogleClassroom → Infrastructure → Domain`, `Common` is shared by all.

| Project | Responsibility |
|---|---|
| `GoogleClassroom` | ASP.NET Core 9 Web API — controllers, middleware, DI wiring, migrations folder |
| `Application` | Services (interfaces + implementations), DTOs, AutoMapper profiles, FluentValidation validators |
| `Domain` | Pure entity models with no dependencies |
| `Infrastructure` | `GcDbContext` (EF Core + Npgsql), repository layer |
| `Common` | Exceptions, enums, options, `HttpContextExtensions`, `ExceptionCatchMiddleware` |
| `Tests` / `IntegrationTests` / `Test` | xUnit unit and integration test suites |

## Key Patterns

**Exception handling** — All exceptions bubble up to `ExceptionCatchMiddleware`. Throw the appropriate typed exception (`NotFoundException`, `ForbiddenException`, `BadRequestException`, `EntryExistsException`, `InvalidAuthenticationDataException`) from `Common/Exceptions/`; the middleware maps them to HTTP status codes and a standard `ApiResponse<T>` JSON body.

**Response shape** — Controllers wrap results in `ApiResponse<T>` (and `PagedResponse<T>` for lists). Follow existing controller patterns when adding endpoints.

**Authentication** — JWT access tokens (40 min) + refresh tokens (7 days, stored in DB as `RefreshToken` entities). Current user ID is read from `HttpContext` via `HttpContextExtensions.GetUserId()`.

**Captain selection strategy** — `CaptainSelectionMode` enum drives which `ICaptainAssignmentStrategy` is used (`FirstMemberStrategy`, `TeacherFixedStrategy`, `VotingAndLotteryStrategy`). Add new modes by implementing the interface and registering in `ICaptainStrategyFactory`.

**Entity inheritance** — `Post` is a base class with `RegularPost` and `Assignment` as derived types (EF Core TPH). New post-like types should follow this pattern.

**Lazy loading** — EF Core proxies are enabled. Navigation properties are virtual and load lazily; be mindful of N+1 queries in service code.

**Validation** — FluentValidation validators in `Application/Validators/` run before services. Add a validator whenever adding a new request DTO and register it in `Program.cs`.

**AutoMapper** — Mapping profiles live in `Application/Profiles/`. Add mappings there rather than doing manual projection in services.

## Project Conventions

- Service interfaces are in `Application/Services/Interfaces/`, implementations in `Application/Services/Implementations/`.
- DTOs are organized by domain subdirectory under `Application/DTOs/` (Auth, Post, Comment, Course, Solution, User, Common).
- `BaseEntity` (no Id) and `BaseEntityWithId` (with Guid Id) are the root entity base classes in `Domain/Models/`.
- Swagger UI is available in development with JWT bearer token support (`SwaggerAuthorizeFilter`).
- CORS currently allows all origins — tighten before any production deployment.
