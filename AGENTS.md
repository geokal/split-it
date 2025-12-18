# Project: Split-It Refactoring

## 🎯 Project End Goal

**The primary objective of this refactoring project is to transform the monolithic Blazor application into a modular, maintainable architecture by slimming down `MainLayout.razor` and `MainLayout.razor.cs` to minimal files that follow standard Blazor Server application patterns.**

### Target Architecture

In a normal, well-structured Blazor Server application, `MainLayout.razor` and `MainLayout.razor.cs` should be **minimal** files that only handle:
- Basic layout structure (navigation, footer, etc.)
- Route rendering (`@Body`)
- User authentication/authorization checks
- Role-based component routing

### Current State vs. Target State

**Current State (Monolithic):**
```
MainLayout.razor (~5,441 lines) → Contains role-specific markup
MainLayout.razor.cs (~34,018 lines) → Contains ALL business logic for all roles
```

**Target State (Modular):**
```
MainLayout.razor (minimal, ~100-200 lines)
  └─> <Student /> component
  └─> <Company /> component
  └─> <Professor /> component
  └─> <Admin /> component
  └─> <ResearchGroup /> component

MainLayout.razor.cs (minimal, ~500-1000 lines)
  └─> Only layout-specific logic
  └─> UserRole management
  └─> Basic initialization
  └─> NO role-specific business logic

Student.razor (moderate size, uses subcomponents)
  └─> <StudentCompanySearchSection />
  └─> <StudentAnnouncementsSection />
  └─> <StudentThesisDisplaySection />
  └─> <StudentJobsDisplaySection />
  └─> <StudentInternshipsSection />
  └─> <StudentEventsSection />

Company.razor (moderate size, uses subcomponents)
  └─> <CompanyAnnouncementsSection />
  └─> <CompanyAnnouncementsManagementSection />
  └─> <CompanyJobsSection />
  └─> <CompanyInternshipsSection />
  └─> <CompanyThesesSection />
  └─> <CompanyEventsSection />
  └─> <CompanyStudentSearchSection />
  └─> <CompanyProfessorSearchSection />
  └─> <CompanyResearchGroupSearchSection />

[Similar structure for Professor, Admin, ResearchGroup]
```

### Component Hierarchy

```
MainLayout.razor (MINIMAL)
  └─> Role Components (Student.razor, Company.razor, etc.)
       └─> Feature Subcomponents (CompanyJobsSection.razor, etc.)
            └─> Shared Components (Pagination, LoadingIndicator, etc.) ← Reusable across ALL roles

Service Layer (Future):
  └─> Role-Specific Services (CompanyService, StudentService, etc.)
  └─> Shared Services (AnnouncementService, ThesisService, etc.)
       └─> All database calls centralized here (DbContext operations)
```

### Why This Architecture?

1. **Maintainability**: Each component has a single responsibility
2. **Scalability**: Easy to add new features without touching MainLayout
3. **Testability**: Components can be tested in isolation
4. **Readability**: Developers can find code quickly
5. **Standard Pattern**: Follows Blazor best practices
6. **Centralized Data Access**: All database calls go through service classes (not scattered in MainLayout.razor.cs)
7. **Reusability**: Shared components and services can be used across all user roles
8. **Separation of Concerns**: UI components, business logic, and data access are properly separated

### Current Work: Component Architecture Migration

**Phase 3: Component Architecture Migration (Current Phase) - Pattern 2 (Smart Components)**

**Architecture Decision:** The project has shifted from Pattern 1 (dumb components with parameters) to Pattern 2 (smart components with code-behind and injected services).

**Pattern 2 Approach:**
- Each component has its own `.razor.cs` code-behind file
- Components inject required services directly (AppDbContext, IJSRuntime, AuthenticationStateProvider, NavigationManager, InternshipEmailService)
- All business logic is moved from `MainLayout.razor.cs` to component code-behind files
- Parent components (e.g., `Professor.razor`, `Student.razor`) simply reference child components without passing parameters
- This provides better separation of concerns, easier testing, and more maintainable code

