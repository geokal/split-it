# Split-It Project Overview

## Current Status
- **Project Type**: Blazor Server-Side application with .NET 8
- **Architecture**: Service Layer Architecture with Dashboard Services pattern
- **Build Status**: ✅ Passes build with 0 errors, only warnings remain
- **Git State**: Ready for commits

## Key Achievements
1. **Component Restructuring Complete**: Migrated from `Shared/` to `Components/Layout/` structure
2. **Service Layer Fully Implemented**: 
   - StudentDashboardService (95% complete - only StudentAnnouncementsSection remaining)
   - CompanyDashboardService (100% complete)
   - ProfessorDashboardService (100% complete)
   - ResearchGroupDashboardService (100% complete)
3. **MainLayout Minimized**: Reduced from 34,017 lines to 127 lines
4. **Component Dependencies Extracted**: 100% complete (CS0103 errors cleared)

## Remaining Work
- Migrate StudentAnnouncementsSection to use IStudentDashboardService (1 component remaining)
- Optional: Address nullable warnings
- Optional: Upgrade to .NET 8 (TargetFramework already set)

## Technical Details
- Uses IDbContextFactory<AppDbContext> for proper resource management
- DTO pattern with immutable records
- Service registration in Program.cs
- Component code-behind files (.razor.cs) for logic separation