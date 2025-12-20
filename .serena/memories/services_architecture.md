# Services Architecture Pattern

## Overview
The project uses a **Dashboard Services** pattern to separate data access from UI components.

## Service Structure
```
Services/
├── UserContext/
│   ├── IUserContextService.cs
│   ├── UserContextService.cs
│   └── UserContextState.cs (record with user auth state)
├── FrontPage/
│   ├── IFrontPageService.cs
│   ├── FrontPageService.cs
│   └── FrontPageData.cs (DTO for front page data)
├── StudentDashboard/
│   ├── IUserContextService.cs
│   ├── UserContextService.cs
│   └── UserContextState.cs (record with user auth state)
├── StudentDashboard/
│   ├── IStudentDashboardService.cs
│   ├── StudentDashboardService.cs
│   └── StudentDashboardData.cs (DTO)
├── CompanyDashboard/
├── ProfessorDashboard/
└── ResearchGroupDashboard/
```

## Key Patterns

### 1. Service Injection Pattern
- Services inject: `AuthenticationStateProvider`, `IDbContextFactory<AppDbContext>`, `ILogger<T>`
- **Always use `IDbContextFactory<AppDbContext>`** (not direct DbContext)
- Services are registered as **Scoped** in `Program.cs`

### 2. Data Transfer Objects (DTOs)
- Each service has a `*DashboardData` class with `init` properties
- Contains all role-specific data (applications, caches, interest IDs, etc.)
- Has an `Empty` static property for default state
- DTOs are immutable and returned from service methods

### 3. Service Methods
- All methods are async and accept `CancellationToken cancellationToken = default`
- Methods return DTOs or strongly-typed results (not raw entities where possible)
- Use `await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);`
- Always use `.AsNoTracking()` for read operations

### 4. Component Usage
- Components inject services: `[Inject] private IStudentDashboardService StudentDashboardService { get; set; }`
- Components call service methods: `var data = await StudentDashboardService.LoadDashboardDataAsync();`
- Components receive some data via `[Parameter]` from parent, but also load their own data via services

### 5. MainLayout Pattern
- MainLayout.razor.cs is now **minimal** (127 lines) ✅
- Only handles: authentication state (via `IUserContextService`), front page data (via `IFrontPageService`), navigation helpers
- **NOT responsible for**: database queries, business logic, role-specific data loading

### 6. FrontPage Service
- Handles loading public/published events and announcements for unauthenticated users
- Returns `FrontPageData` DTO with company events, professor events, and announcements
- Used by MainLayout to populate front page content
- Registered as Scoped service in `Program.cs`

## Example: StudentDashboardService

```csharp
public class StudentDashboardService : IStudentDashboardService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ILogger<StudentDashboardService> _logger;

    public async Task<StudentDashboardData> LoadDashboardDataAsync(CancellationToken cancellationToken = default)
    {
        // 1. Get user email from AuthenticationStateProvider
        // 2. Create DbContext using factory
        // 3. Load all applications, caches, interests
        // 4. Return StudentDashboardData DTO
    }
}
```

## Benefits
1. Separation of Concerns: Services handle data, components handle UI
2. Testability: Services can be unit tested independently
3. Maintainability: Smaller, focused files
4. Reusability: Services can be used by multiple components
5. Performance: DbContextFactory allows better resource management