**Current Status:**
- ✅ **Professor Components (7/7)**: All converted to Pattern 2
- ✅ **Student Components (6/6)**: All converted to Pattern 2
- ✅ **Company Components (9/9)**: All converted to Pattern 2
- ✅ **ResearchGroup Components (5/5)**: All converted to Pattern 2
- ✅ **Admin Components (1/1)**: All converted to Pattern 2

**Phase 3 Complete: 28/28 components converted to Pattern 2** ✅

**Future Phase: Logic Extraction & Service Architecture**
- Eventually, business logic should be extracted from `MainLayout.razor.cs` into:
  - **Service classes** (for centralized data access and business logic)
    - Centralize all database calls (currently scattered throughout MainLayout.razor.cs)
    - Create role-specific services (e.g., `CompanyService`, `StudentService`, `ProfessorService`)
    - Create shared services for common operations (e.g., `AnnouncementService`, `ThesisService`)
    - Services handle all DbContext operations, data transformation, and business rules
  - **ViewModels** (for component-specific logic and state management)
    - ViewModels manage component state and coordinate with services
    - Reduce direct DbContext access from components
  - **Dedicated code-behind files** for role components (e.g., `Student.razor.cs`, `Company.razor.cs`)
    - Move role-specific logic out of MainLayout.razor.cs
    - Each role component has its own code-behind with minimal logic (delegates to services)
- This will further reduce `MainLayout.razor.cs` size and create a maintainable, testable architecture

---

## Project Overview

This project refactors a very large and historically grown Blazor MainLayout.razor that acted as a monolithic container for layout, role-based UI, and feature logic across the entire application from a .NET 6 Blazor app. This document outlines the tasks being performed to refactor the Blazor application MainLayout.razor into separate components for different user roles (student, company, professor, admin). Additionally, it includes steps for extracting shared components and replacing manual pagination controls with a standardized component.

## Task History

### 0. File Cleanup and Anchor Div Restoration (Preliminary Task)

**Goal:** Ensure `MainLayout.razor` is complete and properly formatted with anchor divs before proceeding with refactoring tasks.

**When to perform:** If `MainLayout.razor` appears truncated (significantly fewer lines than expected) or if anchor comments need to be converted to anchor divs.

**Steps:**

1. **Verify file integrity:**
   - Check line count: `wc -l MainLayout.razor`
   - Compare with backup: `wc -l Initial_MainLayout.razor.bak`
   - If `MainLayout.razor` is truncated (e.g., ~19,000 lines instead of ~39,000), restore from backup:
     ```bash
     cp Initial_MainLayout.razor.bak MainLayout.razor
     ```

2. **Remove huge comment lines:**
   - Find all huge comment lines with many dashes: `grep -n '@\*.*-{20,}.*\*@' MainLayout.razor`
   - Remove them using `sed` or manual editing
   - Example: Remove line with pattern `<!---{20,}.*--->`

3. **Replace anchor comments with anchor divs:**
   - Find all anchor comments: `grep -n '@\*.*-{10,}.*\*@' MainLayout.razor`
   - Replace each anchor comment with its corresponding `<div id="..."></div>` according to the mappings below
   - Use `sed` for batch replacements or manual search/replace for precision

