# Architecture Analysis: JobFinder-refactored

## Overview
This document summarizes the architecture patterns used in `/Users/georgek/Downloads/JobFinder-refactored` to guide the refactoring of the split-it project.

---

## 1. Services Layer (`/Services/`)

### Pattern: Dashboard Services
Each role has a dedicated service that handles **all database operations and business logic**:

```
Services/
├── StudentDashboard/
│   ├── IStudentDashboardService.cs       # Interface
│   ├── StudentDashboardService.cs        # Implementation
│   └── StudentDashboardData.cs           # DTO/Data transfer object
├── CompanyDashboard/
│   ├── ICompanyDashboardService.cs
│   ├── CompanyDashboardService.cs
│   └── CompanyDashboardData.cs
└── ... (similar for Professor, ResearchGroup)
```

### Key Characteristics:

1. **Service Interface** (I*DashboardService):
   - Defines async methods for all operations (LoadDashboardDataAsync, WithdrawApplicationAsync, etc.)
   - Uses CancellationToken for async operations

2. **Service Implementation**:
   - Injects: `AuthenticationStateProvider`, `IDbContextFactory<AppDbContext>`, `ILogger`
   - **Uses DbContextFactory pattern** (not direct DbContext injection)
   - Returns strongly-typed DTOs (e.g., `StudentDashboardData`)

3. **Data DTOs**:
   - Immutable data classes with `init` properties
   - Contains all role-specific data (applications, caches, interest IDs, etc.)
   - Has an `Empty` static property for default/empty state

### Example: StudentDashboardService
```csharp
public async Task<StudentDashboardData> LoadDashboardDataAsync(CancellationToken cancellationToken = default)
{
    // Gets user email from AuthenticationStateProvider
    // Creates DbContext using DbContextFactory
    // Loads all applications, caches, interests
    // Returns StudentDashboardData DTO
}
```

---

## 2. Component Architecture (`/Shared/`)

### Pattern: Layout Sections with Partial Classes

#### Main Structure:
```
Shared/
├── MainLayout.razor + .razor.cs          # Minimal - only auth state & navigation
├── StudentLayoutSection.razor + .razor.cs
│   └── Student/                          # Partial class files
│       ├── StudentLayoutSection.AnnouncementsAndNews.cs
│       ├── StudentLayoutSection.Events.cs
│       ├── StudentLayoutSection.JobsSearch.cs
│       ├── StudentLayoutSection.InternshipsSearch.cs
│       └── ...
└── Components/                           # Subcomponents
    ├── Company/
    │   ├── CompanyAnnouncementsComponent.razor
    │   ├── CompanyPositionsComponent.razor
    │   └── ...
    └── ...
```

### MainLayout.razor.cs (Minimal - 127 lines) ✅

**Responsibilities:**
- User authentication state loading (via `IUserContextService`)
- Front page data (via `IFrontPageService`)
- Basic navigation helpers (`ShouldShowAdminTable()`, etc.)
- User role and registration state properties

**NOT responsible for:**
- Database queries (all moved to services)
- Business logic (all moved to services)
- Role-specific data loading (handled by Dashboard Services)

**Implementation:**
- Uses `IFrontPageService.LoadFrontPageDataAsync()` to load public events
- Uses `IUserContextService.GetStateAsync()` for authentication state
- No direct `DbContext` usage

### StudentLayoutSection Pattern:

1. **Main .razor.cs file**:
   - Injects: `IStudentDashboardService`, `IJSRuntime`, `NavigationManager`, etc.
   - Has `[Parameter]` properties for data passed from parent
   - Holds local state (UI toggles, pagination, etc.)
   - Calls service methods for data operations

2. **Partial class files** (e.g., `StudentLayoutSection.Events.cs`):
   - Contains feature-specific methods
   - Organized by domain (Announcements, Events, Jobs, Internships, etc.)
   - Keeps main file clean and organized

### Example: How Components Use Services

