# Services Architecture Documentation

## Dashboard Services Pattern

### Services Structure
```
Services/
├── UserContext/
│   ├── IUserContextService.cs
│   ├── UserContextService.cs
│   └── UserContextState.cs
├── FrontPage/
│   ├── IFrontPageService.cs
│   ├── FrontPageService.cs
│   └── FrontPageData.cs
├── StudentDashboard/
│   ├── IStudentDashboardService.cs
│   ├── StudentDashboardService.cs
│   └── StudentDashboardData.cs
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
3. **DTO Pattern**: Immutable records with init properties
4. **Resource Management**: Always use IDbContextFactory, not direct DbContext
5. **Read Optimization**: Use .AsNoTracking() for read operations

### Migration Status
- ✅ CompanyDashboardService: 100% complete
- ✅ ProfessorDashboardService: 100% complete  
- ✅ ResearchGroupDashboardService: 100% complete
- ✅ StudentDashboardService: 95% complete (StudentAnnouncementsSection remaining)

### Component Usage
Components inject services via [Inject] attribute and call async methods for data operations.