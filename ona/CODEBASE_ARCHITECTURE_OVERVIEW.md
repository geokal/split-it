# Split-It (AcademyHub) - Codebase Architecture Overview

## Project Summary

**Split-It** (branded as AcademyHub) is a .NET 8 Blazor Server application connecting students, companies, professors, and research groups for academic job postings, internships, and thesis opportunities. It's a production-ready application with Auth0 authentication and SQL Server database.

---

## Technology Stack

### Core Framework
- **.NET 8** with SDK 9.0.308 (pinned via `global.json`)
- **Blazor Server** with Interactive Server rendering
- **C# 12** with nullable reference types enabled

### Database & ORM
- **Entity Framework Core 8.0**
- **SQL Server** (connection string in `appsettings.json`)
- **DbContextFactory pattern** for thread-safe database access

### Authentication & Authorization
- **Auth0** for authentication (domain: `auth.academyhub.gr`)
- **Role-based access control**: Student, Company, Professor, ResearchGroup, Admin
- Custom middleware for token refresh (`AuthTokenRefreshMiddleware`)

### Key Dependencies
- **Auth0.AspNetCore.Authentication** (1.4.1)
- **EPPlus** (7.6.0) - Excel operations
- **MailKit/MimeKit** (4.11.0) - Email services
- **Syncfusion.Blazor.Core** (28.1.36) - UI components
- **LinqKit** (1.3.8) - Dynamic LINQ queries

---

## Architecture Patterns

### 1. **Service Layer Architecture**

All business logic and database operations are encapsulated in dedicated services:

```
Services/
├── UserContext/          # Authentication state & user profile
├── FrontPage/            # Public landing page data
├── StudentDashboard/     # Student-specific operations
├── CompanyDashboard/     # Company-specific operations
├── ProfessorDashboard/   # Professor-specific operations
├── ResearchGroupDashboard/
└── AdminDashboard/
```

**Key Service Principles:**
- Services use `IDbContextFactory<AppDbContext>` (NOT direct DbContext injection)
- All read operations use `.AsNoTracking()` for performance
- DTOs with `init` properties for immutability
- All services registered as **Scoped** in `Program.cs`
- All async methods accept `CancellationToken cancellationToken = default`

### 2. **Component Organization**

Components follow a role-based structure:

```
Components/
├── Layout/
│   ├── MainLayout.razor + .cs + .css    # Main layout with auth state
│   ├── NavMenu.razor                     # Navigation menu
│   ├── AccessControl.razor               # User dropdown & role display
│   ├── StudentSections/                  # 7 student components
│   ├── CompanySections/                  # 10 company components
│   ├── ProfessorSections/                # 10 professor components
│   ├── ResearchGroupSections/            # 5-6 research group components
│   └── AdminSections/                    # Admin management
├── Pages/                                # Routable Blazor pages
└── Helpers/                              # Shared UI components
```

**Component Pattern:**
- Each section has a `.razor` (markup) and `.razor.cs` (code-behind) file
- Components inject dashboard services for data operations
- MainLayout (~163 lines) handles auth state and front page data only
- No database queries in MainLayout - delegated to services

### 3. **Data Layer**

```
Models/                   # EF Core entities (60+ models)
├── Student.cs
├── Company.cs
├── Professor.cs
├── ResearchGroup.cs
├── CompanyJob.cs
├── CompanyInternship.cs
├── CompanyThesis.cs
└── ... (events, applications, interests)

Data/
├── AppDbContext.cs       # EF Core DbContext
├── EmailService.cs
├── FileUploadService.cs
└── IAuth0Service.cs

ViewModels/               # DTOs for forms/display
├── JobApplicant.cs
├── InternshipApplicant.cs
└── ThesisApplicant.cs
```

**Database Context:**
- `AppDbContext` configures SQL Server connection
- 60+ DbSet properties for entities
- Migrations stored in `Migrations/` folder
- Auto-migration in production (see `Program.cs`)

---

## Entry Points & Configuration

### Application Entry Point
**`Program.cs`** - Configures:
1. DbContextFactory with retry logic
2. Auth0 authentication with custom login paths
3. HttpClient factories (including Auth0 Management API)
4. All dashboard services (Scoped lifetime)
5. Razor Components with Interactive Server mode
6. Middleware pipeline (token refresh, auth, antiforgery)

