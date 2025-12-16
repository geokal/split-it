# Wiring Plan: CompanyAnnouncementsSection.razor

## Component Overview
- **File**: `Shared/Company/CompanyAnnouncementsSection.razor`
- **Lines**: 633
- **Purpose**: Display and manage uploaded company announcements
- **Status**: ❌ Not wired - needs parameter extraction

## Backup Status
✅ **Backup created**: `Company.razor.backup` (12,391 lines)

---

## Step 1: Dependency Analysis

### Properties Referenced (from component analysis)
1. **Visibility & Loading State**:
   - `isLoadingUploadedAnnouncements` (bool) - Line 11896 in MainLayout.razor.cs
   - `isUploadedAnnouncementsVisible` (bool) - Line 534 in MainLayout.razor.cs

2. **Filtering & Pagination**:
   - `selectedStatusFilterForAnnouncements` (string) - Line 417 in MainLayout.razor.cs
   - `pageSizeForAnnouncements` (int) - Needs verification
   - `pageSizeOptions_SeeMyUploadedAnnouncementsAsCompany` (List<int>) - Needs verification
   - `totalCountAnnouncements` (int) - Needs verification
   - `publishedCountAnnouncements` (int) - Needs verification
   - `unpublishedCountAnnouncements` (int) - Needs verification
   - `currentPageForUploadedAnnouncements` (int) - Needs verification
   - `totalPagesForUploadedAnnouncements` (int) - Needs verification

3. **Data**:
   - `FilteredAnnouncements` (IEnumerable<AnnouncementAsCompany>) - Computed property, needs verification
   - `uploadedAnnouncementsAsCompany` (List<AnnouncementAsCompany>) - Needs verification

4. **Bulk Operations**:
   - `isBulkEditModeForAnnouncements` (bool) - Needs verification
   - `selectedAnnouncementIds` (HashSet<int> or List<int>) - Needs verification
   - `bulkActionForAnnouncements` (string) - Needs verification
   - `newStatusForBulkActionForAnnouncements` (string) - Needs verification

5. **Modal State**:
   - `currentAnnouncement` (AnnouncementAsCompany) - Line 554 in MainLayout.razor.cs
   - `selectedCompanyAnnouncementToSeeDetailsAsCompany` (AnnouncementAsCompany) - Line 1134 in MainLayout.razor.cs

### Methods Referenced (from component analysis)
1. **Visibility Toggle**:
   - `ToggleUploadedAnnouncementsVisibility()` - Line 11896-11920 in MainLayout.razor.cs ✅ Found

2. **Filtering & Pagination**:
   - `HandleStatusFilterChange(ChangeEventArgs)` - Needs verification
   - `OnPageSizeChange_SeeMyUploadedAnnouncementsAsCompany(ChangeEventArgs)` - Needs verification
   - `GoToFirstPageForAnnouncements()` - Needs verification
   - `PreviousPageForAnnouncements()` - Needs verification
   - `GoToPageForAnnouncements(int)` - Needs verification
   - `NextPageForAnnouncements()` - Needs verification
   - `GoToLastPageForAnnouncements()` - Needs verification

3. **Bulk Operations**:
   - `EnableBulkEditModeForAnnouncements()` - Needs verification
   - `ExecuteBulkStatusChange(string)` - Needs verification
   - `ExecuteBulkCopy()` - Needs verification
   - `CancelBulkEditForAnnouncements()` - Needs verification
   - `ToggleAnnouncementSelection(int, ChangeEventArgs)` - Needs verification
   - `ExecuteBulkActionForAnnouncements()` - Needs verification
   - `CloseBulkActionModalForAnnouncements()` - Needs verification

4. **Individual Operations**:
   - `ToggleAnnouncementMenu(int)` - Needs verification
   - `DeleteAnnouncement(int)` - Line 12117 in MainLayout.razor.cs ✅ Found
   - `OpenCompanyAnnouncementDetailsModal(AnnouncementAsCompany)` - Line 1132 in MainLayout.razor.cs ✅ Found
   - `CloseCompanyAnnouncementDetailsModal()` - Line 1137 in MainLayout.razor.cs ✅ Found
   - `ChangeAnnouncementStatus(int, string)` - Line 12706 in MainLayout.razor.cs ✅ Found
   - `OpenEditModal(AnnouncementAsCompany)` - Line 12826 in MainLayout.razor.cs ✅ Found
   - `CloseEditModal()` - Needs verification
   - `UpdateAnnouncement(AnnouncementAsCompany)` - Needs verification

---

## Step 2: Parameter Contract

### Required Parameters

