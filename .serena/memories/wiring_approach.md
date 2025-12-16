# Wiring Approach for Components

## Current Phase
**Phase 3: Wiring Components to MainLayout.razor.cs**

## Component Status (from COMPONENT_DEPENDENCIES.md)
- ✅ **Common Components**: Pagination, NewsSection, LoadingIndicator, RegistrationPrompt (mostly wired)
- ❌ **Company Components (9)**: All extracted, none wired yet
  - CompanyAnnouncementsManagementSection, CompanyAnnouncementsSection
  - CompanyJobsSection, CompanyInternshipsSection, CompanyThesesSection
  - CompanyEventsSection, CompanyStudentSearchSection
  - CompanyProfessorSearchSection, CompanyResearchGroupSearchSection
- ❌ **Professor Components (7)**: All extracted, none wired yet
  - ProfessorAnnouncementsManagementSection, ProfessorThesesSection
  - ProfessorInternshipsSection, ProfessorEventsSection
  - ProfessorStudentSearchSection, ProfessorCompanySearchSection, ProfessorResearchGroupSearchSection
- ❌ **Student Components (6)**: All extracted, none wired yet
  - StudentCompanySearchSection, StudentAnnouncementsSection
  - StudentThesisDisplaySection, StudentJobsDisplaySection
  - StudentInternshipsSection, StudentEventsSection
- ❌ **ResearchGroup Components (5)**: All extracted, none wired yet
  - ResearchGroupAnnouncementsSection, ResearchGroupEventsSection
  - ResearchGroupCompanySearchSection, ResearchGroupProfessorSearchSection, ResearchGroupStatisticsSection
- ❌ **Admin Components (1)**: Extracted, not wired yet
  - AdminSection

## Strategy
1. **Identify Dependencies**: For each component, find what it needs from code-behind
2. **Map Symbols**: Use Serena tools to find properties/methods in `MainLayout.razor.cs`
3. **Define Parameters**: Add `[Parameter]` declarations to components
4. **Pass Parameters**: Update parent components to pass data/callbacks
5. **Verify**: Ensure all bindings and events are connected

## Component Hierarchy
```
MainLayout.razor
  └─> Company.razor
        └─> Shared/Company/CompanyJobsSection.razor
        └─> Shared/Company/CompanyInternshipsSection.razor
        └─> ... (other Company components)
```

## Parameter Types
- **Data Properties**: Lists, models, state variables
- **EventCallbacks**: Methods to handle user actions
- **Configuration**: Flags, settings, user role

## Parameter Contracts (from PARAMETER_CONTRACTS.md)
Each component needs:
- **Data Properties**: Lists, models, state variables
- **EventCallbacks**: Methods to handle user actions
- **Configuration**: Flags, settings, user role

See `PARAMETER_CONTRACTS.md` for detailed parameter lists for each component. Example for CompanyAnnouncementsManagementSection:
- Form state (visibility toggles, loading states)
- Announcement model with two-way binding
- Validation properties
- Loading/progress indicators
- Event handlers (character limits, file upload, save operations)
- Uploaded announcements management (filtering, pagination, counts)
- Bulk operations (edit mode, selection, status changes, copy)

## Serena Tools for Wiring
- `find_symbol` - Find properties/methods in code-behind
- `find_referencing_symbols` - Find where symbols are used
- `search_for_pattern` - Search for binding patterns (`@bind`, `@onclick`)
- `get_symbols_overview` - Get overview of symbols in file

## Example Wiring Pattern
```razor
@* In CompanyJobsSection.razor *@
[Parameter] public List<Job> Jobs { get; set; }
[Parameter] public EventCallback<int> OnJobClick { get; set; }

@* In Company.razor *@
<CompanyJobsSection Jobs="@jobs" OnJobClick="@HandleJobClick" />

@* In MainLayout.razor.cs *@
private List<Job> jobs = new();
private async Task HandleJobClick(int jobId) { ... }
```

## Key Files
- `MainLayout.razor.cs` - Source of all properties/methods
- `COMPONENT_DEPENDENCIES.md` - Documents required parameters
- `PARAMETER_CONTRACTS.md` - Defines parameter contracts

## Implementation Strategy (from PARAMETER_CONTRACTS.md)
1. **For each component**:
   - Extract all property/method references from the component file
   - Map to MainLayout.razor.cs properties/methods
   - Create `[Parameter]` declarations
   - Update component to use parameters instead of direct references

2. **For MainLayout.razor**:
   - Pass parameters to each component instance
   - Ensure all required parameters are provided

3. **Testing**:
   - Verify each component receives correct data
   - Test event handlers
   - Verify form submissions work
   - Test pagination and filtering