### Configuration Files
- **`appsettings.json`** - Base config (Auth0, DB connection, email settings)
- **`appsettings.Development.json`** - Dev overrides
- **`appsettings.Production.json`** - Production settings
- **`global.json`** - SDK version lock (9.0.308)

### Authentication Flow
1. User visits `/login` → `Login.cshtml.cs`
2. Auth0 challenge initiated with redirect URI
3. After Auth0 callback, user redirected to target page
4. `UserContextService` loads user profile and role
5. `MainLayout` displays role-specific sections
6. `AccessControl.razor` shows user dropdown with profile/settings/logout

---

## Critical Patterns & Conventions

### 1. **Namespace Convention**
```csharp
QuizManager.Components.Layout                      // Layout components
QuizManager.Components.Layout.StudentSections      // Student components
QuizManager.Components.Layout.CompanySections      // Company components
QuizManager.Services.StudentDashboard              // Student services
QuizManager.Models                                 // EF entities
QuizManager.Data                                   // DbContext & utilities
```

### 2. **Service Pattern Example**
```csharp
public interface IStudentDashboardService
{
    Task<StudentDashboardData> LoadDashboardDataAsync(CancellationToken cancellationToken = default);
    Task RefreshDashboardCacheAsync(CancellationToken cancellationToken = default);
    // ... more methods
}

public class StudentDashboardService : IStudentDashboardService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    
    public async Task<StudentDashboardData> LoadDashboardDataAsync(CancellationToken ct = default)
    {
        using var db = _dbFactory.CreateDbContext();
        var data = await db.Students.AsNoTracking().ToListAsync(ct);
        return new StudentDashboardData { Students = data };
    }
}
```

### 3. **Component Injection Pattern**
```csharp
@inject IStudentDashboardService StudentDashboard
@inject NavigationManager Navigation
@inject IJSRuntime JS

@code {
    protected override async Task OnInitializedAsync()
    {
        var data = await StudentDashboard.LoadDashboardDataAsync();
        // Use data...
    }
}
```

### 4. **Role-Based Rendering**
```razor
<AuthorizeView Roles="Student">
    <Authorized>
        <StudentSection />
    </Authorized>
</AuthorizeView>

<AuthorizeView Roles="Company">
    <Authorized>
        <CompanySection />
    </Authorized>
</AuthorizeView>
```

---

## Important Developer Notes

### ⚠️ Known Issues
1. **OmniSharp Analyzer**: The `muhammad-sammy.csharp` extension has issues with .NET 9 SDK loading `Microsoft.AspNetCore.Razor.Utilities.Shared`. Code Actions may not work, but IntelliSense is functional.

2. **Dev Container**: Currently in `PHASE_FAILED` state - may need rebuild or configuration fixes.

### Build & Run Commands
```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build split-it.sln

# Run application (listens on configured ports)
dotnet run --project QuizManager.csproj

# Production build
dotnet publish -c Release
```

### Database Migrations
```bash
# Add migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Note: Production auto-migrates on startup (see Program.cs)
```

---

## Key Files to Understand First

1. **`Program.cs`** - Application bootstrap, DI configuration
2. **`Components/Layout/MainLayout.razor.cs`** - Main layout logic
3. **`Services/UserContext/UserContextService.cs`** - User authentication state
4. **`Data/AppDbContext.cs`** - Database schema
5. **`Models/Student.cs`, `Company.cs`, `Professor.cs`** - Core entities
6. **`Components/Layout/AccessControl.razor`** - User UI component

---

## Getting Started Checklist

1. ✅ Verify .NET 8 SDK installed (9.0.308 preferred)
2. ✅ Check SQL Server connection string in `appsettings.Development.json`
3. ✅ Review Auth0 configuration (domain, client ID/secret)
4. ✅ Run `dotnet restore` to install dependencies
5. ✅ Run `dotnet build` to verify compilation
6. ✅ Check database migrations: `dotnet ef database update`
7. ✅ Run application: `dotnet run --project QuizManager.csproj`
8. ✅ Navigate to the application URL (typically https://localhost:5001)

---

## Additional Documentation

The `docs/` folder contains:
- **`COMPONENT_EXTRACTION_PROGRESS.md`** - Refactoring history
- **`ERROR_INVESTIGATION.md`** - Debugging notes
- **`USER_ROLE_ANALYSIS.md`** - Role requirements
- **`production_layout_analysis.md`** - UI/UX analysis

This is a well-structured, production-ready application following modern .NET patterns with clear separation of concerns between UI, business logic, and data access layers.
