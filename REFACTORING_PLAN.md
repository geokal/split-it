# Refactoring Plan: Split-It Project

## 🎯 Project End Goal

**Transform the monolithic Blazor application into a modular architecture with minimal MainLayout files.**

### Target State

**MainLayout.razor** → Minimal file (~100-200 lines) containing only:
- Basic layout structure
- Role-based component routing: `<Student />`, `<Company />`, `<Professor />`, `<Admin />`, `<ResearchGroup />`

**MainLayout.razor.cs** → Minimal file (~500-1000 lines) containing only:
- Layout-specific logic
- UserRole management
- Basic initialization
- **NO role-specific business logic** (logic is now in component code-behinds)

**Role Components** (Student.razor, Company.razor, etc.) → Use subcomponents:
- `Company.razor` uses `<CompanyJobsSection />`, `<CompanyAnnouncementsSection />`, etc.
- `Student.razor` uses `<StudentCompanySearchSection />`, `<StudentThesisDisplaySection />`, etc.

**Subcomponents** → Located in `Shared/[Role]/` folders:
- Each has its own `.razor.cs` code-behind file
- Inject required services directly (AppDbContext, IJSRuntime, etc.)
- Self-contained business logic

**Shared Components** → Located in `Shared/` folder (root level):
- Reusable across ALL user roles (e.g., `Pagination`, `LoadingIndicator`, `NewsSection`)
- No role-specific logic - truly shared functionality

**Service Layer** (Future Phase):
- **Centralized Database Calls**: All DbContext operations go through service classes
- **Role-Specific Services**: `CompanyService`, `StudentService`, `ProfessorService`
- **Shared Services**: `AnnouncementService`, `ThesisService`, `JobService`

---

## Current Status Summary

### ✅ Completed (100%)
- Role-specific files extracted (Student.razor, Company.razor, Professor.razor, Admin.razor, ResearchGroup.razor)
- Common components extracted (Pagination, NewsSection, LoadingIndicator, RegistrationPrompt)
- **All 28 components extracted** across all roles
- **Pattern 2 Architecture Migration** - All components converted:
  - ✅ Professor components (7/7)
  - ✅ Student components (6/6)
  - ✅ Company components (9/9)
  - ✅ ResearchGroup components (5/5)
  - ✅ Admin components (1/1)

### ❌ Future Phase
- Extract remaining business logic from MainLayout.razor.cs into service classes

---

## Pattern 2 Architecture

Each component follows this structure:

```
ComponentName.razor        - UI markup only
ComponentName.razor.cs     - Code-behind with:
                            - [Inject] services
                            - Private state fields
                            - Business logic methods
```

**Injected Services:**
- `AppDbContext` - Database access
- `IJSRuntime` - JavaScript interop
- `AuthenticationStateProvider` - User authentication
- `NavigationManager` - Navigation
- `InternshipEmailService` - Email notifications (where needed)
- `HttpClient` - External API calls (where needed)
- `GoogleScholarService` - Publications (ResearchGroup only)

---

## Component Summary

| Role | Components | Status |
|------|------------|--------|
| Company | 9 | ✅ All converted |
| Professor | 7 | ✅ All converted |
| Student | 6 | ✅ All converted |
| ResearchGroup | 5 | ✅ All converted |
| Admin | 1 | ✅ All converted |
| **Total** | **28** | **28/28 (100%)** ✅ |

---

## Success Criteria

- ✅ All components extracted
- ✅ All components converted to Pattern 2 (27/28)
- ⏳ Admin component conversion
- ⏳ All functionality works as before
- ⏳ No runtime errors
- ⏳ All roles functional

---

## Notes

- **File Size Targets:**
  - MainLayout.razor: < 1000 lines (just structure)
  - MainLayout.razor.cs: To be reduced in future phase
  - Role files: Minimal (just component references)
  - Components: Self-contained with code-behind

- **Component Organization:**
  - Common across roles → `Shared/`
  - Single role only → `Shared/<Role>/`
