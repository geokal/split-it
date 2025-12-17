# Wiring Progress

> **⚠️ NOTE: This document tracks Pattern 1 (dumb components with parameters) progress, which has been superseded by Pattern 2 (smart components with code-behind).**  
> The project has migrated to Pattern 2. See Serena memory `pattern2_component_architecture` for current approach.

## Current Status: Pattern 2 Migration

**Completed:**
- ✅ All Professor components (7/7) converted to Pattern 2
- ✅ All Student components (6/6) converted to Pattern 2

**Pending:**
- ⏳ Company components (0/9) - Pattern 2 conversion
- ⏳ ResearchGroup components (0/5) - Pattern 2 conversion
- ⏳ Admin components (0/1) - Pattern 2 conversion

---

# Pattern 1 Wiring Progress (Deprecated) Documentation

**Last Updated**: 2025-12-16
**Status**: In Progress - CompanyAnnouncementsSection wired, ready to integrate into Company.razor

---

## Established Pattern (From CompanyAnnouncementsSection.razor)

### Step 1: Dependency Analysis
1. Use Serena's `find_symbol` tool to find exact property/method names in `MainLayout.razor.cs`
2. Use `grep` to find all references in the component markup
3. Document all dependencies

### Step 2: Create @code Section
- Add `@code` block at end of component (before any leftover markup)
- Use **EXACT** names from MainLayout.razor.cs (camelCase, not PascalCase)
- All properties → `[Parameter]` declarations
- All methods → `EventCallback` or `EventCallback<T>` parameters
- Computed properties → Pass as `Func<T>` if needed

### Step 3: Update Component Markup
- Replace direct property references: `isLoadingUploadedAnnouncements` (not `IsLoadingUploadedAnnouncements`)
- Replace method calls: `ToggleUploadedAnnouncementsVisibility` (not `.InvokeAsync()` - Blazor handles this)
- Keep all bindings: `@bind="propertyName"` stays the same
- Keep all event handlers: `@onclick="MethodName"` stays the same

### Step 4: Remove Leftover Markup
- Component should end with `</div>` then `@code { ... }`
- Remove any leftover sections from parent file

### Step 5: Verify Parameter Names
- Use Serena's `find_symbol` to verify exact names
- All parameters must match MainLayout.razor.cs exactly

---

## CompanyAnnouncementsSection.razor - COMPLETED ✅

### Parameters Added (47 total):
- Visibility: `isLoadingUploadedAnnouncements`, `isUploadedAnnouncementsVisible`, `ToggleUploadedAnnouncementsVisibility`
- Filtering: `selectedStatusFilterForAnnouncements`, `HandleStatusFilterChange`
- Pagination: `pageSizeForAnnouncements`, `pageSizeOptions_SeeMyUploadedAnnouncementsAsCompany`, `OnPageSizeChange_SeeMyUploadedAnnouncementsAsCompany`, `currentPageForAnnouncements`, `totalPagesForAnnouncements`, pagination callbacks
- Counts: `totalCountAnnouncements`, `publishedCountAnnouncements`, `unpublishedCountAnnouncements`
- Data: `FilteredAnnouncements`, `GetPaginatedAnnouncements`
- Bulk Operations: `isBulkEditModeForAnnouncements`, `selectedAnnouncementIds`, `bulkActionForAnnouncements`, `newStatusForBulkActionForAnnouncements`, and all bulk operation callbacks
- Individual Operations: `activeAnnouncementMenuId`, `ToggleAnnouncementMenu`, `DeleteAnnouncement`, `OpenCompanyAnnouncementDetailsModal`, `CloseCompanyAnnouncementDetailsModal`, `ChangeAnnouncementStatus`, `OpenEditModal`, `CloseEditModal`, `currentAnnouncement`, `UpdateAnnouncement`, `selectedCompanyAnnouncementToSeeDetailsAsCompany`
- Modals: `isEditModalVisible`, `HandleFileUploadToEditCompanyAnnouncementAttachment`

### Status:
- ✅ @code section added with all parameters
- ✅ Markup updated to use parameters
- ✅ Leftover markup removed
- ✅ All parameter names match MainLayout.razor.cs exactly
- ✅ **COMPLETED**: Company.razor updated to use component (lines 713-761)
- ✅ Old inline markup removed (was causing RZ9981 error)

---

## Next Steps

### Immediate: Wire CompanyAnnouncementsSection into Company.razor
1. Find where announcements section is in Company.razor (around line 75-609)
2. Replace inline markup with: `<CompanyAnnouncementsSection />`
3. Pass all 47 parameters from MainLayout.razor.cs properties/methods

### Then: Apply Pattern to Remaining Components
- CompanyJobsSection.razor
- CompanyInternshipsSection.razor
- CompanyThesesSection.razor
- CompanyEventsSection.razor
- CompanyStudentSearchSection.razor
- CompanyProfessorSearchSection.razor
- CompanyResearchGroupSearchSection.razor
- CompanyAnnouncementsManagementSection.razor

---

## Key Files
- `MainLayout.razor.cs` - Source of all properties/methods (33,000+ lines)
- `Company.razor` - Container that needs to pass parameters (12,391 lines)
- `Shared/Company/CompanyAnnouncementsSection.razor` - First wired component (670 lines)
- `WIRING_PLAN_CompanyAnnouncementsSection.md` - Detailed wiring plan (in same directory)
- `WIRING_APPROACH_COMPARISON.md` - Approach comparison
- `WIRING_PATTERN_EXPLANATION.md` - Pattern explanation

---

## Important Notes
- **Never change symbol names** - Use exact names from MainLayout.razor.cs
- **Use Serena's find_symbol** to verify exact property/method names
- **Component parameters are camelCase** (matching MainLayout.razor.cs)
- **Blazor handles EventCallback invocation** - Don't use `.InvokeAsync()` in markup
- **Remove leftover markup** - Components should end cleanly

---

## If Starting New Conversation
1. Read this file (`docs/wiring/WIRING_PROGRESS.md`)
2. Read `docs/wiring/WIRING_PLAN_CompanyAnnouncementsSection.md` for detailed steps
3. Check CompanyAnnouncementsSection.razor as reference
4. Continue with next component or Company.razor integration

