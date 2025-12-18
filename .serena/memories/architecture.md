# Split-It Architecture (Complete)

## Structure
- `MainLayout.razor` (1,557 lines): Layout + role component references
- 5 role components: Student, Company, Professor, Admin, ResearchGroup
- 28 subcomponents in `Shared/[Role]/` folders with Pattern 2 architecture

## Pattern 2 Architecture
Each component has:
- `.razor`: UI markup only
- `.razor.cs`: Code-behind with `[Inject]` services (AppDbContext, IJSRuntime, AuthenticationStateProvider, NavigationManager, InternshipEmailService)

## Backups
Original markup in `backups/` folder (verified complete)

## Next Steps
- Testing and validation
- Future: Extract logic from MainLayout.razor.cs into service classes