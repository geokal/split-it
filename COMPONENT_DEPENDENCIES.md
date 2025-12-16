# Component Dependencies Mapping

This document maps the dependencies between extracted components and the `MainLayout.razor.cs` code-behind file. It identifies which properties, methods, and event handlers each component needs to function properly.

## Overview

The refactoring extracted large sections from role-specific Razor files into reusable components. These components need to be wired to the code-behind (`MainLayout.razor.cs`) to access:
- Data properties (lists, models, state)
- Event handlers (click, change, toggle)
- Navigation and business logic methods
- Service injections (already handled via `@inject` in components)

## Analysis Approach

1. **Component Parameter Analysis**: Examine each extracted component to identify `[Parameter]` declarations
2. **Code-Behind Mapping**: Find corresponding properties/methods in `MainLayout.razor.cs`
3. **Dependency Documentation**: Document required parameters for each component
4. **Wiring Strategy**: Plan how to pass parameters from `MainLayout.razor` to components

## Component Categories

### 1. Common/Shared Components (Used by Multiple Roles)

#### Pagination.razor
**Location**: `Shared/Pagination.razor`
**Parameters Required**:
- `CurrentPage` (int)
- `TotalPages` (int)
- `GoToPage` (EventCallback<int>)
- `GoToFirstPage` (EventCallback)
- `PreviousPage` (EventCallback)
- `NextPage` (EventCallback)
- `GoToLastPage` (EventCallback)

**Status**: ✅ Already wired in most places

#### NewsSection.razor
**Location**: `Shared/NewsSection.razor`
**Parameters Required**: (Check component for exact parameters)
**Status**: ⚠️ Needs verification

#### LoadingIndicator.razor
**Location**: `Shared/LoadingIndicator.razor`
**Parameters Required**: None (self-contained)
**Status**: ✅ Already wired

#### RegistrationPrompt.razor
**Location**: `Shared/RegistrationPrompt.razor`
**Parameters Required**: (Check component for exact parameters)
**Status**: ⚠️ Needs verification

---

### 2. Company Role Components ✅ EXTRACTED (2025-12-16)

#### CompanyAnnouncementsManagementSection.razor
**Location**: `Shared/Company/CompanyAnnouncementsManagementSection.razor` (638 lines)
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - Announcement creation form
- Likely needs: form state, announcement model, file upload handlers, save methods, validation properties

**Status**: ✅ Extracted - ❌ Not wired - needs parameter extraction

#### CompanyAnnouncementsSection.razor
**Location**: `Shared/Company/CompanyAnnouncementsSection.razor` (609 lines)
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - View uploaded announcements
- Likely needs: announcements list, pagination, visibility toggles, expand/collapse handlers

**Status**: ✅ Extracted - ❌ Not wired - needs parameter extraction

#### CompanyJobsSection.razor
**Location**: `Shared/Company/CompanyJobsSection.razor` (1,658 lines)
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - Job creation and management
- Likely needs: job model, form properties, save/update methods, pagination, etc.

**Status**: ✅ Extracted - ❌ Not wired - needs parameter extraction

#### CompanyInternshipsSection.razor
**Location**: `Shared/Company/CompanyInternshipsSection.razor` (1,978 lines)
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - Internship creation and management
- Likely needs: internship model, form visibility toggles, save/update methods, pagination, etc.

**Status**: ✅ Extracted - ❌ Not wired - needs parameter extraction

#### CompanyThesesSection.razor
**Location**: `Shared/Company/CompanyThesesSection.razor` (2,914 lines)
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - Thesis creation and management
- Likely needs: thesis model, form properties, save methods, professor search, etc.

**Status**: ✅ Extracted - ❌ Not wired - needs parameter extraction

#### CompanyEventsSection.razor
**Location**: `Shared/Company/CompanyEventsSection.razor` (2,602 lines)
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - Calendar and event management
- Likely needs: events list, calendar state, event creation/management methods

**Status**: ✅ Extracted - ❌ Not wired - needs parameter extraction

#### CompanyStudentSearchSection.razor
**Location**: `Shared/Company/CompanyStudentSearchSection.razor` (671 lines)
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - Student search functionality
- Likely needs: search properties, results list, pagination, modal state, etc.

**Status**: ✅ Extracted - ❌ Not wired - needs parameter extraction

#### CompanyProfessorSearchSection.razor
**Location**: `Shared/Company/CompanyProfessorSearchSection.razor` (528 lines)
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - Professor search functionality
- Likely needs: search properties, results list, pagination, modal state, etc.

**Status**: ✅ Extracted - ❌ Not wired - needs parameter extraction

