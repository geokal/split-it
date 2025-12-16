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
- `Student.razor`, `Company.razor`, `Professor.razor`, `Admin.razor`, `ResearchGroup.razor` - Role-specific containers
- `Shared/` - Shared components organized by role:
  - `Shared/Company/` - Company-specific components
  - `Shared/Student/` - Student-specific components
  - `Shared/Professor/` - Professor-specific components
  - `Shared/ResearchGroup/` - Research group components
  - `Shared/Admin/` - Admin components
  - `Shared/` (root) - Common components (Pagination, NewsSection, etc.)

## Current Phase
**Phase 3: Wiring Components**
- Extracted components need to be wired to `MainLayout.razor.cs`
- Components need parameters passed from code-behind
- Event handlers need to be connected

## Key Concepts
- **Component Extraction**: Large sections extracted into separate `.razor` files
- **Code-Behind Pattern**: `MainLayout.razor.cs` contains all C# logic
- **Parameter Wiring**: Components receive data/methods via parameters
- **Event Callbacks**: Components trigger methods in code-behind via EventCallbacks
