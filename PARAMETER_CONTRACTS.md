# Component Parameter Contracts

This document defines the parameter contracts for all extracted components. Each component section lists:
- Required parameters with types
- Optional parameters
- Event callbacks
- Data models
- Notes on usage

## Contract Format

```csharp
[Parameter] public Type ParameterName { get; set; }
[Parameter] public EventCallback<ArgType> EventHandlerName { get; set; }
```

---

## Common Components

### Pagination.razor
**File**: `Shared/Pagination.razor`
**Status**: ✅ Complete

**Parameters**:
- `CurrentPage` (int) - Current page number
- `TotalPages` (int) - Total number of pages
- `GoToPage` (EventCallback<int>) - Navigate to specific page
- `GoToFirstPage` (EventCallback) - Navigate to first page
- `PreviousPage` (EventCallback) - Navigate to previous page
- `NextPage` (EventCallback) - Navigate to next page
- `GoToLastPage` (EventCallback) - Navigate to last page

---

## Company Role Components

### CompanyAnnouncementsSection.razor
**File**: `Shared/Company/CompanyAnnouncementsSection.razor`
**Status**: ✅ Already wired

**Parameters**: (See COMPONENT_DEPENDENCIES.md for full list)

---

### CompanyAnnouncementsManagementSection.razor
**File**: `Shared/Company/CompanyAnnouncementsManagementSection.razor`
**Status**: ❌ Needs wiring

**Required Parameters** (to be extracted from MainLayout.razor.cs):

**Form State**:
- `isAnnouncementsFormVisible` (bool) - Controls form visibility
- `ToggleFormVisibilityForUploadCompanyAnnouncements` (EventCallback) - Toggle form visibility
- `isUploadedAnnouncementsVisible` (bool) - Controls uploaded announcements visibility
- `ToggleUploadedAnnouncementsVisibility` (EventCallback) - Toggle uploaded announcements visibility
- `isLoadingUploadedAnnouncements` (bool) - Loading state

**Announcement Model**:
- `announcement` (CompanyAnnouncement) - Current announcement being created/edited
- `remainingCharactersInAnnouncementFieldUploadAsCompany` (int) - Character counter
- `remainingCharactersInAnnouncementDescriptionUploadAsCompany` (int) - Description character counter

**Validation**:
- `showErrorMessageforUploadingAnnouncementAsCompany` (bool) - Show validation errors
- `AnnouncementAttachmentErrorMessage` (string) - File upload error message
- `isFormValidToSaveAnnouncementAsCompany` (bool) - Form validation state
- `saveAnnouncementAsCompanyMessage` (string) - Save operation message
- `isSaveAnnouncementAsCompanySuccessful` (bool) - Save operation success state

**Loading/Progress**:
- `showLoadingModal` (bool) - Show loading modal
- `loadingProgress` (int) - Loading progress percentage

**Event Handlers**:
- `CheckCharacterLimitInAnnouncementFieldUploadAsCompany` (EventCallback) - Character limit check
- `CheckCharacterLimitInAnnouncementDescriptionUploadAsCompany` (EventCallback) - Description character limit check
- `HandleFileSelectedForAnnouncementAttachment` (EventCallback<InputFileChangeEventArgs>) - File selection handler
- `SaveAnnouncementAsUnpublished` (EventCallback) - Save as draft
- `SaveAnnouncementAsPublished` (EventCallback) - Publish announcement

**Uploaded Announcements Management**:
- `selectedStatusFilterForAnnouncements` (string) - Current filter selection
- `HandleStatusFilterChange` (EventCallback<ChangeEventArgs>) - Filter change handler
- `pageSizeForAnnouncements` (int) - Items per page
- `pageSizeOptions_SeeMyUploadedAnnouncementsAsCompany` (List<int>) - Page size options
- `OnPageSizeChange_SeeMyUploadedAnnouncementsAsCompany` (EventCallback<ChangeEventArgs>) - Page size change handler
- `totalCountAnnouncements` (int) - Total announcements count
- `publishedCountAnnouncements` (int) - Published announcements count
- `unpublishedCountAnnouncements` (int) - Unpublished announcements count
- `uploadedAnnouncementsAsCompany` (List<CompanyAnnouncement>) - List of uploaded announcements
- `currentPageForUploadedAnnouncements` (int) - Current page for uploaded announcements
- `totalPagesForUploadedAnnouncements` (int) - Total pages for uploaded announcements
- Pagination event handlers for uploaded announcements

**Bulk Operations** (if applicable):
- `isBulkEditModeForAnnouncements` (bool) - Bulk edit mode state
- `selectedAnnouncementIds` (List<int>) - Selected announcement IDs
- Bulk action event handlers

---

### CompanyInternshipsSection.razor
**File**: `Shared/Company/CompanyInternshipsSection.razor`
**Status**: ❌ Needs wiring

**Required Parameters** (to be extracted):

**Form State**:
- `isUploadCompanyInternshipsFormVisible` (bool)
- `ToggleFormVisibilityForUploadCompanyInternships` (EventCallback)
- `isFormVisibleToShowMyActiveInternshipsAsCompany` (bool)
- `ToggleFormVisibilityToShowMyActiveInternshipsAsCompany` (EventCallback)

**Internship Model**:
- `companyInternship` (CompanyInternship) - Current internship being created/edited

**Validation**:
- `showErrorMessage` (bool)
- `remainingCharactersInInternshipFieldUploadAsCompany` (int)

