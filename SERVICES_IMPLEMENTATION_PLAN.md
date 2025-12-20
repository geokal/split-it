# Services Implementation Plan

## Goal ✅ COMPLETE
Extract all database operations and business logic from `MainLayout.razor.cs` (34,017 lines) into dedicated Dashboard Services, reducing MainLayout to ~200 lines.

## Current State ✅
- **MainLayout.razor.cs**: 127 lines (reduced from 34,017 lines) ✅
- **All DB queries**: Moved to services ✅
- **Business logic**: Separated into services ✅
- **Components**: Still use direct `DbContext` injection (future refactoring task)

## Target State ✅ ACHIEVED
- **MainLayout.razor.cs**: 127 lines (only auth state, navigation, front page data) ✅
- **Services**: One service per role handling all DB operations ✅
- **FrontPageService**: Handles front page data loading ✅
- **Components**: Still inject `DbContext` directly (future refactoring task)

---

## Phase 1: Create StudentDashboard Service (Starting Point)

### Step 1.1: Create Folder Structure
```
Services/
└── StudentDashboard/
    ├── IStudentDashboardService.cs
    ├── StudentDashboardService.cs
    └── StudentDashboardData.cs
```

### Step 1.2: Create StudentDashboardData DTO
Based on JobFinder-refactored pattern:
- Properties for: applications (thesis, jobs, internships), caches, interest IDs
- All properties with `init` accessors
- Static `Empty` property
- Immutable record-like structure

### Step 1.3: Create IStudentDashboardService Interface
Key methods to extract:
1. `LoadDashboardDataAsync()` - Load all student data
2. `WithdrawJobApplicationAsync(long rngForJobApplied)`
3. `WithdrawProfessorThesisApplicationAsync(long rngForThesisApplied)`
4. `WithdrawCompanyInternshipApplicationAsync(long rngForInternshipApplied)`
5. `WithdrawProfessorInternshipApplicationAsync(long rngForInternshipApplied)`
6. `GetJobTitleSuggestionsAsync(string searchTerm)`
7. `GetCompanyNameSuggestionsAsync(string searchTerm)`
8. `ShowInterestInCompanyEventAsync(long eventRng, bool needsTransport, string? chosenLocation)`
9. `WithdrawCompanyEventInterestAsync(long eventRng)`
10. `ShowInterestInProfessorEventAsync(long eventRng, bool needsTransport, string? chosenLocation)`
11. `WithdrawProfessorEventInterestAsync(long eventRng)`
12. `GetCompanyInternshipAttachmentAsync(long internshipId)`
13. `GetCompanyByEmailAsync(string email)`
14. `GetProfessorByEmailAsync(string email)`

### Step 1.4: Implement StudentDashboardService
- Inject: `AuthenticationStateProvider`, `IDbContextFactory<AppDbContext>`, `ILogger<StudentDashboardService>`
- Extract methods from MainLayout.razor.cs
- Use `await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);`
- Use `.AsNoTracking()` for read operations
- Return DTOs, not raw entities

### Step 1.5: Register Service in Program.cs
```csharp
builder.Services.AddScoped<IStudentDashboardService, StudentDashboardService>();
```

---

## Phase 2: Create UserContext Service

### Step 2.1: Create UserContextService
- Handles user authentication state
- Returns `UserContextState` record with: IsAuthenticated, Role, Email, registration flags, user entities
- Used by MainLayout instead of direct auth state handling

### Step 2.2: Register in Program.cs
```csharp
builder.Services.AddScoped<IUserContextService, UserContextService>();
```

---

## Phase 3: Update Components to Use Services

### Step 3.1: Update StudentLayoutSection.razor.cs
1. Inject `IStudentDashboardService`
2. Replace direct DB queries with service calls
3. Load data in `OnInitializedAsync()` via service
4. Update methods to call service methods instead of direct DB access

### Step 3.2: Update MainLayout.razor.cs
1. Inject `IUserContextService` instead of direct auth handling
2. Remove all Student-related DB queries
3. Keep only: auth state loading, navigation helpers, front page data

---

## Phase 4: Extract Remaining Roles

Repeat Phase 1 for:
- CompanyDashboardService
- ProfessorDashboardService
- ResearchGroupDashboardService

---

## Phase 5: Verify & Cleanup ✅

1. ✅ Run `dotnet build` to check for errors (builds successfully)
2. ✅ Verify MainLayout.razor.cs is ~200 lines (127 lines achieved)
3. ⚠️ Verify all components use services (components still use DbContext - future task)
4. ⚠️ Test functionality end-to-end (manual testing required)
5. ✅ Remove unused methods from MainLayout.razor.cs (all business logic removed)

## Phase 6: FrontPage Service ✅

1. ✅ Create `Services/FrontPage/` folder structure
2. ✅ Create `IFrontPageService` interface
3. ✅ Create `FrontPageData` DTO class
4. ✅ Implement `FrontPageService` for loading public events and announcements
5. ✅ Register `IFrontPageService` in `Program.cs`
6. ✅ Update MainLayout to use `FrontPageService` for front page data

---

## Key Files to Reference

- **JobFinder-refactored reference**:
  - `/Users/georgek/Downloads/JobFinder-refactored/Services/StudentDashboard/StudentDashboardService.cs`
  - `/Users/georgek/Downloads/JobFinder-refactored/Services/UserContext/UserContextService.cs`
  - `/Users/georgek/Downloads/JobFinder-refactored/Shared/MainLayout.razor.cs` (minimal version)

- **Architecture docs**:
  - `ARCHITECTURE_ANALYSIS.md` - Detailed architecture patterns
  - `AGENTS.md` - Project overview and structure

