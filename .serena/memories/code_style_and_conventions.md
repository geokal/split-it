# Code Style and Conventions

## Language
- **Primary**: C# (.NET 6)
- **Markup**: Razor (.razor files)
- **Code-Behind**: C# in .razor.cs files

## Naming Conventions
- **Components**: PascalCase (e.g., `CompanyJobsSection.razor`)
- **Parameters**: PascalCase (e.g., `CurrentPage`, `TotalPages`)
- **EventCallbacks**: PascalCase with "Callback" suffix (e.g., `OnPageChanged`)
- **Methods**: PascalCase (e.g., `LoadJobs()`, `HandleClick()`)
- **Properties**: PascalCase (e.g., `UserRole`, `IsLoading`)
- **Variables**: camelCase (e.g., `currentPage`, `totalItems`)

## File Organization
- **Role-specific components**: `Shared/<Role>/ComponentName.razor`
- **Common components**: `Shared/ComponentName.razor`
- **Code-behind**: `ComponentName.razor.cs` (same directory as .razor)

## Component Structure
```razor
@* Component documentation *@
@inject ServiceName Service

<ParameterName>Parameter</ParameterName>

<div>
  @* Markup *@
</div>

@code {
  // Component logic
}
```

## Parameter Declaration
- Use `[Parameter]` attribute for component parameters
- Use `[Parameter] public EventCallback<T> OnAction { get; set; }` for events
- Use `[Parameter] public RenderFragment? ChildContent { get; set; }` for content projection

## Code-Behind Pattern
- All business logic in `MainLayout.razor.cs`
- Components receive data and callbacks via parameters
- No direct service injection in extracted components (unless necessary)

## Blazor Conventions
- Use `@bind` for two-way binding
- Use `@onclick`, `@onchange`, `@oninput` for events
- Use `@if`, `@else`, `@foreach` for conditional rendering
- Use `@key` for list rendering optimization