**Form Handlers**:
- `CheckCharacterLimitInInternshipFieldUploadAsCompany` (EventCallback)
- `UpdateTransportOffer` (EventCallback<bool>)
- `ToggleCheckboxesForCompanyInternship` (EventCallback)
- `OnAreaCheckedChangedForCompanyInternship` (EventCallback<ChangeEventArgs, Area>)
- `ToggleSubFieldsForCompanyInternship` (EventCallback<Area>)
- `OnSubFieldCheckedChangedForCompanyInternship` (EventCallback<ChangeEventArgs, Area, string>)
- `HandleSaveClickToSaveInternshipsAsCompany` (EventCallback)
- `HandlePublishClickToSaveInternshipsAsCompany` (EventCallback)

**Areas/Skills Data**:
- `availableAreasForCompanyInternship` (List<Area>)
- `showCheckboxesForCompanyInternship` (bool)
- `ExpandedAreasForCompanyInternship` (List<int>)
- `HasAnySelectionForCompanyInternship` (Func<bool>)
- `IsAreaSelectedForCompanyInternship` (Func<Area, bool>)
- `IsSubFieldSelectedForCompanyInternship` (Func<Area, string, bool>)

**Professor Selection**:
- `selectedProfessorId` (int?)
- `availableProfessors` (List<Professor>) - For EKPA supervisor selection

**Uploaded Internships Management**:
- `selectedStatusFilterForInternships` (string)
- `pageSizeForInternships` (int)
- `pageSizeOptions_SeeMyUploadedInternshipsAsCompany` (List<int>)
- `OnPageSizeChange_SeeMyUploadedInternshipsAsCompany` (EventCallback<ChangeEventArgs>)
- `uploadedInternshipsAsCompany` (List<CompanyInternship>)
- Pagination properties and handlers
- `EnableBulkEditModeForInternships` (EventCallback)
- `ExecuteBulkStatusChangeForInternships` (EventCallback<string>)

---

### CompanyEventsSection.razor
**File**: `Shared/Company/CompanyEventsSection.razor`
**Status**: ❌ Needs wiring

**Required Parameters** (to be extracted):
- Calendar state properties (currentMonth, daysInCurrentMonth, etc.)
- Events list
- Event creation/management handlers
- Calendar navigation handlers

---

### CompanyThesesSection.razor
**File**: `Shared/Company/CompanyThesesSection.razor`
**Status**: ❌ Needs wiring

**Required Parameters** (to be extracted):
- Thesis model
- Form state properties
- Validation properties
- Save/publish handlers
- Uploaded theses management properties

---

## Professor Role Components

### ProfessorAnnouncementsManagementSection.razor
**File**: `Shared/Professor/ProfessorAnnouncementsManagementSection.razor`
**Status**: ❌ Needs wiring

**Required Parameters** (to be extracted):
- Similar to CompanyAnnouncementsManagementSection but for Professor role
- Professor-specific announcement properties and handlers

---

### ProfessorThesesSection.razor
**File**: `Shared/Professor/ProfessorThesesSection.razor`
**Status**: ❌ Needs wiring

**Required Parameters** (to be extracted):
- Thesis model for professor
- Form state and validation
- Areas/skills selection
- Save/publish handlers
- Uploaded theses management

---

## Student Role Components

### StudentThesisDisplaySection.razor
**File**: `Shared/Student/StudentThesisDisplaySection.razor`
**Status**: ❌ Needs wiring

**Required Parameters** (to be extracted):
- `showThesisApplications` (bool)
- `OnFilterChange` (EventCallback<ChangeEventArgs>)
- `OnPageSizeChange_SearchForThesisAsStudent` (EventCallback<ChangeEventArgs>)
- `thesisList` (List<Thesis>) - Filtered thesis list
- `currentThesisPage` (int)
- `totalThesisPages_SearchThesisAsStudent` (int)
- `ChangeThesisPage` (EventCallback<int>)
- `ShowProfessorHyperlinkNameDetailsModalInStudentThesis` (EventCallback<string>)
- `ShowCompanyHyperlinkNameDetailsModalInStudentThesis` (EventCallback<string>)
- `ShowCompanyThesisDetailsAsStudent` (EventCallback<string>)
- `ShowProfessorThesisDetailsAsStudent` (EventCallback<string>)
- `ApplyForThesisAsStudent` (EventCallback<Thesis>)
- Modal state properties and close handlers

---

### StudentJobsDisplaySection.razor
**File**: `Shared/Student/StudentJobsDisplaySection.razor`
**Status**: ❌ Needs wiring

**Required Parameters** (to be extracted):
- Job display and filtering properties
- Pagination properties
- Application handlers
- Modal handlers

---

### StudentInternshipsDisplaySection.razor
**File**: `Shared/Student/StudentInternshipsDisplaySection.razor`
**Status**: ❌ Needs wiring

**Required Parameters** (to be extracted):
- Internship display and filtering properties
- Pagination properties
- Application handlers
- Modal handlers

---

### StudentAppliedInternshipsSection.razor
**File**: `Shared/Student/StudentAppliedInternshipsSection.razor`
**Status**: ❌ Needs wiring

**Required Parameters** (to be extracted):
- Applied internships list
- Toggle visibility handlers
- Pagination properties
- Withdrawal handlers

---

## Implementation Strategy

1. **For each component**:
   - Extract all property/method references from the component file
   - Map to MainLayout.razor.cs properties/methods
   - Create `[Parameter]` declarations
   - Update component to use parameters instead of direct references

2. **For MainLayout.razor**:
   - Pass parameters to each component instance
   - Ensure all required parameters are provided

3. **Testing**:
   - Verify each component receives correct data
   - Test event handlers
   - Verify form submissions work
   - Test pagination and filtering

## Notes

- Some components may need code-behind files (`.razor.cs`) for complex logic
- Event handlers may need to be wrapped to match EventCallback signatures
- Some properties may need to be converted to parameters with proper types
- Consider creating shared models/interfaces for common patterns

