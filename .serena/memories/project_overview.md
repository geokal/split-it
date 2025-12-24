# Split-It Project Overview

## Current Status
- **Project Type**: Blazor Server-Side application with .NET 8
- **Architecture**: Service Layer Architecture with Dashboard Services pattern
- **Build Status**: ✅ Passes build with 0 errors, only warnings remain
- **Git State**: Ready for commits

## Key Achievements
1. **Component Restructuring Complete**: Migrated from `Shared/` to `Components/Layout/` structure (.NET 8 convention)
2. **Service Layer Fully Implemented**: 
   - StudentDashboardService (95% complete - only StudentAnnouncementsSection remaining)
   - CompanyDashboardService (100% complete)
   - ProfessorDashboardService (100% complete)
   - ResearchGroupDashboardService (100% complete)
3. **MainLayout Minimized**: Reduced from 34,017 lines to 127 lines
4. **Component Dependencies Extracted**: 100% complete (CS0103 errors cleared)
5. **Bootstrap Tabs Fixed**: Resolved nested tab-pane issue (January 2025)
6. **Render Mode Configured**: Added InteractiveServer render mode for .NET 8 Blazor

## Recent Fixes (January 2025)
- **Bootstrap Tabs**: Fixed nested tab-pane divs in Company section components
- **Render Mode**: Added `@using static Microsoft.AspNetCore.Components.Web.RenderMode` to _Imports.razor
- **Component Structure**: Removed duplicate tab-pane wrappers from child components

## Remaining Work
- Migrate StudentAnnouncementsSection to use IStudentDashboardService (1 component remaining)
- Optional: Address nullable warnings
- Optional: Complete .NET 8 migration verification

## Technical Details
- Uses IDbContextFactory<AppDbContext> for proper resource management
- DTO pattern with immutable records
- Service registration in Program.cs
- Component code-behind files (.razor.cs) for logic separation
- Bootstrap 5 tabs work automatically (no custom JavaScript needed)
- Render mode: InteractiveServer for .NET 8 Blazor Server

## Component Structure
- Parent components (e.g., CompanySection.razor) wrap content in tab-pane divs
- Child components (e.g., CompanyJobsSection.razor) should NOT have tab-pane wrappers
- Avoid nested tab-pane structures - breaks Bootstrap functionality
