---
description: Repository Information Overview
alwaysApply: true
---

# Split-It (QuizManager) Information

## Summary

**Split-It** is a Blazor Server web application that implements a comprehensive job and internship management platform for academic institutions. The platform connects students, companies, professors, and research groups to facilitate job postings, internship placements, thesis opportunities, and event management. Built with Auth0 authentication and SQL Server backend, it supports multiple user roles with specialized dashboards and features for each role type.

## Structure

**Root Organization**:
- **Components/** - Blazor components organized by role (StudentSections, CompanySections, ProfessorSections, ResearchGroupSections, AdminSections)
- **Data/** - Database context, entity configuration, and data access services
- **Models/** - Entity models for all domain objects (Student, Company, Professor, ResearchGroup, Jobs, Internships, Theses, Events, Announcements)
- **Services/** - Business logic services (UserContext, StudentDashboard, CompanyDashboard, ProfessorDashboard, ResearchGroupDashboard, FrontPage)
- **Pages/** - Routable Razor pages (Index, Profile, QuizViewer, SearchJobs, SearchThesis, UploadJobs, Login, Logout, Error)
- **Migrations/** - EF Core database migrations (40+ migrations for schema evolution)
- **ViewModels/** - Data transfer objects for specific views (JobApplicant, ThesisApplicant, NewsArticle)
- **wwwroot/** - Static assets (CSS, images, icons)

## Language & Runtime

**Framework**: ASP.NET Core Blazor Server  
**Language**: C# with nullable reference types enabled  
**Target Framework**: .NET 8.0  
**Build System**: MSBuild (via `dotnet` CLI)  
**Package Manager**: NuGet

## Dependencies

**Key Framework Dependencies**:
- `Microsoft.EntityFrameworkCore` (6.0.0) - ORM for database access
- `Microsoft.EntityFrameworkCore.SqlServer` (6.0.0) - SQL Server provider
- `Microsoft.AspNetCore.Components` - Blazor server components
- `Auth0.AspNetCore.Authentication` (1.4.1) - Auth0 OAuth2 integration
- `Auth0.ManagementApi` (7.37.0) - Auth0 API client

**Email & Communication**:
- `MailKit` (4.11.0), `MimeKit` (4.11.0), `SendGrid` (9.29.3)
- `NETCore.MailKit` (2.1.0)

**Utilities & Libraries**:
- `EPPlus` (7.6.0) - Excel file generation
- `LinqKit` (1.3.8) - LINQ extensions
- `HtmlAgilityPack` (1.11.71) - HTML parsing
- `Syncfusion.Blazor.Core` (28.1.36) - UI components
- `System.Text.Json` (9.0.1) - JSON serialization
- `Crc32.NET` (1.2.0) - Checksum calculations
- `Microsoft.Graph` (5.75.0), `Microsoft.Identity.Web.MicrosoftGraph` (3.8.2) - Microsoft Graph integration

## Build & Installation

**Prerequisites**:
- .NET 8.0 SDK
- SQL Server (local or remote)
- Auth0 account credentials

**Build Command**:
```bash
dotnet build
```

**Run Command**:
```bash
dotnet run
```

**Application URL**: `https://localhost:7290`

**Database Setup**:
- Connection string configured in `appsettings.json`
- Automatic migrations on startup via `db.Database.Migrate()` in `Program.cs`
- Database: SQL Server (JobFinder database)

**Configuration Files**:
- `appsettings.json` - Main settings (connection strings, Auth0, email)
- `appsettings.Development.json` - Development overrides
- `appsettings.Production.json` - Production settings
- `.gitignore` - Git exclusion patterns

## Main Files & Resources

**Application Entry Points**:
- **Pages/Index.razor** - Front page with public events and announcements (42.5 KB)
- **Pages/Profile.razor** - User profile management (92.21 KB)
- **Pages/QuizViewer.razor** - Assessment/survey viewer (218.9 KB)
- **Components/App.razor** - Root component (33.59 KB)
- **Components/Layout/MainLayout.razor** - Master layout template (37.1 KB)

**Role-Specific Components** (Components/Layout/):
- **StudentSections/** - Student dashboard components (events, jobs, internships, theses)
- **CompanySections/** - Company dashboard components (job posting, search, event management)
- **ProfessorSections/** - Professor dashboard components (thesis proposals, events, student search)
- **ResearchGroupSections/** - Research group components (announcements, statistics)
- **AdminSections/** - Administrative interface components

**Configuration & Setup**:
- `Program.cs` - Application startup and service registration
- `Properties/launchSettings.json` - Launch profile configuration
- `QuizManager.csproj` - Project file with NuGet dependencies

**Database Schema**:
- `Data/AppDbContext.cs` - EF Core DbContext with all entity configurations (28.52 KB)

## Services Architecture

**Dashboard Services Pattern** - Business logic separated from UI:
- **IUserContextService** - Authentication state and user information
- **IStudentDashboardService** - Student-specific data and operations (StudentDashboardService: 49.17 KB)
- **ICompanyDashboardService** - Company-specific data and operations (CompanyDashboardService: 122.11 KB)
- **IProfessorDashboardService** - Professor-specific data and operations (ProfessorDashboardService: 73.14 KB)
- **IResearchGroupDashboardService** - Research group data and operations
- **IFrontPageService** - Public data loading (events, announcements)

Each service uses `IDbContextFactory<AppDbContext>` for efficient database operations and returns strongly-typed DTOs (`*DashboardData` classes).

## Authentication & Authorization

**Provider**: Auth0 OAuth2  
**Integration**: Auth0.AspNetCore.Authentication library  
**Domain**: `auth.academyhub.gr`  
**Configuration**: Credentials stored in `appsettings.json` (Auth0 section)  
**Features**:
- User login/logout via Auth0
- Role-based access control (Student, Company, Professor, ResearchGroup, Admin)
- User claims processing
- Auth0 Management API integration for user management

## Pages & Routes

**Public Pages**:
- `/` - Index page (front page with public events)
- `/login` - Auth0 login redirect
- `/logout` - Auth0 logout
- `/error` - Error page

**Authenticated Pages**:
- `/profile` - User profile management
- `/quizviewer` - Assessment viewer
- `/searchjobs` - Job search interface
- `/searchthesis` - Thesis search interface
- `/uploadjobs` - Company job posting (company only)
- `/uploadthesis` - Professor/Company thesis posting
- `/emailverification` - Email verification flow

