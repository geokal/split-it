# Project Structure

## Root Level
- `MainLayout.razor` - Main layout (uses components)
- `MainLayout.razor.cs` - Code-behind (~33,000+ lines)
- `MainLayout.razor.before_split` - Source for extraction (39,265 lines)
- `Initial_MainLayout.razor.bak` - Original backup (before any changes)

## Role-Specific Containers
- `Student.razor` - Student role container
- `Company.razor` - Company role container
- `Professor.razor` - Professor role container
- `Admin.razor` - Admin role container
- `ResearchGroup.razor` - Research group role container

## Shared Components

### Common (Used by Multiple Roles)
- `Shared/Pagination.razor` - Reusable pagination
- `Shared/NewsSection.razor` - News display
- `Shared/LoadingIndicator.razor` - Loading state
- `Shared/RegistrationPrompt.razor` - Registration UI

### Role-Specific (Extracted from Role Files)
- `Shared/Company/` - 9 components (Jobs, Internships, Theses, Events, etc.)
- `Shared/Student/` - 6 components (Company Search, Announcements, Jobs, etc.)
- `Shared/Professor/` - 7 components (Theses, Internships, Events, etc.)
- `Shared/ResearchGroup/` - 5 components (Announcements, Events, Search, etc.)
- `Shared/Admin/` - 1 component (AdminSection)

## Documentation
- `AGENTS.md` - Project tasks and guidelines
- `PROGRESS.md` - Task completion tracking
- `REFACTORING_PLAN.md` - Strategic refactoring plan
- `COMPONENT_DEPENDENCIES.md` - Component wiring dependencies
- `EXTRACTION_STANDARD.md` - Component extraction guidelines
- `PARAMETER_CONTRACTS.md` - Parameter definitions
- `WIRING_TEST_PLAN.md` - Wiring testing approach

## Key Patterns
- **Extraction Source**: Always use `MainLayout.razor.before_split` as source
- **Boundaries**: Use anchor divs (`<div id="...-start"></div>`) when available
- **Verification**: Use MD5 checksums and line counts for verification
- **Wiring**: Components receive data/callbacks via parameters from code-behind
