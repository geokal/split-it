# Project: Split-It Refactoring

This document outlines the tasks being performed to refactor the `split-it` Blazor application. The application is a .NET 6 Blazor app.

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

**Status:** In Progress. The `Shared.NewsSection` component has been created, and the "University News Section" and "SVSE News Section" in `Professor.razor` and `Company.razor` have been replaced with instances of this new component.
