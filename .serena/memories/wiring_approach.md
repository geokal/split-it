# Wiring Approach for Components

## Current Phase
**Phase 3: Wiring Components to MainLayout.razor.cs**

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