**Anchor Comment to Div Mappings:**
- `@*---------------------------------------------------------------------------------------- STUDENTS TAB TABLE START ---------------------------------------------------------------------------------------------------------------------------------------------*@` → `<div id="students-tab-table-start"></div>`
- `@*---------------------------------------------------------------------------------------- STUDENTS TAB TABLE END ---------------------------------------------------------------------------------------------------------------------------------------------*@` → `<div id="students-tab-table-end"></div>`
- `@*---------------------------------------------------------------------------------------------------------- COMPANIES START------------------------------------------------------------------------------------------------------------*@` → `<div id="companies-start"></div>`
- `@*--------------------------------------------------------------------------- ΕΝΕΡΓΕΣ ΘΕΣΕΙΣ ΕΡΓΑΣΙΑΣ COMPANY -----------------------------------------------------------------------------*@` → `<div id="energes-theseis-ergasias-company"></div>`
- `@*----------------------------------------------------------------------------------------------------------JOBS TAB COMPANIES END------------------------------------------------------------------------------------------------------------*@` → `<div id="jobs-tab-companies-end"></div>`
- `@*----------------------------------------------------------------------------------------------------------INTERNSHIPS TAB COMPANIES START------------------------------------------------------------------------------------------------------------*@` → `<div id="internships-tab-companies-start"></div>`
- `@*-------------------------------------------------------------------------------------SHOW MY UPLOADED INTERNSHIPS AS COMPANY---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*@` → `<div id="show-my-uploaded-internships-as-company"></div>`
- `@*----------------------------------------------------------------------------------------------------------INTERNSHIPS TAB COMPANIES END------------------------------------------------------------------------------------------------------------*@` → `<div id="internships-tab-companies-end"></div>`
- `@*----------------------------------------------------------------------------------------------------------THESIS COMPANIES START------------------------------------------------------------------------------------------------------------*@` → `<div id="thesis-companies-start"></div>`
- `@*---------------------------------------------------------------------------------------------------------- COMPANY THESIS END------------------------------------------------------------------------------------------------------------*@` → `<div id="company-thesis-end"></div>`
- `@*----------------------------------------------------------------------------------------------------------COMPANY EVENTS START------------------------------------------------------------------------------------------------------------*@` → `<div id="company-events-start"></div>`
- `@*----------------------------------------------------------------------------------------------------------COMPANY EVENTS END------------------------------------------------------------------------------------------------------------*@` → `<div id="company-events-end"></div>`
- `@*----------------------------------------------------------------------------------------------------------COMPANY STUDENT SEARCH START------------------------------------------------------------------------------------------------------------*@` → `<div id="company-student-search-start"></div>`
- `@*----------------------------------------------------------------------------------------------------------COMPANY STUDENT SEARCH END------------------------------------------------------------------------------------------------------------*@` → `<div id="company-student-search-end"></div>`
- `@*----------------------------------------------------------------------------------------------------------COMPANY FOR PROFESSOR SEARCH TAB START------------------------------------------------------------------------------------------------------------*@` → `<div id="company-for-professor-search-tab-start"></div>`
- `@*---------------------------------------------------------------------------------------- PROFESSORS TAB TABLE START ---------------------------------------------------------------------------------------------------------------------------------------------*@` → `<div id="professors-tab-table-start"></div>`
- `@*--------------------------------------------------------------------- PROFESSOR PTYXIAKES - DIMIOURGIA PTYXIAKIS ------------------------------------------------------------------------------------------------------------------------------------*@` → `<div id="professor-ptyxiakes-dimiourgia-ptyxiakis"></div>`
- `@*-------------------------------------------------------------------------------------SHOW MY UPLOADED INTERNSHIPS AS PROFESSOR ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*@` → `<div id="show-my-uploaded-internships-as-professor"></div>`
- `@*---------------------------------------------------------------------------------------- PROFESSORS TAB TABLE END ---------------------------------------------------------------------------------------------------------------------------------------------*@` → `<div id="professors-tab-table-end"></div>`
- `@*---------------------------------------------------------------------------------------- ADMIN TABLE END ---------------------------------------------------------------------------------------------------------------------------------------------*@` → `<div id="admin-table-end"></div>`

**Verification:**
- Verify all anchor divs are present: `grep 'id=".*-start"\|id=".*-end"' MainLayout.razor | wc -l` (should find 16 anchor divs)
- Verify no huge comment lines remain: `grep '@\*.*-{20,}.*\*@' MainLayout.razor` (should return no results)
- Check file line count matches expected (~39,265 lines)

**Status:** Completed.

## Current Task