```csharp
// In StudentLayoutSection.razor.cs
[Inject] private IStudentDashboardService StudentDashboardService { get; set; }

private StudentDashboardData studentDashboardData = StudentDashboardData.Empty;

protected override async Task OnInitializedAsync()
{
    studentDashboardData = await StudentDashboardService.LoadDashboardDataAsync();
    // Update local properties from DTO
    JobApplications = studentDashboardData.JobApplications.ToList();
    JobCache = studentDashboardData.JobCache;
}

// In partial class (e.g., StudentLayoutSection.JobsSearch.cs)
private async Task WithdrawJobApplication(long jobId)
{
    await StudentDashboardService.WithdrawJobApplicationAsync(jobId);
    // Reload data
    studentDashboardData = await StudentDashboardService.LoadDashboardDataAsync();
    StateHasChanged();
}
```

---

## 3. Service Registration (`Program.cs`)

All services registered as **Scoped**:

```csharp
builder.Services.AddScoped<IStudentDashboardService, StudentDashboardService>();
builder.Services.AddScoped<ICompanyDashboardService, CompanyDashboardService>();
builder.Services.AddScoped<IProfessorDashboardService, ProfessorDashboardService>();
builder.Services.AddScoped<IResearchGroupDashboardService, ResearchGroupDashboardService>();
builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddScoped<IFrontPageDataService, FrontPageDataService>();

// DbContextFactory pattern
builder.Services.AddDbContextFactory<AppDbContext>(options => {
    options.UseSqlServer(connectionString);
});
```

---

## 4. Key Differences from Current split-it Project

### Current State (split-it):
- ❌ `MainLayout.razor.cs` has 34,017 lines
- ❌ All database queries directly in components
- ❌ Business logic mixed with UI logic
- ❌ 1620 Student references in MainLayout vs 158 in components
- ❌ Direct `DbContext` injection (not factory pattern)

### Target State (JobFinder-refactored pattern):
- ✅ `MainLayout.razor.cs` ~200 lines (only auth state)
- ✅ Services handle all DB operations
- ✅ Components inject services and call methods
- ✅ All role-specific logic in role layout sections
- ✅ Uses `IDbContextFactory<AppDbContext>`

---

## 5. Migration Strategy for split-it

### Phase 1: Create Services
1. Create `/Services/StudentDashboard/` folder
2. Create `IStudentDashboardService` interface
3. Create `StudentDashboardData` DTO class
4. Implement `StudentDashboardService` with methods from MainLayout.razor.cs
5. Register services in Program.cs

### Phase 2: Extract Logic from MainLayout
1. Identify all Student-related methods in MainLayout.razor.cs
2. Move DB queries to `StudentDashboardService`
3. Move UI state/helpers to `StudentLayoutSection.razor.cs`
4. Update components to inject and use services

### Phase 3: Organize Component Code
1. Split `StudentLayoutSection.razor.cs` into partial classes by feature
2. Keep main file for injections and core state
3. Move feature-specific methods to partial files

### Phase 4: Repeat for Other Roles
- CompanyDashboardService
- ProfessorDashboardService
- ResearchGroupDashboardService

---

## 6. Benefits of This Architecture

1. **Separation of Concerns**: Services handle data, components handle UI
2. **Testability**: Services can be unit tested independently
3. **Maintainability**: Smaller, focused files
4. **Reusability**: Services can be used by multiple components
5. **Performance**: DbContextFactory allows better resource management
6. **Scalability**: Easy to add new features without bloating existing files

---

## 7. Important Patterns to Follow

1. **Always use `IDbContextFactory<AppDbContext>`** in services
2. **Return DTOs** from services, not raw entities
3. **Use `CancellationToken`** in all async service methods
4. **Keep MainLayout minimal** - only cross-cutting concerns
5. **Use partial classes** for large component code-behind files
6. **Inhabit pattern**: Components receive data via `[Parameter]` from parent, but also load their own data via services

