# Split-It Project Overview

## Purpose
This is a .NET 6 Blazor Server application refactoring project. The goal is to refactor a monolithic `MainLayout.razor` file (~39,000 lines) into smaller, manageable, role-based components.

## Tech Stack
- **Framework**: .NET 6 Blazor Server
- **Language**: C# / Razor
- **UI**: Blazor Server Components
- **Architecture**: Component-based with code-behind pattern

## Project Structure
- `MainLayout.razor` - Main layout markup (refactored, now uses components)
- `MainLayout.razor.cs` - Code-behind (~33,000+ lines) containing all business logic
- `MainLayout.razor.before_split` - Source for extraction (39,265 lines, after Task 0)
- `Student.razor`, `Company.razor`, `Professor.razor`, `Admin.razor`, `ResearchGroup.razor` - Role-specific containers
- `Shared/` - Shared components organized by role:
  - `Shared/Company/` - Company-specific components (9 components)
  - `Shared/Student/` - Student-specific components (6 components)
  - `Shared/Professor/` - Professor-specific components (7 components)
  - `Shared/ResearchGroup/` - Research group components (5 components)
  - `Shared/Admin/` - Admin components (1 component)
  - `Shared/` (root) - Common components (Pagination, NewsSection, etc.)

## Task Status (from AGENTS.md)
- ✅ **Task 0**: File cleanup and anchor div restoration (Completed)
- ✅ **Task 1**: Splitting MainLayout.razor into role-specific components (Completed)
  - Student.razor: 10,010 lines
  - Company.razor: 12,391 lines  
  - Professor.razor: 11,246 lines
  - Admin.razor: 183 lines
  - ResearchGroup.razor: 3,881 lines
- ✅ **Task 2**: Extracting shared components (Substantially Complete)
  - Company.razor: 8,783 → 677 lines (92% reduction)
  - Professor.razor: 1,309 → 61 lines (95% reduction)
  - Student.razor: 8,529 → 1,211 lines (86% reduction)
  - Total: ~35,891 lines extracted across all roles

## Current Phase
**Phase 3: Wiring Components to MainLayout.razor.cs**
- Extracted components need to be wired to `MainLayout.razor.cs`
- Components need parameters passed from code-behind
- Event handlers need to be connected
- **Status**: Phase 3.1-3.3 complete, Phase 3.4 (wire role-specific components) in progress

## Key Concepts
- **Component Extraction**: Large sections extracted into separate `.razor` files
- **Code-Behind Pattern**: `MainLayout.razor.cs` contains all C# logic
- **Parameter Wiring**: Components receive data/methods via parameters
- **Event Callbacks**: Components trigger methods in code-behind via EventCallbacks
- **Anchor Divs**: Used for precise component boundaries during extraction
- **Source of Truth**: `MainLayout.razor.before_split` is the definitive source for extraction

## Component Organization Rules
- **Common code across all user roles** → `Shared/`
- **Role-specific sections** → `Shared/<Role>/ComponentName.razor`
- **Only place code in Shared/** if it is exactly the same for all user roles
- **If code is not exactly the same**, it should not be a shared component
- **If a component is only in one user role**, create folder under Shared with that user-role name
