# MainLayout.razor.cs - Role Functionality Analysis

## 🎯 Project End Goal Context

**This analysis is part of a larger refactoring effort to slim down `MainLayout.razor.cs` from ~34,018 lines to a minimal file (~500-1000 lines).**

**Target State:**
- `MainLayout.razor.cs` should contain ONLY layout-specific logic, UserRole management, and basic initialization
- **Service Layer** (centralized architecture):
  - **All database calls centralized** in service classes (currently scattered in MainLayout.razor.cs)
  - Role-specific services: `CompanyService`, `StudentService`, `ProfessorService`, etc.
  - Shared services: `AnnouncementService`, `ThesisService`, `JobService`, `InternshipService`, etc.
  - Services handle all DbContext operations, data transformation, and business rules
  - No direct DbContext access from components or MainLayout.razor.cs
- ViewModels (for component-specific logic and state management)
- Role component code-behind files (e.g., `Student.razor.cs`, `Company.razor.cs`) - minimal logic, delegates to services
- Components in `Shared/[Role]/` folders receive data via `[Parameter]` declarations, with logic provided by services
- **Shared components** in `Shared/` root folder are reusable across ALL user roles

**Current State:** All business logic remains in `MainLayout.razor.cs` (legacy monolithic structure with scattered database calls)

**Current Phase:** Wiring extracted components to MainLayout.razor.cs via parameters (Phase 3)

---

## Overview
This document maps where role-specific functionality is currently located in `MainLayout.razor.cs` (34,018 lines). The file contains mixed functionality for all user roles, with some shared infrastructure. **This mapping helps identify what needs to be extracted in future phases.**

## File Structure

### 1. Initialization and Shared Infrastructure (Lines 1-300)
- **Lines 1-9**: Research Group search properties (used by Company role)
- **Lines 10-160**: Research Group statistics and search infrastructure
- **Lines 163-179**: UserRole management and Admin visibility
- **Lines 180-214**: Shared data loading (LoadStudentsAsync)
- **Lines 217-300**: **SHARED** - Common data structures used across roles

### 2. Company Role Functionality

#### Company - Data Models and Properties (Lines ~273-400)
- `CompanyJob job` - Job creation model
- `CompanyThesis thesis` - Thesis creation model  
- `AnnouncementAsCompany announcement` - Announcement creation model
- `CompanyInternship companyInternship` - Internship creation model
- Error state properties with `AsCompany` suffix

#### Company - Job Management (Lines ~4300-4700)
- `ApplyForJobAsStudent()` - Student applying to company jobs (SHARED with Student)
- Job validation and saving methods with `AsCompany` suffix
- `HandlePublishSaveJobAsCompany()` - Line ~11701
- `HandleTemporarySaveJobAsCompany()` - Line ~11659

#### Company - Thesis Management (Lines ~4800-5100)
- `HandleThesisValidationWhenSaveThesisAsCompany()` - Line ~4843
- `SaveThesisAsCompany()` methods
- Thesis validation with `AsCompany` suffix

#### Company - Internship Management (Lines ~5000-5200)
- `HandleInternshipValidationWhenSaveInternshipAsCompany()` - Line ~5082
- `SaveInternshipAsCompany()` methods
- Internship validation with `AsCompany` suffix

#### Company - Announcement Management (Lines ~11700-11800)
- `SaveAnnouncementAsCompany()` methods
- `SaveAnnouncementAsPublished()` - Line ~11728
- `SaveAnnouncementAsUnpublished()` - Line ~11753

#### Company - Search Functionality
- Research Group search (Lines 11-135) - `AsCompany` suffix
- Student search - `AsCompanyToFindStudent` suffix
- Professor search - `AsCompanyToFindProfessor` suffix

### 3. Student Role Functionality

#### Student - Application Management (Lines ~3377-3600)
- `LoadUserThesisApplications()` - Line 3377
- `LoadUserJobApplications()` - Line 3468
- `ToggleAndLoadStudentThesisApplications()` - Line 3501
- `ToggleAndLoadStudentJobApplications()` - Line 3527

#### Student - Job Applications (Lines ~4300-4600)
- `ApplyForJobAsStudent()` - Line ~4300
- `UpdateProgressWhenApplyForJobAsStudent()` - Progress tracking
- Job application loading and management

#### Student - Internship Applications (Lines ~8000-8100)
- `ToggleAndLoadStudentInternshipApplications()` - Line 8005
- `LoadUserInternshipApplications()` - Line 8031
- Internship application management

#### Student - Thesis Applications
- Thesis application methods with `AsStudent` suffix
- Application withdrawal methods

### 4. Professor Role Functionality

