# Project: Split-It (AcademyHub)

## Status: .NET 8 Production Application ✅

---

## Overview

Split-It (also known as AcademyHub) is a .NET 8 Blazor Server application for managing academic job/internship/thesis postings joining students, companies, professors, and research groups.

---

## Technology Stack

- **Framework**: .NET 8 (SDK 9.0.302 via `global.json`)
- **UI**: Blazor Server with Interactive Server rendering
- **Database**: Entity Framework Core 8 with SQL Server
- **Auth**: Auth0 (role-based: Student, Company, Professor, ResearchGroup, Admin)
- **CSS**: Bootstrap 5 + custom styles

---

## Folder Structure

```
Components/
├── Layout/
│   ├── MainLayout.razor + .cs + .css
│   ├── NavMenu.razor + .css
│   ├── AccessControl.razor
│   ├── StudentSections/
│   │   ├── StudentSection.razor
│   │   ├── StudentAnnouncementsSection.razor + .cs
│   │   ├── StudentCompanySearchSection.razor + .cs
│   │   ├── StudentEventsSection.razor + .cs
│   │   ├── StudentInternshipsSection.razor + .cs
│   │   ├── StudentJobsDisplaySection.razor + .cs
│   │   └── StudentThesisDisplaySection.razor + .cs
│   ├── CompanySections/
│   │   └── (10 component pairs)
│   ├── ProfessorSections/
│   │   └── (10 component pairs)
│   ├── ResearchGroupSections/
│   │   └── (5-6 component pairs)
│   └── AdminSections/
│       └── AdminSection.razor + .cs
├── Pages/
│   └── (Blazor routable pages)
└── Helpers/
    └── (shared UI components)

Services/
├── UserContext/
│   ├── IUserContextService.cs
│   ├── UserContextService.cs
│   └── UserContextState.cs
├── FrontPage/
│   ├── IFrontPageService.cs
│   ├── FrontPageService.cs
│   └── FrontPageData.cs
├── StudentDashboard/
│   ├── IStudentDashboardService.cs
│   ├── StudentDashboardService.cs
│   └── StudentDashboardData.cs
├── CompanyDashboard/
├── ProfessorDashboard/
└── ResearchGroupDashboard/

Pages/            # Razor Pages (cshtml) for auth
├── _Host.cshtml
├── Login.cshtml + .cs
├── Logout.cshtml + .cs
└── Error.cshtml + .cs

Models/           # EF Core entities
Data/             # DbContext, migrations config
ViewModels/       # DTOs for forms/display
```

---

## Namespaces

| Area          | Namespace                                             |
| ------------- | ----------------------------------------------------- |
| Layout        | `QuizManager.Components.Layout`                       |
| Student       | `QuizManager.Components.Layout.StudentSections`       |
| Company       | `QuizManager.Components.Layout.CompanySections`       |
| Professor     | `QuizManager.Components.Layout.ProfessorSections`     |
| ResearchGroup | `QuizManager.Components.Layout.ResearchGroupSections` |
| Admin         | `QuizManager.Components.Layout.AdminSections`         |
| Helpers       | `QuizManager.Components.Helpers`                      |

---

## Services Architecture

### Dashboard Services Pattern

All database operations flow through dedicated services:

1. **UserContextService** - Authentication state and user profile data
2. **FrontPageService** - Public events/announcements for landing page
3. **StudentDashboardService** - Student CRUD operations
4. **CompanyDashboardService** - Company CRUD operations
5. **ProfessorDashboardService** - Professor CRUD operations
6. **ResearchGroupDashboardService** - Research group operations

### Key Principles

- Services use `IDbContextFactory<AppDbContext>` (not direct injection)
- All read operations use `.AsNoTracking()`
- DTOs are immutable with `init` properties
- Services registered as **Scoped** in `Program.cs`
- All async methods accept `CancellationToken cancellationToken = default`

### MainLayout

- **Current size**: ~163 lines (code-behind)
- Handles: auth state, front page data, navigation helpers
- Does NOT handle: database queries, business logic

---

## Role-Based Components

Each role has a main section component and specialized sub-sections:

| Role          | Main Component       | Sub-Sections                                                                                  |
| ------------- | -------------------- | --------------------------------------------------------------------------------------------- |
| Student       | StudentSection       | Events, Jobs, Thesis, Internships, CompanySearch, Announcements                               |
| Company       | CompanySection       | Jobs, Internships, Theses, Events, Announcements, Search (Students/Professors/ResearchGroups) |
| Professor     | ProfessorSection     | Theses, Events, Internships, Announcements, Search (Students/Companies/ResearchGroups)        |
| ResearchGroup | ResearchGroupSection | Events, Announcements, Statistics, Search                                                     |
| Admin         | AdminSection         | User management, system config                                                                |

---

## Build & Run

```bash
# Restore and build
dotnet restore
dotnet build split-it.sln

# Run locally
dotnet run --project QuizManager.csproj

# Production build
dotnet publish -c Release
```

---

## Configuration

- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Local development (connection strings, Auth0 dev tenant)
- `appsettings.Production.json` - Production settings
- `global.json` - SDK version pinned to 9.0.302

---

## Known Issues

1. **OmniSharp Analyzer Errors**: The `muhammad-sammy.csharp` extension has issues loading `Microsoft.AspNetCore.Razor.Utilities.Shared` with .NET 9 SDKs. This only affects Code Actions (quick fixes); IntelliSense works fine.

---

## Additional Documentation

See the `docs/` folder for:

- `COMPONENT_EXTRACTION_PROGRESS.md` - Refactoring history
- `ERROR_INVESTIGATION.md` - Debugging notes
- `USER_ROLE_ANALYSIS.md` - Role requirements
- `production_layout_analysis.md` - UI/UX analysis
