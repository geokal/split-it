# Project: Split-It Refactoring

This project refactors a very large and historically grown Blazor MainLayout.razor that acted as a monolithic container for layout, role-based UI, and feature logic across the entire application from a .NET 6 Blazor app. This document outlines the tasks being performed to refactor the a Blazor application MainLayout.razor into separate components for different user roles (student, company, professor, admin). Additionally, it includes steps for extracting shared components and replacing manual pagination controls with a standardized component.

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
- Proceed to Phase 3: Wiring Components (see [REFACTORING_PLAN.md](REFACTORING_PLAN.md))

For completed tasks and progress updates, see [PROGRESS.md](PROGRESS.md).

## Current Task

### 2. Identifying and Extracting Shared Components

**Status:** In Progress.

## Operational Guidelines

### Command Line Tools for File Manipulation

**Recommendation:** For robust and efficient text processing and file manipulation tasks, especially for replacements and pattern matching, prefer command-line utilities such as `grep`, `sed`, and `rg` (ripgrep).

**Usage Notes:**
- `grep`: Ideal for searching text patterns within files.
- `rg` (ripgrep): A faster and more user-friendly alternative to `grep`, often preferred for large codebases.
- `sed`: Powerful for stream editing, including search-and-replace operations on specific lines or blocks of text. When using `sed` on macOS for in-place editing, remember to use `sed -i ''` to avoid creating backup files. For multi-line replacements, careful escaping of newlines (`\`) or using temporary files with `sed` commands is crucial for accuracy.

**Reasoning:** These tools offer precise control over text, handle large files efficiently, and are less prone to issues related to programming language-specific string handling or environment variations when used correctly. This approach minimizes errors during automated code modifications.

## Memories
- Shared components should follow this pattern:
- Common code across all user roles goes directly into `Shared/`.
- Role-specific sections extracted from a monolithic file are placed in a subdirectory under `Shared/` named after the role from which they were extracted (e.g., `Shared/Company/ComponentName.razor` for components extracted from `Company.razor`).
- Always update AGENTS.md and PROGRESS.md and keep them updated after changes or tasks being completed.
- Only place code in the Shared/ folder if it is exactly the same for all user roles that will use it. If the code is not exactly the same, it should not be a shared component.
- if a component is only in one user role create a folder under Shared with that user-role name and place there the extracted component