### 1. Splitting `MainLayout.razor` into Role-Specific Components

**Goal:** Deconstruct the monolithic `MainLayout.razor` file into smaller, more manageable components based on user roles.

**Method:**
- Identify blocks of code within `MainLayout.razor` that are specific to a user role (e.g., `Student`, `Company`, `Professor`, `Admin`). These are typically enclosed in `@if (UserRole == "...")` statements.
- Extract the content of each role-specific `if` block into a new `.razor` file named after the corresponding role (e.g., `Student.razor`, `Company.razor`).
- To aid in this process, specific comments in the original file were designated as anchors and replaced with `div` elements with unique IDs.

**Anchor `div` Mappings:**
- `STUDENTS TAB TABLE START` -> `<div id="students-tab-table-start"></div>`
- `STUDENTS TAB TABLE END` -> `<div id="students-tab-table-end"></div>`
- `COMPANIES START` -> `<div id="companies-start"></div>`
- `ΕΝΕΡΓΕΣ ΘΕΣΕΙΣ ΕΡΓΑΣΙΑΣ COMPANY` -> `<div id="energes-theseis-ergasias-company"></div>`
- `JOBS TAB COMPANIES END` -> `<div id="jobs-tab-companies-end"></div>`
- `INTERNSHIPS TAB COMPANIES START` -> `<div id="internships-tab-companies-start"></div>`
- `SHOW MY UPLOADED INTERNSHIPS AS COMPANY` -> `<div id="show-my-uploaded-internships-as-company"></div>`
- `INTERNSHIPS TAB COMPANIES END` -> `<div id="internships-tab-companies-end"></div>`
- `THESIS COMPANIES START` -> `<div id="thesis-companies-start"></div>`
- `COMPANY THESIS END` -> `<div id="company-thesis-end"></div>`
- `COMPANY EVENTS START` -> `<div id="company-events-start"></div>`
- `COMPANY EVENTS END` -> `<div id="company-events-end"></div>`
- `COMPANY STUDENT SEARCH START` -> `<div id="company-student-search-start"></div>`
- `COMPANY STUDENT SEARCH END` -> `<div id="company-student-search-end"></div>`
- `COMPANY FOR PROFESSOR SEARCH TAB START` -> `<div id="company-for-professor-search-tab-start"></div>`
- `PROFESSORS TAB TABLE START` -> `<div id="professors-tab-table-start"></div>`
- `PROFESSOR PTYXIAKES - DIMIOURGIA PTYXIAKIS` -> `<div id="professor-ptyxiakes-dimiourgia-ptyxiakis"></div>`
- `SHOW MY UPLOADED INTERNSHIPS AS PROFESSOR` -> `<div id="show-my-uploaded-internships-as-professor"></div>`
- `PROFESSORS TAB TABLE END` -> `<div id="professors-tab-table-end"></div>`
- `ADMIN TABLE END` -> `<div id="admin-table-end"></div>`

**Status:** ✅ Re-extracted and Verified Complete (2024-12-19)

**Action Taken:**
- ✅ Extracted role-specific sections from `MainLayout.razor.before_split` (39,265 lines)
- ✅ Replaced component files with fresh extractions:
  - `Student.razor`: 10,010 lines (extracted from lines 1540-11549)
  - `Company.razor`: 12,391 lines (extracted from lines 11554-23944)
  - `Professor.razor`: 11,246 lines (extracted from lines 23945-35190)
  - `Admin.razor`: 183 lines (extracted from lines 35191-35373)

**Verification Results:**
- ✅ `MainLayout.razor` contains component references: `<Student />`, `<Company />`, `<Professor />`, `<Admin />`
- ✅ No inline role-specific code blocks found in `MainLayout.razor` (0 `@if (UserRole == "...")` blocks)
- ✅ All component files now match the current `MainLayout.razor.before_split` source
- ✅ `MainLayout.razor` reduced from ~39,265 lines to 5,441 lines (86% reduction)

**Note:** Task 1 was re-done to ensure component files match the current MainLayout.razor structure.



