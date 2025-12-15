# Project: Split-It Refactoring

This project refactors a very large and historically grown Blazor MainLayout.razor that acted as a monolithic container for layout, role-based UI, and feature logic across the entire application from a .NET 6 Blazor app. This document outlines the tasks being performed to refactor the a Blazor application MainLayout.razor into separate components for different user roles (student, company, professor, admin). Additionally, it includes steps for extracting shared components and replacing manual pagination controls with a standardized component.

## Task History

### 1. Splitting `New_MainLayout.razor` into Role-Specific Components

**Goal:** Deconstruct the monolithic `New_MainLayout.razor` file into smaller, more manageable components based on user roles.

**Method:**
- Identify blocks of code within `New_MainLayout.razor` that are specific to a user role (e.g., `Student`, `Company`, `Professor`, `Admin`). These are typically enclosed in `@if (UserRole == "...")` statements.
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

**Status:** Completed.

## Current Task

### 2. Identifying and Extracting Shared Components

**Goal:** Analyze the four newly created Razor files (`Student.razor`, `Company.razor`, `Professor.razor`, and `Admin.razor`) and identify common, reusable code that can be extracted into shared Blazor components within a `Shared/` folder.

**Motivation:** This will reduce code duplication and improve the maintainability of the application, following the principles of .NET 8 Blazor server development. After refactoring and placing common markup code to `/Shared`, we will add the code-behind code for the monolithic `MainLayout.razor` (which will be `New_MainLayout.razor` renamed).

## Progress Update

### 3. Pagination Refactoring

**Goal:** Replace all manual pagination controls in `Admin.razor`, `Company.razor`, `Professor.razor`, and `Student.razor` with the `Shared.Pagination` component to standardize the pagination logic and improve maintainability.

**Status:** Completed. All manual pagination controls have been replaced, and previously untracked files (`AGENTS.md`, `ResearchGroup.razor`, and `Shared/`) have been added to the repository.

### 4. Extracting News Section Component

**Goal:** Extract repetitive news display logic in `Professor.razor` and `Company.razor` into a reusable `Shared.NewsSection` component. This component encapsulates the UI and logic for displaying a list of news articles with a toggleable section.

**Status:** Completed. The `Shared.NewsSection` component has been created, and the "University News Section" and "SVSE News Section" in `Professor.razor` and `Company.razor` have been replaced with instances of this new component.

### 5. Extracting Announcement Sections into Shared Components

**Goal:** Extract repetitive announcement display logic in `Company.razor`, `Professor.razor`, and `Student.razor` into reusable shared components.

**Status:** Completed.
- The "Platform company announcements" section in `Company.razor` has been extracted into `Shared.CompanyAnnouncementsSection.razor` and integrated.
- The "Platform professor announcements" section in `Company.razor` has been extracted into `Shared.ProfessorAnnouncementsSection.razor` and integrated.
- The "Platform research group announcements" section in `Company.razor` has been extracted into `Shared.ResearchGroupAnnouncementsSection.razor` and integrated.

### 6. Consolidating CompanyInternshipsSection Component
**Goal:** Consolidate duplicate `CompanyInternshipsSection.razor` files to maintain a single, canonical version and adhere to role-specific shared component guidelines.
**Status:** Completed. Identified a duplicate `CompanyInternshipsSection.razor` in the root `Shared/` folder and an empty/incorrect one in `Shared/Company/`. The functional component from `Shared/` was moved to `Shared/Company/CompanyInternshipsSection.razor`, replacing the empty file. No active references to either component were found in the project.

### 7. Extracting Professor Search Section into Shared Component

**Goal:** Extract the professor search section from `Company.razor` into a reusable `Shared/Professor/ProfessorSearchSection.razor` component.

**Status:** Completed. The `ProfessorSearchSection.razor` component has been created in the `Shared/Professor` directory and integrated into `Company.razor`.

### 8. Extracting Research Group Search Section into Shared Component

**Goal:** Extract the research group search section from `Company.razor` into a reusable `Shared/Company/ResearchGroupSearchSection.razor` component.

**Status:** Completed. The `ResearchGroupSearchSection.razor` component has been created in the `Shared/Company` directory and integrated into `Company.razor`.

### 9. Extracting Student Company Search Section into Shared Component

**Goal:** Extract the company search section from `Student.razor` into a reusable `Shared/Student/StudentCompanySearchSection.razor` component.

**Status:** Completed. The `StudentCompanySearchSection.razor` component has been created in the `Shared/Student` directory and integrated into `Student.razor`.

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
- Always update AGENTS.md and keep it updated after changes or tasks being completed.
- Only place code in the Shared/ folder if it is exactly the same for all user roles that will use it. If the code is not exactly the same, it should not be a shared component.
- if a component is only in one user role create a folder under Shared with that user-role name and place there the extracted component
