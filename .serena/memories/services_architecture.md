# Services Architecture Pattern

## Overview
The project uses a **Dashboard Services** pattern to separate data access from UI components. All database operations and business logic have been extracted from MainLayout.razor.cs (reduced from 34,017 to 127 lines).

## Service Structure
```
Services/
├── UserContext/
│   ├── IUserContextService.cs
│   ├── UserContextService.cs
│   └── UserContextState.cs (record with user auth state)
├── FrontPage/
│   ├── IFrontPageService.cs
│   ├── FrontPageService.cs (event-driven with external data fetching)
│   ├── FrontPageData.cs (DTO for front page data)
│   └── FrontPageDataState.cs (event-driven state record)
├── StudentDashboard/
│   ├── IStudentDashboardService.cs
│   ├── StudentDashboardService.cs (with IMemoryCache caching)
│   └── StudentDashboardData.cs (DTO)
├── CompanyDashboard/
│   ├── ICompanyDashboardService.cs
│   ├── CompanyDashboardService.cs (fully implemented)
│   └── CompanyDashboardData.cs
├── ProfessorDashboard/
│   ├── IProfessorDashboardService.cs
│   ├── ProfessorDashboardService.cs (fully implemented)
│   └── ProfessorDashboardData.cs
└── ResearchGroupDashboard/
    ├── IResearchGroupDashboardService.cs
    ├── ResearchGroupDashboardService.cs (interface and scaffold created)
    └── ResearchGroupDashboardData.cs
```

## Key Patterns

### 1. Service Injection Pattern
- Services inject: `AuthenticationStateProvider`, `IDbContextFactory<AppDbContext>`, `ILogger<T>`, `IMemoryCache` (where applicable)
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

### 4. Caching Pattern (StudentDashboardService)
- Uses `IMemoryCache` with `SemaphoreSlim` for thread-safe double-check locking
- Cache key: `"student_dashboard_{email}"`
- TTL: 5 minutes
- Cache invalidation: `RefreshDashboardCacheAsync()` called after all mutation operations
- Pattern: Check cache → Lock → Double-check cache → Load from DB → Cache → Return

### 5. Event-Driven State Management (FrontPageService)
- Uses `FrontPageDataState` record to hold current state
- Implements `event Action<FrontPageDataState>? StateChanged`
- Uses `SemaphoreSlim` for thread-safe state updates
- Methods: `EnsureDataLoadedAsync()`, `RefreshAsync()`
- External data: Fetches UoA news, SVSE news, weather using `IHttpClientFactory` and `HtmlAgilityPack`
- MainLayout subscribes to `StateChanged` events for reactive UI updates

### 6. Component Usage
- Components inject services: `[Inject] private IStudentDashboardService StudentDashboardService { get; set; }`
- Components call service methods: `var data = await StudentDashboardService.LoadDashboardDataAsync();`
- Components receive some data via `[Parameter]` from parent, but also load their own data via services
- **Note**: Currently components still inject `AppDbContext` directly (future refactoring task)

### 7. MainLayout Pattern
- MainLayout.razor.cs is now **minimal** (127 lines) ✅
- Only handles: authentication state (via `IUserContextService`), front page data (via `IFrontPageService`), navigation helpers
- Implements `IDisposable` to unsubscribe from `FrontPageService.StateChanged` events
- Uses `CascadingValue` to pass state to child components
- Properties made public for Razor component access
- **NOT responsible for**: database queries, business logic, role-specific data loading

### 8. FrontPage Service
- Handles loading public/published events and announcements for unauthenticated users
- Returns `FrontPageData` DTO with company events, professor events, and announcements
- Event-driven: MainLayout subscribes to `StateChanged` events for reactive UI updates
- External integrations: Fetches news (UoA, SVSE) and weather data from external APIs using `IHttpClientFactory` and `HtmlAgilityPack`
- Used by MainLayout to populate front page content
- Registered as Scoped service in `Program.cs`

## Example: StudentDashboardService with Caching

```csharp
public class StudentDashboardService : IStudentDashboardService
{
    private readonly IMemoryCache _memoryCache;
    private readonly SemaphoreSlim _dashboardLock = new(1, 1);
    
    public async Task<StudentDashboardData> LoadDashboardDataAsync(CancellationToken cancellationToken = default)
    {
        var email = await ResolveStudentEmailAsync(cancellationToken);
        var cacheKey = $"student_dashboard_{email}";
        
        if (_memoryCache.TryGetValue(cacheKey, out StudentDashboardData? cachedData))
            return cachedData!;
            
        await _dashboardLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after lock
            if (_memoryCache.TryGetValue(cacheKey, out cachedData))
                return cachedData!;
                
            // Load from DB
            var data = await LoadDataFromDatabaseAsync(email, cancellationToken);
            
            // Cache for 5 minutes
            _memoryCache.Set(cacheKey, data, TimeSpan.FromMinutes(5));
            return data;
        }
        finally
        {
            _dashboardLock.Release();
        }
    }
    
    public async Task RefreshDashboardCacheAsync(CancellationToken cancellationToken = default)
    {
        var email = await ResolveStudentEmailAsync(cancellationToken);
        var cacheKey = $"student_dashboard_{email}";
        _memoryCache.Remove(cacheKey);
    }
}
```

## Implementation Status
- ✅ StudentDashboardService: Fully implemented with caching
- ✅ CompanyDashboardService: Fully implemented (61/61 methods)
- ✅ ProfessorDashboardService: Fully implemented (41/41 methods)
- ✅ ResearchGroupDashboardService: Interface and scaffold created
- ✅ UserContextService: Fully implemented
- ✅ FrontPageService: Fully implemented with event-driven state and external data fetching

## Benefits
1. Separation of Concerns: Services handle data, components handle UI
2. Testability: Services can be unit tested independently
3. Maintainability: Smaller, focused files
4. Reusability: Services can be used by multiple components
5. Performance: DbContextFactory allows better resource management, caching reduces DB load
6. Scalability: Event-driven patterns enable reactive UI updates