#### Professor - Thesis Management
- `SaveThesisAsProfessor()` methods
- `HandleThesisValidationWhenSaveThesisAsProfessor()`
- Thesis upload and management

#### Professor - Internship Management
- `SaveInternshipAsProfessor()` methods
- Internship applicant management
- `LoadProfessorInternshipApplicantData()` - Line 4099

#### Professor - Announcement Management
- `SaveAnnouncementAsProfessor()` methods
- `HandleFileSelectedForAnnouncementAttachmentAsProfessor()` - Line 5324

#### Professor - Event Management
- `SaveEventAsProfessor()` methods
- Event creation and management

### 5. Research Group Functionality

#### Research Group - Properties (Lines 1-160)
- Research Group search properties
- Statistics visibility toggle
- Research Group data models

#### Research Group - Statistics (Lines ~148-160)
- `ToggleStatisticsVisibility()` - Line 148
- Statistics loading methods

### 6. Shared Functionality

#### Shared - Data Loading
- `LoadAreasAsync()` - Line 2135 - Used by all roles
- `LoadSkillsAsync()` - Line 2140 - Used by all roles
- `LoadJobs()` - Line 6617 - Shared job loading
- `LoadThesesAsCompany()` - Line 6649 - Company-specific
- `LoadThesesAsProfessor()` - Line 6678 - Professor-specific
- `LoadProfessors()` - Line 6716 - Shared
- `LoadCompanies()` - Line 6742 - Shared

#### Shared - File Handling
- `HandleFileSelected()` - Line 5183 - Generic file handler
- Role-specific file handlers with suffixes

#### Shared - Validation
- `HandleValidationError()` - Line 4933 - Generic validation
- Role-specific validation methods

## Code-Behind File Analysis

### Current Structure
- ✅ **Correct Pattern**: `public partial class MainLayout : LayoutComponentBase`
- ✅ **Separation**: Logic separated from markup (MainLayout.razor)
- ✅ **Encapsulation**: Private fields and methods appropriately scoped

### Recommendations

#### 1. **No Structural Changes Needed**
The code-behind file structure is correct. The file is large (33,978 lines) but follows proper Blazor code-behind patterns.

#### 2. **Consider Future Refactoring** (Optional)
While the current structure works, consider:
- **Extracting role-specific logic** into separate service classes or partial classes
- **Creating role-specific code-behind files** (e.g., `MainLayout.Company.cs`, `MainLayout.Student.cs`)
- **Using dependency injection** for role-specific services

#### 3. **Current Approach is Valid**
Since components are now extracted and wired via parameters:
- Components don't need direct access to role-specific methods
- All functionality is accessible via `[Parameter]` declarations
- The monolithic code-behind is acceptable for this legacy codebase

## Role Identification Patterns

### Naming Conventions
- **Company**: Methods/properties with `AsCompany` suffix
- **Student**: Methods/properties with `AsStudent` suffix  
- **Professor**: Methods/properties with `AsProfessor` suffix
- **Research Group**: Properties with `AsRG` or `ResearchGroup` prefix
- **Shared**: No role suffix, or used by multiple roles

### Search Patterns
```csharp
// Company search patterns
*AsCompanyToFindStudent
*AsCompanyToFindProfessor
*AsCompanyToFindResearchGroup

// Student search patterns
*AsStudent
*WhenSearchFor*

// Professor search patterns
*AsProfessor
*ForProfessor*
```

## Key Method Locations

### Company Methods
- Job Save: ~Line 11701 (`HandlePublishSaveJobAsCompany`)
- Thesis Save: ~Line 11709 (`HandlePublishSaveThesisAsCompany`)
- Internship Save: ~Line 11541 (`ChangeInternshipStatusToUnpublished`)
- Announcement Save: ~Line 11728 (`SaveAnnouncementAsPublished`)

### Student Methods
- Apply for Job: ~Line 4300 (`ApplyForJobAsStudent`)
- Load Applications: ~Line 3377 (`LoadUserThesisApplications`)

### Professor Methods
- Save Thesis: Methods with `AsProfessor` suffix
- Save Internship: Methods with `AsProfessor` suffix
- Manage Applicants: `LoadProfessorInternshipApplicantData()` - Line 4099

## Conclusion

The `MainLayout.razor.cs` file is a **monolithic code-behind** containing all role-specific functionality. While large, it:
- ✅ Follows proper Blazor code-behind patterns
- ✅ Correctly separates logic from markup
- ✅ Uses appropriate encapsulation (private fields/methods)
- ✅ Works with the component-based architecture (components receive data via parameters)

**No structural changes are required** for the code-behind file. The current approach is valid for this legacy codebase, and the extracted components properly access functionality via parameters.

