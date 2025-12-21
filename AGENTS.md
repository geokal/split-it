# Project: Split-It - .NET 8 Restructuring Workspace

## Status: Restructured to .NET 8 Components Structure ✅

---

## Problem Analysis

### The Conflict
Component names conflicted with Model entity names in JobFinder:

| Entity (Models/) | Component (old Shared/) |
|------------------|-------------------------|
| `QuizManager.Models.Student` | `QuizManager.Shared.Student` |
| `QuizManager.Models.Company` | `QuizManager.Shared.Company` |
| `QuizManager.Models.Professor` | `QuizManager.Shared.Professor` |
| `QuizManager.Models.ResearchGroup` | `QuizManager.Shared.ResearchGroup` |

### Solution
1. Rename role components to `*Section.razor` suffix
2. Restructure to .NET 8 `Components/` folder convention
3. Use distinct namespaces: `QuizManager.Components.Layout.[Role]`

---

## .NET 8 Folder Structure (from Microsoft docs)

```
Components/
├── Layout/
│   ├── MainLayout.razor + .cs + .css
│   ├── NavMenu.razor + .css
│   ├── AccessControl.razor
│   ├── Student/
│   │   ├── StudentSection.razor + .cs  (renamed from Student.razor)
│   │   ├── StudentAnnouncementsSection.razor + .cs
│   │   ├── StudentCompanySearchSection.razor + .cs
│   │   ├── StudentEventsSection.razor + .cs
│   │   ├── StudentInternshipsSection.razor + .cs
│   │   ├── StudentJobsDisplaySection.razor + .cs
│   │   └── StudentThesisDisplaySection.razor + .cs
│   ├── Company/
│   │   ├── CompanySection.razor + .cs  (renamed from Company.razor)
│   │   ├── CompanyAnnouncementsSection.razor + .cs
│   │   ├── CompanyAnnouncementsManagementSection.razor + .cs
│   │   ├── CompanyEventsSection.razor + .cs
│   │   ├── CompanyInternshipsSection.razor + .cs
│   │   ├── CompanyJobsSection.razor + .cs
│   │   ├── CompanyProfessorSearchSection.razor + .cs
│   │   ├── CompanyResearchGroupSearchSection.razor + .cs
│   │   ├── CompanyStudentSearchSection.razor + .cs
│   │   └── CompanyThesesSection.razor + .cs
│   ├── Professor/
│   │   ├── ProfessorSection.razor + .cs
│   │   ├── ProfessorAnnouncementsManagementSection.razor + .cs
│   │   ├── ProfessorCompanySearchSection.razor + .cs
│   │   ├── ProfessorEventsSection.razor + .cs
│   │   ├── ProfessorInternshipsSection.razor + .cs
│   │   ├── ProfessorResearchGroupSearchSection.razor + .cs
│   │   ├── ProfessorStudentSearchSection.razor + .cs
│   │   └── ProfessorThesesSection.razor + .cs
│   ├── ResearchGroup/
│   │   ├── ResearchGroupSection.razor + .cs
│   │   ├── ResearchGroupAnnouncementsSection.razor + .cs
│   │   ├── ResearchGroupCompanySearchSection.razor + .cs
│   │   ├── ResearchGroupEventsSection.razor + .cs
│   │   ├── ResearchGroupProfessorSearchSection.razor + .cs
│   │   └── ResearchGroupStatisticsSection.razor + .cs
│   └── Admin/
│       └── AdminSection.razor + .cs
├── Pages/
│   └── (routable Blazor pages)
├── Helpers/
│   ├── InputTextAreaWithMaxLength.razor
│   ├── LoadingIndicator.razor
│   ├── Pagination.razor
│   ├── RegistrationPrompt.razor
│   ├── NewsSection.razor
│   └── FullScreenLayout.razor
└── App.razor
```

**Razor Pages (separate from Blazor components):**
```
Pages/
├── _Host.cshtml
├── _Layout.cshtml
├── Login.cshtml + .cs
├── Logout.cshtml + .cs
├── Error.cshtml + .cs
└── ... (other Razor Pages)
```

---

## Namespaces

| Role | Namespace |
|------|-----------|
| Layout | `QuizManager.Components.Layout` |
| Student | `QuizManager.Components.Layout.Student` |
| Company | `QuizManager.Components.Layout.Company` |
| Professor | `QuizManager.Components.Layout.Professor` |
| ResearchGroup | `QuizManager.Components.Layout.ResearchGroup` |
| Admin | `QuizManager.Components.Layout.Admin` |
| Helpers | `QuizManager.Components.Helpers` |

---

## Migration Steps Completed

### Phase 1: Create Folder Structure ✅
- Created `Components/Layout/[Role]/` folders
- Created `Components/Pages/` and `Components/Helpers/`