#### CompanyResearchGroupSearchSection.razor
**Location**: `Shared/Company/CompanyResearchGroupSearchSection.razor` (692 lines)
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - Research group search functionality
- Likely needs: search properties, results list, pagination, modal state, etc.

**Status**: ✅ Extracted - ❌ Not wired - needs parameter extraction

---

### 3. Professor Role Components ✅ EXTRACTED (2025-12-16)

#### ProfessorAnnouncementsManagementSection.razor
**Location**: `Shared/Professor/ProfessorAnnouncementsManagementSection.razor` (854 lines)
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - Announcement creation and management
- Likely needs: form state, announcement model, file upload handlers, save methods, validation

**Status**: ✅ Extracted - ❌ Not wired - needs parameter extraction

#### ProfessorThesesSection.razor
**Location**: `Shared/Professor/ProfessorThesesSection.razor` (1,755 lines)
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - Thesis creation and management
- Likely needs: theses list, pagination, visibility toggles, management handlers

**Status**: ✅ Extracted - ❌ Not wired - needs parameter extraction

#### ProfessorInternshipsSection.razor
**Location**: `Shared/Professor/ProfessorInternshipsSection.razor` (1,841 lines)
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - Internships management
- Likely needs: internships list, form state, save/update methods, pagination, etc.

**Status**: ✅ Extracted - ❌ Not wired - needs parameter extraction

#### ProfessorEventsSection.razor
**Location**: `Shared/Professor/ProfessorEventsSection.razor` (2,868 lines)
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - Events calendar and management
- Likely needs: events list, calendar state, event creation/management methods, date handling, etc.

**Status**: ✅ Extracted - ❌ Not wired - needs parameter extraction

#### ProfessorStudentSearchSection.razor
**Location**: `Shared/Professor/ProfessorStudentSearchSection.razor` (623 lines)
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - Student search functionality
- Likely needs: search properties, results list, pagination, modal state, etc.

**Status**: ✅ Extracted - ❌ Not wired - needs parameter extraction

#### ProfessorCompanySearchSection.razor
**Location**: `Shared/Professor/ProfessorCompanySearchSection.razor` (385 lines)
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - Company search functionality
- Likely needs: search properties, results list, pagination, modal state, etc.

**Status**: ✅ Extracted - ❌ Not wired - needs parameter extraction

#### ProfessorResearchGroupSearchSection.razor
**Location**: `Shared/Professor/ProfessorResearchGroupSearchSection.razor` (2,356 lines)
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - Research group search functionality
- Likely needs: search properties, results list, pagination, modal state, etc.

**Status**: ✅ Extracted - ❌ Not wired - needs parameter extraction

---

### 4. Student Role Components

#### StudentThesisDisplaySection.razor
**Location**: `Shared/Student/StudentThesisDisplaySection.razor`
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - Thesis display and filtering
**Status**: ❌ Not wired - needs parameter extraction

#### StudentJobsDisplaySection.razor
**Location**: `Shared/Student/StudentJobsDisplaySection.razor`
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - Job display and filtering
**Status**: ❌ Not wired - needs parameter extraction

#### StudentInternshipsDisplaySection.razor
**Location**: `Shared/Student/StudentInternshipsDisplaySection.razor`
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - Internship display and filtering
**Status**: ❌ Not wired - needs parameter extraction

#### StudentAppliedInternshipsSection.razor
**Location**: `Shared/Student/StudentAppliedInternshipsSection.razor`
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - Applied internships display
**Status**: ❌ Not wired - needs parameter extraction

---

## Next Steps

### Phase 3.2: Create Component Parameter Contracts
1. For each component marked "NEEDS ANALYSIS":
   - Extract all `@bind`, `@onclick`, `@onchange` references
   - Identify all properties/methods being called
   - Document required parameters with types
   - Create parameter contract documentation

### Phase 3.3: Wire Common Components
1. Verify `Pagination` is correctly wired everywhere
2. Check `NewsSection` parameters
3. Verify `LoadingIndicator` and `RegistrationPrompt` usage

### Phase 3.4: Wire Role-Specific Components
1. Start with Company components (most complex)
2. Then Professor components
3. Finally Student components
4. For each component:
   - Add `[Parameter]` declarations
   - Update `MainLayout.razor` to pass parameters
   - Test component functionality

## Notes

- Many components were extracted with inline code that directly references `MainLayout.razor.cs` properties
- These references need to be converted to `[Parameter]` declarations
- Some components may need code-behind files (`.razor.cs`) for complex logic
- Event handlers may need to be wrapped or adapted for component use