```csharp
@code {
    // Visibility & Loading
    [Parameter] public bool IsLoadingUploadedAnnouncements { get; set; }
    [Parameter] public bool IsUploadedAnnouncementsVisible { get; set; }
    [Parameter] public EventCallback ToggleUploadedAnnouncementsVisibility { get; set; }

    // Filtering
    [Parameter] public string SelectedStatusFilterForAnnouncements { get; set; }
    [Parameter] public EventCallback<ChangeEventArgs> HandleStatusFilterChange { get; set; }

    // Pagination
    [Parameter] public int PageSizeForAnnouncements { get; set; }
    [Parameter] public List<int> PageSizeOptions_SeeMyUploadedAnnouncementsAsCompany { get; set; }
    [Parameter] public EventCallback<ChangeEventArgs> OnPageSizeChange_SeeMyUploadedAnnouncementsAsCompany { get; set; }
    [Parameter] public int CurrentPageForUploadedAnnouncements { get; set; }
    [Parameter] public int TotalPagesForUploadedAnnouncements { get; set; }
    [Parameter] public EventCallback GoToFirstPageForAnnouncements { get; set; }
    [Parameter] public EventCallback PreviousPageForAnnouncements { get; set; }
    [Parameter] public EventCallback<int> GoToPageForAnnouncements { get; set; }
    [Parameter] public EventCallback NextPageForAnnouncements { get; set; }
    [Parameter] public EventCallback GoToLastPageForAnnouncements { get; set; }

    // Counts
    [Parameter] public int TotalCountAnnouncements { get; set; }
    [Parameter] public int PublishedCountAnnouncements { get; set; }
    [Parameter] public int UnpublishedCountAnnouncements { get; set; }

    // Data
    [Parameter] public IEnumerable<AnnouncementAsCompany> FilteredAnnouncements { get; set; }

    // Bulk Operations
    [Parameter] public bool IsBulkEditModeForAnnouncements { get; set; }
    [Parameter] public HashSet<int> SelectedAnnouncementIds { get; set; }
    [Parameter] public EventCallback EnableBulkEditModeForAnnouncements { get; set; }
    [Parameter] public EventCallback<string> ExecuteBulkStatusChange { get; set; }
    [Parameter] public EventCallback ExecuteBulkCopy { get; set; }
    [Parameter] public EventCallback CancelBulkEditForAnnouncements { get; set; }
    [Parameter] public EventCallback<int, ChangeEventArgs> ToggleAnnouncementSelection { get; set; }
    [Parameter] public string BulkActionForAnnouncements { get; set; }
    [Parameter] public string NewStatusForBulkActionForAnnouncements { get; set; }
    [Parameter] public EventCallback ExecuteBulkActionForAnnouncements { get; set; }
    [Parameter] public EventCallback CloseBulkActionModalForAnnouncements { get; set; }

    // Individual Operations
    [Parameter] public EventCallback<int> ToggleAnnouncementMenu { get; set; }
    [Parameter] public EventCallback<int> DeleteAnnouncement { get; set; }
    [Parameter] public EventCallback<AnnouncementAsCompany> OpenCompanyAnnouncementDetailsModal { get; set; }
    [Parameter] public EventCallback CloseCompanyAnnouncementDetailsModal { get; set; }
    [Parameter] public EventCallback<int, string> ChangeAnnouncementStatus { get; set; }
    [Parameter] public EventCallback<AnnouncementAsCompany> OpenEditModal { get; set; }
    [Parameter] public EventCallback CloseEditModal { get; set; }
    [Parameter] public AnnouncementAsCompany CurrentAnnouncement { get; set; }
    [Parameter] public EventCallback<AnnouncementAsCompany> UpdateAnnouncement { get; set; }
}
```

---

## Step 3: Implementation Steps

### 3.1 Add @code Section to Component
- [ ] Create `@code` block in `CompanyAnnouncementsSection.razor`
- [ ] Add all `[Parameter]` declarations
- [ ] Verify parameter types match MainLayout.razor.cs

### 3.2 Update Component Markup
- [ ] Replace direct property references with parameter names
- [ ] Replace direct method calls with EventCallback invocations
- [ ] Ensure all bindings use `@bind` or `@bind-Value` syntax

### 3.3 Update Company.razor
- [ ] Find where `CompanyAnnouncementsSection` is used
- [ ] Add parameter passing for all required parameters
- [ ] Map MainLayout.razor.cs properties/methods to component parameters

### 3.4 Verify MainLayout.razor.cs
- [ ] Confirm all referenced properties exist
- [ ] Confirm all referenced methods exist
- [ ] Verify method signatures match EventCallback types

### 3.5 Testing
- [ ] Test visibility toggle
- [ ] Test filtering
- [ ] Test pagination
- [ ] Test bulk operations
- [ ] Test individual operations (delete, edit, status change)
- [ ] Test modals

---

## Step 4: Verification Checklist

- [ ] All properties have corresponding `[Parameter]` declarations
- [ ] All methods have corresponding `EventCallback` parameters
- [ ] Component markup uses parameters instead of direct references
- [ ] Company.razor passes all required parameters
- [ ] No compilation errors
- [ ] All functionality works as before

---

## Next Steps

1. **Complete dependency verification**: Use Serena tools to find all remaining properties/methods
2. **Create parameter contract**: Finalize the `@code` section
3. **Update component**: Replace direct references with parameters
4. **Update Company.razor**: Add parameter passing
5. **Test**: Verify all functionality

---

## Notes

- This component is relatively straightforward (view/manage announcements)
- Most complexity is in bulk operations and modals
- Pagination is already handled by Shared.Pagination component (if used)
- Some properties may be computed (like `FilteredAnnouncements`) and need special handling