### Phase 2: Move & Rename Files ✅
- Renamed `Student.razor` → `StudentSection.razor`
- Renamed `Company.razor` → `CompanySection.razor`
- Renamed `Professor.razor` → `ProfessorSection.razor`
- Renamed `ResearchGroup.razor` → `ResearchGroupSection.razor`
- Moved all subcomponents from `Shared/[Role]/` to `Components/Layout/[Role]/`

### Phase 3: Update Namespaces ✅
- All `.razor.cs` files updated to `QuizManager.Components.Layout.[Role]`
- Created `_Imports.razor` with new namespaces

### Phase 4: Update Component References ✅
- MainLayout.razor now uses `<StudentSection />`, `<CompanySection />`, etc.

### Phase 5: Migrate to JobFinder ✅
- Copied `Components/` folder to JobFinder
- Removed old `Shared/` structure

---

## _Imports.razor

```razor
@using System.Net.Http
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Components.Authorization
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.JSInterop
@using QuizManager
@using QuizManager.Components.Layout
@using QuizManager.Components.Layout.Student
@using QuizManager.Components.Layout.Company
@using QuizManager.Components.Layout.Professor
@using QuizManager.Components.Layout.ResearchGroup
@using QuizManager.Components.Layout.Admin
@using QuizManager.Components.Helpers
```

---

---

## Services Architecture ✅

### Current Status
- `MainLayout.razor.cs` has been reduced from **34,017 lines to 127 lines** ✅
- All database queries moved to services
- Business logic separated from UI logic
- Front page data loading extracted to `FrontPageService`

### Solution: Dashboard Services Pattern

We've adopted the **Dashboard Services** architecture pattern from JobFinder-refactored:
1. ✅ Extract all database operations to services
2. ✅ Minimize MainLayout to only auth state & navigation
3. ✅ Move business logic from components to services
4. ✅ Use `IDbContextFactory<AppDbContext>` for better resource management

#### Services Structure
```
Services/
├── UserContext/
│   ├── IUserContextService.cs          # User authentication state
│   ├── UserContextService.cs
│   └── UserContextState.cs             # Record with user data
├── FrontPage/
│   ├── IFrontPageService.cs            # Front page data loading
│   ├── FrontPageService.cs             # Public events & announcements
│   └── FrontPageData.cs                # DTO for front page data
├── StudentDashboard/
│   ├── IStudentDashboardService.cs     # Student operations interface
│   ├── StudentDashboardService.cs      # All student DB operations
│   └── StudentDashboardData.cs         # DTO with student data
├── CompanyDashboard/
│   ├── ICompanyDashboardService.cs
│   ├── CompanyDashboardService.cs
│   └── CompanyDashboardData.cs
├── ProfessorDashboard/
│   ├── IProfessorDashboardService.cs
│   ├── ProfessorDashboardService.cs
│   └── ProfessorDashboardData.cs
└── ResearchGroupDashboard/
    ├── IResearchGroupDashboardService.cs
    ├── ResearchGroupDashboardService.cs
    └── ResearchGroupDashboardData.cs
```

#### Key Principles

1. **Service Injection Pattern**:
   - Services inject: `AuthenticationStateProvider`, `IDbContextFactory<AppDbContext>`, `ILogger<T>`
   - Always use `IDbContextFactory<AppDbContext>` (not direct DbContext)
   - Services registered as **Scoped** in `Program.cs`

2. **Data Transfer Objects (DTOs)**:
   - Each service has a `*DashboardData` class with `init` properties
   - Contains all role-specific data (applications, caches, interest IDs, etc.)
   - Has an `Empty` static property for default state
   - DTOs are immutable

3. **Service Methods**:
   - All methods are async with `CancellationToken cancellationToken = default`
   - Return DTOs or strongly-typed results
   - Use `await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);`
   - Always use `.AsNoTracking()` for read operations

4. **Component Usage**:
   - Components inject services: `[Inject] private IStudentDashboardService StudentDashboardService { get; set; }`
   - Components call service methods: `var data = await StudentDashboardService.LoadDashboardDataAsync();`
   - Components receive some data via `[Parameter]` from parent, but also load their own data via services

5. **MainLayout Pattern**:
   - MainLayout.razor.cs is now **minimal** (127 lines) ✅
   - Only handles: authentication state (via `IUserContextService`), front page data (via `IFrontPageService`), navigation helpers
   - **NOT responsible for**: database queries, business logic, role-specific data loading

6. **FrontPage Service**:
   - Handles loading public/published events and announcements for unauthenticated users
   - Returns `FrontPageData` DTO with company events, professor events, and announcements
   - Used by MainLayout to populate front page content

#### Implementation Status

