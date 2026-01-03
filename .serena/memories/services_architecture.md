# Services Architecture Documentation

## Dashboard Services Pattern

### Services Structure
```
Services/
├── UserContext/
│   ├── IUserContextService.cs          # User authentication state
│   ├── UserContextService.cs
│   └── UserContextState.cs             # Record with user data
├── FrontPage/
│   ├── IFrontPageService.cs            # Front page data loading
│   ├── FrontPageService.cs             # Public events & announcements
│   └── FrontPageData.cs                # DTO for front page data
├── StudentDashboard/
│   ├── IStudentDashboardService.cs     # Student operations interface
│   ├── StudentDashboardService.cs      # All student DB operations
│   └── StudentDashboardData.cs         # DTO with student data
├── CompanyDashboard/
│   ├── ICompanyDashboardService.cs
│   ├── CompanyDashboardService.cs
│   └── CompanyDashboardData.cs
├── ProfessorDashboard/
│   ├── IProfessorDashboardService.cs
│   ├── ProfessorDashboardService.cs
│   └── ProfessorDashboardData.cs
└── ResearchGroupDashboard/
    ├── IResearchGroupDashboardService.cs
    ├── ResearchGroupDashboardService.cs
    └── ResearchGroupDashboardData.cs
```

### Key Principles
1. **Service Injection**: AuthenticationStateProvider, IDbContextFactory<AppDbContext>, ILogger<T>
2. **Async Methods**: All methods async with CancellationToken support
3. **DTO Pattern**: Immutable records with init properties and `Empty` static property
4. **Resource Management**: Always use IDbContextFactory, not direct DbContext
5. **Read Optimization**: Use .AsNoTracking() for read operations
6. **Service Registration**: Registered as Scoped in Program.cs

### Migration Status
- ✅ CompanyDashboardService: 100% complete (all sections service-backed)
- ✅ ProfessorDashboardService: 100% complete (all sections service-backed)
- ✅ ResearchGroupDashboardService: 100% complete (all sections service-backed)
- ✅ StudentDashboardService: 95% complete (StudentAnnouncementsSection still uses IDbContextFactory directly)

### Component Usage Pattern
```csharp
[Inject] private ICompanyDashboardService CompanyDashboardService { get; set; } = default!;

protected override async Task OnInitializedAsync()
{
    var data = await CompanyDashboardService.LoadDashboardDataAsync();
    // Use data...
}
```

### MainLayout Pattern
- MainLayout.razor.cs is minimal (127 lines)
- Only handles: auth state, front page data, navigation helpers
- NOT responsible for: database queries, business logic, role-specific data loading
- Uses IUserContextService for user state
- Uses IFrontPageService for front page data
