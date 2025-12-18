# Pattern 2: Smart Components with Code-Behind

## Architecture Decision

The project has shifted from Pattern 1 (dumb components receiving all logic via parameters) to Pattern 2 (smart components with their own code-behind files and injected services).

## Pattern 2 Structure

Each component follows this structure:
- **ComponentName.razor**: Markup only (no `@code` section)
- **ComponentName.razor.cs**: Code-behind with:
  - Injected services via `[Inject]` attributes
  - All component state (private fields)
  - All component logic (methods, event handlers)
  - No `[Parameter]` declarations needed

## Injected Services (Common)

Most components inject these services:
- `AppDbContext dbContext` - Database access
- `IJSRuntime JS` - JavaScript interop
- `AuthenticationStateProvider AuthenticationStateProvider` - User authentication
- `NavigationManager NavigationManager` - Navigation
- `InternshipEmailService InternshipEmailService` - Email notifications (for internship/thesis components)

## Conversion Pattern

1. **Create code-behind file**: `ComponentName.razor.cs`
2. **Define class**: `public partial class ComponentName : ComponentBase`
3. **Inject services**: Add `[Inject]` properties for required services
4. **Move state**: Copy all private fields from MainLayout.razor.cs related to this component
5. **Move logic**: Copy all methods from MainLayout.razor.cs related to this component
6. **Add helper methods**: Copy helper methods (e.g., `GetStudentDetails`, `SafeInvokeJsAsync`)
7. **Remove `@code` section**: Delete `@code` block from `.razor` file
8. **Update parent component**: Remove all parameters from component reference in parent (e.g., `Professor.razor`, `Student.razor`)
9. **Namespace**: Use `SplitIt.Shared.[Role]` namespace (e.g., `SplitIt.Shared.Professor`)

## Completed Conversions (27/28)

### Professor (7/7) ✅
### Student (6/6) ✅
### Company (9/9) ✅
### ResearchGroup (5/5) ✅

## All Conversions Complete ✅

All 28 components converted to Pattern 2.

## Benefits

1. **Separation of Concerns**: Each component manages its own state and logic
2. **Testability**: Components can be tested in isolation
3. **Maintainability**: Logic is co-located with UI
4. **No Parameter Passing**: Parent components don't need to pass dozens of parameters
5. **Service Injection**: Direct access to services (no dependency on MainLayout.razor.cs)
6. **Future Service Layer**: Easy to extract logic to service classes later

## Example

```csharp
// ProfessorThesesSection.razor.cs
namespace SplitIt.Shared.Professor
{
    public partial class ProfessorThesesSection : ComponentBase
    {
        [Inject] private AppDbContext dbContext { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        private string CurrentUserEmail = "";
        private List<ProfessorThesis> professorTheses = new();
        // ... all state and logic
    }
}
```

```razor
@* Professor.razor *@
<ProfessorThesesSection />
```