**Phase 1: Create Services Layer** ✅
1. ✅ Create `/Services/StudentDashboard/` folder structure
2. ✅ Create `IStudentDashboardService` interface
3. ✅ Create `StudentDashboardData` DTO class
4. ✅ Implement `StudentDashboardService` with methods extracted from MainLayout.razor.cs
5. ✅ Register services in `Program.cs`

**Phase 2: Extract Logic from MainLayout** ✅
1. ✅ Identify all Student-related methods in MainLayout.razor.cs
2. ✅ Move DB queries to `StudentDashboardService`
3. ✅ Move business logic to appropriate services
4. ⚠️ Components still inject `AppDbContext` directly (future refactoring task)

**Phase 3: Repeat for Other Roles** ✅
- ✅ CompanyDashboardService (fully implemented)
- ✅ ProfessorDashboardService (fully implemented)
- ✅ ResearchGroupDashboardService (interface and scaffold created)

**Phase 4: Minimize MainLayout** ✅
- ✅ Reduced MainLayout.razor.cs from 34,017 lines to 127 lines
- ✅ Created `FrontPageService` for front page data
- ✅ Removed all `DbContext` usage from MainLayout
- ✅ Kept only: auth state (via `IUserContextService`), front page data (via `IFrontPageService`), navigation helpers

**Phase 5: Component Service Migration (in progress)**
- ✅ Company components now use `ICompanyDashboardService` for CRUD/status, lookups, attachments, and interest flows (Jobs, Internships, Theses, Events, Announcements, Search)
- ⚠️ Professor/ResearchGroup/Student components still inject `AppDbContext` directly (future refactor pass)

See `ARCHITECTURE_ANALYSIS.md` for detailed architecture reference from JobFinder-refactored.

---

## Lessons Learned: Component Refactoring Approach

### ⚠️ What We Did Wrong
We minimized `MainLayout.razor.cs` **before** extracting component dependencies, which broke all component references at once and created thousands of errors.

### ✅ Correct Approach (For Future Refactoring)

**For each component:**
1. Identify dependencies on MainLayout
2. Extract to component's `.cs` file
3. Test component compiles
4. Move to next component

**After ALL components are fixed:**
5. Minimize MainLayout
6. Verify everything still works

### Why This Matters
- Components were written to reference MainLayout's properties/methods
- Removing MainLayout properties before extraction breaks all references
- Large component files (2000+ lines) with many property references = hundreds of errors per file
- Fixing incrementally (one component at a time) is safer and more manageable

---

## Phase 7: Component Dependency Extraction ✅ (Complete)

### Status
- **Started**: ~5,600 CS0103 errors (after MainLayout minimization)
- **Current**: 0 CS0103 errors remaining
- **Total Build Errors**: 0 (warnings only)

### Error Count Fluctuations (Expected Behavior)
⚠️ **Important**: Error counts may increase after fixes. This indicates **progress**:
- Fixing CS0103/CS0102 allows compiler to proceed further
- Next layer of errors (CS1061, CS1503) are revealed
- Error type shifts: syntax → type → semantic errors = forward progress

See `docs/ERROR_INVESTIGATION.md` for detailed explanation.

### Fully Fixed Components (0 CS0103 errors) ✅
1. ✅ StudentThesisDisplaySection (412→0)
2. ✅ StudentJobsDisplaySection (474→0)
3. ✅ StudentEventsSection (506→0)
4. ✅ CompanyEventsSection (376→0)
5. ✅ ProfessorEventsSection (754→0)
6. ✅ CompanyJobsSection
7. ✅ CompanyInternshipsSection
8. ✅ CompanyThesesSection
9. ✅ ProfessorThesesSection
10. ✅ ResearchGroupAnnouncementsSection
11. ✅ CompanyResearchGroupSearchSection
12. ✅ ResearchGroupDetails
13. ✅ QuizViewer (pages 1-4)

### Extraction Strategy
- **Source**: `backups/MainLayout.razor.cs.backup` (33,977 lines)
- **Process**: Identify CS0103 errors → Find in backup → Extract to component `.cs` file
- **Commits**: 16+ targeted commits for completed fixes

See `docs/COMPONENT_EXTRACTION_PROGRESS.md` for detailed progress.

## Next Steps

1. ✅ Update `_Imports.razor` with new namespaces
2. ✅ Update `_Host.cshtml` to reference `Components/Layout/MainLayout`
3. ✅ **Create Services Layer** - Extract DB logic from MainLayout to services
4. ✅ **Minimize MainLayout** - Reduced to 127 lines
5. ✅ **Extract Component Dependencies** - 100% complete (CS0103 cleared)
6. ⚠️ **Triage warnings** - Nullable and async warnings
7. ⚠️ **Update Components** - Refactor components to use services instead of direct `DbContext` injection (future task)
8. Upgrade to .NET 8 (change `<TargetFramework>net8.0</TargetFramework>`)