### 2. Identifying and Extracting Shared Components

**Goal:** Analyze the four newly created Razor files (`Student.razor`, `Company.razor`, `Professor.razor`, and `Admin.razor`) and identify common, reusable code that can be extracted into shared Blazor components within a `Shared/` folder.

**Motivation:** This will reduce code duplication and improve the maintainability of the application, following the principles of .NET 8 Blazor server development. After refactoring and placing common markup code to `/Shared`, we will wire the code-behind (`MainLayout.razor.cs`, which contains the extracted `@code{}` section from the original ~780000 line MainLayout.razor) to the refactored Razor components.

**Strategic Plan:** See [REFACTORING_PLAN.md](REFACTORING_PLAN.md) for a comprehensive plan with priorities, phases, and execution order. The plan recommends completing component extraction before wiring the code-behind.

**Status:** ✅ Substantially Complete.

**Completed Work:**
- ✅ All major role-specific sections extracted (Phase 2) - see [PROGRESS.md](PROGRESS.md)
- ✅ Common components extracted (Pagination, NewsSection, LoadingIndicator, RegistrationPrompt)
- ✅ Pagination standardized across all components (including `CompanyUploadedJobsSection.razor` fix)
- ✅ Analysis of remaining patterns documented in [SHARED_PATTERNS_ANALYSIS.md](SHARED_PATTERNS_ANALYSIS.md)

**Impact:**
- **Company.razor**: 8,783 → 677 lines (92% reduction)
- **Professor.razor**: 1,309 → 61 lines (95% reduction)
- **Student.razor**: 8,529 → 1,211 lines (86% reduction)
- **Total lines extracted**: ~17,000+ lines across all role-specific files

**Remaining Opportunities:**
- Minor patterns identified (modals, buttons, collapsible sections) but not recommended for extraction due to significant variations. See [SHARED_PATTERNS_ANALYSIS.md](SHARED_PATTERNS_ANALYSIS.md) for detailed analysis.

**Next Steps:**
- ✅ Phase 3 Complete - All 28 components converted to Pattern 2
- ⏳ Testing and validation
- ⏳ Future: Extract business logic from MainLayout.razor.cs into service classes

For completed tasks and progress updates, see [PROGRESS.md](PROGRESS.md).

## Current Status

### Phase 3: Component Architecture Migration

**Status:** ✅ Complete (28/28 components converted to Pattern 2)

All components now have their own `.razor.cs` code-behind files with injected services. No parameters are passed from parent components.

## Operational Guidelines

### Command Line Tools for File Manipulation

**Recommendation:** For robust and efficient text processing and file manipulation tasks, especially for replacements and pattern matching, prefer command-line utilities such as `grep`, `sed`, and `rg` (ripgrep).

**Usage Notes:**
- `grep`: Ideal for searching text patterns within files.
- `rg` (ripgrep): A faster and more user-friendly alternative to `grep`, often preferred for large codebases.
- `sed`: Powerful for stream editing, including search-and-replace operations on specific lines or blocks of text. When using `sed` on macOS for in-place editing, remember to use `sed -i ''` to avoid creating backup files. For multi-line replacements, careful escaping of newlines (`\`) or using temporary files with `sed` commands is crucial for accuracy.

**Reasoning:** These tools offer precise control over text, handle large files efficiently, and are less prone to issues related to programming language-specific string handling or environment variations when used correctly. This approach minimizes errors during automated code modifications.

## Guidelines

### Component Organization
- Common code across all user roles → `Shared/` root folder
- Role-specific components → `Shared/[Role]/` (e.g., `Shared/Company/`)

### Pattern 2 Architecture
Each component has:
- `.razor` file: UI markup only (no `@code` section)
- `.razor.cs` file: Code-behind with `[Inject]` services and all logic

### Documentation
- Always update `AGENTS.md` and `PROGRESS.md` after changes
- See `REFACTORING_PLAN.md` for architecture details
