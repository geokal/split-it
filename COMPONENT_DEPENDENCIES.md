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

### 2. Company Role Components

#### CompanyAnnouncementsSection.razor
**Location**: `Shared/Company/CompanyAnnouncementsSection.razor`
**Parameters Required**:
- `Announcements` (List<CompanyAnnouncement>)
- `pageSize` (int)
- `currentPageForCompanyAnnouncements` (int)
- `totalPagesForCompanyAnnouncements` (int)
- `GoToFirstPageForCompanyAnnouncements` (EventCallback<int>)
- `PreviousPageForCompanyAnnouncements` (EventCallback<int>)
- `NextPageForCompanyAnnouncements` (EventCallback<int>)
- `GoToLastPageForCompanyAnnouncements` (EventCallback<int>)
- `GoToPageForCompanyAnnouncements` (EventCallback<int>)
- `isCompanyAnnouncementsVisible` (bool)
- `ToggleCompanyAnnouncementsVisibility` (EventCallback)
- `expandedAnnouncementId` (int)
- `ToggleDescription` (EventCallback<int>)
- `DownloadAnnouncementAttachmentFrontPage` (EventCallback<byte[], string>)

**Status**: ✅ Already wired in Company.razor

#### CompanyAnnouncementsManagementSection.razor
**Location**: `Shared/Company/CompanyAnnouncementsManagementSection.razor`
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - This component was extracted with inline code that references many properties/methods
- Likely needs: announcement creation form properties, file upload handlers, save methods, etc.

**Status**: ❌ Not wired - needs parameter extraction

#### CompanyInternshipsSection.razor
**Location**: `Shared/Company/CompanyInternshipsSection.razor`
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - Large component with forms and management sections
- Likely needs: internship model, form visibility toggles, save/update methods, pagination, etc.

**Status**: ❌ Not wired - needs parameter extraction

#### CompanyEventsSection.razor
**Location**: `Shared/Company/CompanyEventsSection.razor`
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - Calendar and event management
- Likely needs: events list, calendar state, event creation/management methods

**Status**: ❌ Not wired - needs parameter extraction

#### CompanyThesesSection.razor
**Location**: `Shared/Company/CompanyThesesSection.razor`
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - Thesis creation and management
- Likely needs: thesis model, form properties, save methods, etc.

**Status**: ❌ Not wired - needs parameter extraction

#### CompanyJobsSection.razor
**Location**: `Shared/Company/CompanyJobsSection.razor`
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS**
**Status**: ❌ Not wired - needs parameter extraction

#### ResearchGroupSearchSection.razor
**Location**: `Shared/Company/ResearchGroupSearchSection.razor`
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - Search functionality
- Likely needs: search properties, results list, pagination, etc.

**Status**: ❌ Not wired - needs parameter extraction

---

### 3. Professor Role Components

#### ProfessorAnnouncementsManagementSection.razor
**Location**: `Shared/Professor/ProfessorAnnouncementsManagementSection.razor`
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - Announcement creation and management
**Status**: ❌ Not wired - needs parameter extraction

#### ProfessorThesesSection.razor
**Location**: `Shared/Professor/ProfessorThesesSection.razor`
**Parameters Required**: 
- ⚠️ **NEEDS ANALYSIS** - Thesis creation and management
**Status**: ❌ Not wired - needs parameter extraction

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

