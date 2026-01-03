# Phase 1 Implementation Guide: Wrapper Extraction + Service Expansion

**Date**: December 23, 2025  
**Status**: Implementation Ready  
**Duration**: 2-3 weeks  
**Complexity**: Low  
**ROI**: High (7,345 lines eliminated, 28% reduction)

---

## Table of Contents

1. [Phase 1 Overview](#phase-1-overview)
2. [Implementation Strategy](#implementation-strategy)
3. [Wrapper Extraction Details](#wrapper-extraction-details)
4. [Service Expansion Details](#service-expansion-details)
5. [Step-by-Step Roadmap](#step-by-step-roadmap)
6. [Risk Mitigation](#risk-mitigation)
7. [Testing Strategy](#testing-strategy)
8. [Success Metrics](#success-metrics)
9. [Timeline Estimates](#timeline-estimates)

---

## Phase 1 Overview

### Goals
- Extract 5 shared UI wrapper components (eliminate 6,150 lines of duplication)
- Expand 4 role services with 33 new methods (eliminate 1,195 lines of DB access from components)
- Remove 12 `DbContextFactory` injections from components
- Achieve **28% codebase reduction** (7,345 lines)
- Improve testability and maintainability

### Deliverables
- 5 new Razor wrapper components in `Components/Shared/`
- Updated interfaces for 4 role services
- Updated implementations for 4 role services
- 28 refactored component files
- Test coverage for new wrappers and service methods

### Impact by Role
| Role | Components Affected | Lines Eliminated | DbContext Removed |
|------|-------------------|------------------|------------------|
| **Student** | 5 | ~240 | 1 |
| **Company** | 10 | ~900 | 3 |
| **Professor** | 8 | ~560 | 3 |
| **ResearchGroup** | 5 | ~4,645 | 5 |
| **TOTAL** | **28** | **~7,345** | **12** |

---

## Implementation Strategy

### Phase Sequencing (Lowest Risk → Highest ROI)

The implementation follows this order to minimize risk and validate the pattern before scaling:

#### Week 1: High-Impact, Low-Risk Targets
1. **ResearchGroupAnnouncementsSection Service Expansion** (Highest ROI, lowest risk)
2. **CRUDListManager Wrapper Extraction** (Affects 10 components)

#### Week 2: Medium-Impact Targets
3. **MultiFieldSearchForm Wrapper Extraction** (Affects 9 components)
4. **CalendarEventViewer Wrapper Extraction** (Affects 4 components)

#### Week 3: Wrap-Up
5. **InterestSubmissionButton & BulkActionConfirmationModal** (Affects 14 components total)
6. **Remaining component refactoring and testing**

### Extraction Pattern

Each wrapper follows this pattern:

```csharp
// 1. Define the contract (parameters, events, slots)
<WrapperComponent
  @* Data inputs *@
  Items="IEnumerable<TEntity>"
  
  @* Configuration *@
  PageSize="int"
  AllowBulkEdit="bool"
  
  @* Slots (rendering hooks) *@
  @ColumnTemplate="RenderFragment<TEntity>"
  @ToolbarExtension="RenderFragment"
  
  @* Events (callback hooks) *@
  OnStateChanged="EventCallback<ComponentState>"
  OnAction="EventCallback<(string action, object data)>" />

// 2. Wrapper owns: Pagination, bulk selection, filter state
// 3. Parent owns: Data fetching, action execution, business logic
// 4. Events flow from wrapper → parent (never the reverse)
```

---

## Wrapper Extraction Details

### Wrapper 1: CRUDListManager

**Priority**: HIGH (affects 10 components, ~4,000 lines)  
**Complexity**: MEDIUM (state management for pagination + bulk operations)  
**Target Start**: Week 1  
**Components Affected**: CompanyJobsSection, CompanyInternshipsSection, CompanyThesesSection, CompanyAnnouncementsSection, ProfessorJobsSection, ProfessorInternshipsSection, ProfessorThesesSection, ProfessorAnnouncementsSection, ResearchGroupAnnouncementsSection, CompanyEventsSection

#### Specification

```csharp
// Components/Shared/CRUDListManager.razor

@using System.Collections.Generic
@using System.Linq
@using System.Threading.Tasks
@typeparam TEntity where TEntity : class

<div class="crud-list-container">
  <!-- Status Filter -->
  <div class="filter-section">
    <select @onchange="HandleStatusFilterChanged" class="status-filter">
      @foreach (var option in StatusOptions)
      {
        <option value="@option" 
                selected="@(option == FilterStatus)">
          @option
        </option>
      }
    </select>
  </div>

  <!-- Item Count Display -->
  <div class="item-counts">
    <span>Σύνολο: @TotalCount</span>
    <span>Δημοσιευμένα: @PublishedCount</span>
    <span>Μη Δημοσιευμένα: @UnpublishedCount</span>
  </div>

  <!-- Bulk Edit Mode Toggle -->
  <button @onclick="ToggleBulkEditMode" class="bulk-toggle">
    @(IsBulkEditMode ? "Ακύρωση" : "Επεξεργασία")
  </button>

  <!-- Bulk Actions (only in edit mode) -->
  @if (IsBulkEditMode)
  {
    <div class="bulk-actions">
      <select @bind="SelectedBulkAction" class="bulk-action-dropdown">
        <option value="">-- Επιλέξτε Ενέργεια --</option>
        <option value="publish">Δημοσίευση</option>
        <option value="unpublish">Αποδημοσίευση</option>
        <option value="delete">Διαγραφή</option>
      </select>
      <button @onclick="ExecuteBulkAction" 
              disabled="@(SelectedItemIds.Count == 0 || string.IsNullOrEmpty(SelectedBulkAction))">
        Εφαρμογή (@SelectedItemIds.Count)
      </button>
    </div>
  }

  <!-- Item List -->
  <div class="item-list">
    @if (IsBulkEditMode)
    {
      <div class="checkbox-column">
        <input type="checkbox" @onchange="SelectAllCheckboxChanged" />
      </div>
    }

    <!-- Column Headers -->
    <div class="list-header">
      @ColumnTemplate
    </div>

    <!-- Data Rows -->
    @foreach (var item in FilteredItems)
    {
      <div class="list-row" data-id="@GetItemId(item)">
        @if (IsBulkEditMode)
        {
          <input type="checkbox" 
                 @onchange="e => HandleItemCheckboxChanged(item, (bool)e.Value)"
                 checked="@IsItemSelected(item)" />
        }
        @ColumnTemplate(item)
      </div>
    }
  </div>

  <!-- Pagination -->
  <div class="pagination">
    <button @onclick="() => GoToPage(1)" disabled="@(CurrentPage == 1)"><<</button>
    <button @onclick="() => GoToPage(CurrentPage - 1)" disabled="@(CurrentPage == 1)"><</button>
    
    <span>Σελίδα @CurrentPage / @TotalPages</span>
    
    <button @onclick="() => GoToPage(CurrentPage + 1)" disabled="@(CurrentPage == TotalPages)">></button>
    <button @onclick="() => GoToPage(TotalPages)" disabled="@(CurrentPage == TotalPages)">>></button>

    <select @onchange="HandlePageSizeChanged" class="page-size-selector">
      @foreach (var size in PageSizeOptions)
      {
        <option value="@size" selected="@(size == PageSize)">@size per page</option>
      }
    </select>
  </div>
</div>

@code {
  [Parameter] public IEnumerable<TEntity> Items { get; set; } = Enumerable.Empty<TEntity>();
  [Parameter] public string FilterStatus { get; set; } = "Όλα";
  [Parameter] public List<string> StatusOptions { get; set; } = new();
  [Parameter] public int PageSize { get; set; } = 10;
  [Parameter] public int[] PageSizeOptions { get; set; } = new[] { 10, 50, 100 };
  [Parameter] public int CurrentPage { get; set; } = 1;
  [Parameter] public int TotalCount { get; set; }
  [Parameter] public int PublishedCount { get; set; }
  [Parameter] public int UnpublishedCount { get; set; }

  [Parameter] public RenderFragment<TEntity> ColumnTemplate { get; set; }
  [Parameter] public EventCallback<string> OnStatusFilterChanged { get; set; }
  [Parameter] public EventCallback<int> OnPageChanged { get; set; }
  [Parameter] public EventCallback<int> OnPageSizeChanged { get; set; }
  [Parameter] public EventCallback<(string action, HashSet<int> ids)> OnBulkActionSelected { get; set; }

  private bool IsBulkEditMode = false;
  private HashSet<int> SelectedItemIds = new();
  private string SelectedBulkAction = "";
  private IEnumerable<TEntity> FilteredItems => Items.Skip((CurrentPage - 1) * PageSize).Take(PageSize);
  private int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

  private async Task HandleStatusFilterChanged(ChangeEventArgs e)
  {
    FilterStatus = e.Value?.ToString() ?? "Όλα";
    CurrentPage = 1;
    await OnStatusFilterChanged.InvokeAsync(FilterStatus);
  }

  private async Task HandlePageSizeChanged(ChangeEventArgs e)
  {
    if (int.TryParse(e.Value?.ToString(), out int newSize))
    {
      PageSize = newSize;
      CurrentPage = 1;
      await OnPageSizeChanged.InvokeAsync(newSize);
    }
  }

  private async Task GoToPage(int page)
  {
    CurrentPage = page;
    await OnPageChanged.InvokeAsync(page);
  }

  private void ToggleBulkEditMode()
  {
    IsBulkEditMode = !IsBulkEditMode;
    if (!IsBulkEditMode)
    {
      SelectedItemIds.Clear();
    }
  }

  private async Task ExecuteBulkAction()
  {
    if (!string.IsNullOrEmpty(SelectedBulkAction) && SelectedItemIds.Count > 0)
    {
      await OnBulkActionSelected.InvokeAsync((SelectedBulkAction, SelectedItemIds));
    }
  }

  private void HandleItemCheckboxChanged(TEntity item, bool isChecked)
  {
    var itemId = GetItemId(item);
    if (isChecked)
    {
      SelectedItemIds.Add(itemId);
    }
    else
    {
      SelectedItemIds.Remove(itemId);
    }
  }

  private void SelectAllCheckboxChanged(ChangeEventArgs e)
  {
    if ((bool)e.Value)
    {
      SelectedItemIds = new HashSet<int>(FilteredItems.Select(GetItemId));
    }
    else
    {
      SelectedItemIds.Clear();
    }
  }

  private bool IsItemSelected(TEntity item) => SelectedItemIds.Contains(GetItemId(item));

  private int GetItemId(TEntity item)
  {
    var idProp = item.GetType().GetProperty("Id");
    return idProp != null ? (int)idProp.GetValue(item) : 0;
  }
}
```

#### Usage Example

**Before (CompanyJobsSection)**:
```csharp
// ~300 lines of pagination + bulk edit logic IN COMPONENT
private async Task FilterJobsBy(string status)
{
  selectedStatusFilterForJobs = status;
  currentPageForJobs = 1;
  FilteredJobs = UploadedJobs
    .Where(j => status == "Όλα" || j.CompanyJobStatus == status)
    .ToList();
  await InvokeAsync(StateHasChanged);
}

private void HandleJobCheckboxChanged(int jobId, bool isChecked)
{
  if (isChecked)
    selectedJobIds.Add(jobId);
  else
    selectedJobIds.Remove(jobId);
}

// ... 20+ more pagination/bulk methods
```

**After (CompanyJobsSection)**:
```csharp
<CRUDListManager
  Items="FilteredJobs"
  FilterStatus="selectedStatusFilterForJobs"
  StatusOptions="new() { \"Όλα\", \"Δημοσιευμένη\", \"Μη Δημοσιευμένη\" }"
  PageSize="JobsPerPage"
  CurrentPage="currentPageForJobs"
  TotalCount="totalCountJobs"
  PublishedCount="publishedCountJobs"
  UnpublishedCount="unpublishedCountJobs"
  
  OnStatusFilterChanged="HandleStatusFilterChanged"
  OnPageChanged="HandlePageChanged"
  OnPageSizeChanged="HandlePageSizeChanged"
  OnBulkActionSelected="HandleBulkActionSelected">
  
  <ColumnTemplate>
    <td>@item.CompanyJobTitle</td>
    <td>@item.CompanyJobSalary€</td>
    <td>
      <button @onclick="() => EditJob(item.Id)">Επεξεργασία</button>
    </td>
  </ColumnTemplate>
</CRUDListManager>

@code {
  private async Task HandleBulkActionSelected((string action, HashSet<int> ids) data)
  {
    showBulkActionModal = true;
    bulkAction = data.action;
    selectedJobIds = data.ids;
  }
}
```

#### Component Refactoring Checklist

For each of 10 affected components:
- [ ] Replace 200+ lines of pagination code with `<CRUDListManager>`
- [ ] Remove `currentPage*`, `pageSize*`, `FilteredJobs` state variables
- [ ] Remove all pagination methods (`GoToPage`, `FilterBy`, `SelectAll`, etc.)
- [ ] Keep entity-specific bulk action execution in parent
- [ ] Verify pagination works correctly
- [ ] Verify bulk selection works correctly
- [ ] Update related tests

---

### Wrapper 2: MultiFieldSearchForm

**Priority**: HIGH (affects 9 components, ~1,800 lines)  
**Complexity**: MEDIUM (state management for form fields, suggestions, pagination)  
**Target Start**: Week 2  
**Components Affected**: StudentCompanySearchSection, CompanyStudentSearchSection, CompanyProfessorSearchSection, CompanyResearchGroupSearchSection, ProfessorStudentSearchSection, ProfessorCompanySearchSection, ProfessorResearchGroupSearchSection, ResearchGroupCompanySearchSection, ResearchGroupProfessorSearchSection

#### Specification

```csharp
// Components/Shared/MultiFieldSearchForm.razor

@using System.Collections.Generic
@using System.Linq
@using System.Threading.Tasks
@typeparam TResult where TResult : class

<div class="multi-field-search-form">
  <!-- Search Form Section -->
  <div class="search-form">
    @SearchFieldsTemplate
    
    <button @onclick="ExecuteSearch" disabled="@IsSearching" class="search-button">
      @(IsSearching ? "Αναζήτηση..." : "Αναζήτηση")
    </button>
    <button @onclick="ClearSearch" class="clear-button">Καθαρισμός</button>
  </div>

  <!-- Search Results -->
  @if (HasSearched)
  {
    <div class="search-results">
      <div class="result-count">
        @Results.Count αποτελέσματα
      </div>

      <!-- Results Table -->
      @if (Results.Count > 0)
      {
        <div class="results-list">
          @foreach (var result in PaginatedResults)
          {
            @ResultRowTemplate(result)
          }
        </div>

        <!-- Results Pagination -->
        @if (TotalPages > 1)
        {
          <div class="pagination">
            <button @onclick="() => GoToResultPage(1)" disabled="@(CurrentResultPage == 1)"><<</button>
            <button @onclick="() => GoToResultPage(CurrentResultPage - 1)" disabled="@(CurrentResultPage == 1)"><</button>
            
            <span>Σελίδα @CurrentResultPage / @TotalPages</span>
            
            <button @onclick="() => GoToResultPage(CurrentResultPage + 1)" disabled="@(CurrentResultPage == TotalPages)">></button>
            <button @onclick="() => GoToResultPage(TotalPages)" disabled="@(CurrentResultPage == TotalPages)">>></button>
          </div>
        }
      }
      else
      {
        <div class="no-results">Δεν βρέθηκαν αποτελέσματα</div>
      }
    </div>
  }
</div>

@code {
  [Parameter] public RenderFragment SearchFieldsTemplate { get; set; }
  [Parameter] public RenderFragment<TResult> ResultRowTemplate { get; set; }
  [Parameter] public List<TResult> Results { get; set; } = new();
  [Parameter] public int ResultPageSize { get; set; } = 20;
  [Parameter] public EventCallback<SearchCriteria> OnSearchSubmitted { get; set; }
  [Parameter] public EventCallback OnSearchCleared { get; set; }

  private bool IsSearching = false;
  private bool HasSearched = false;
  private int CurrentResultPage = 1;
  private IEnumerable<TResult> PaginatedResults => Results.Skip((CurrentResultPage - 1) * ResultPageSize).Take(ResultPageSize);
  private int TotalPages => (int)Math.Ceiling((double)Results.Count / ResultPageSize);

  private async Task ExecuteSearch()
  {
    IsSearching = true;
    CurrentResultPage = 1;
    HasSearched = true;
    // Parent component handles actual search logic via OnSearchSubmitted
    await OnSearchSubmitted.InvokeAsync(null); // Parent extracts form values
    IsSearching = false;
  }

  private async Task ClearSearch()
  {
    HasSearched = false;
    Results.Clear();
    CurrentResultPage = 1;
    await OnSearchCleared.InvokeAsync();
  }

  private async Task GoToResultPage(int page)
  {
    CurrentResultPage = page;
    await InvokeAsync(StateHasChanged);
  }
}

public class SearchCriteria
{
  public string SearchText { get; set; }
  public Dictionary<string, object> Filters { get; set; } = new();
}
```

#### Component Refactoring Checklist

For each of 9 affected components:
- [ ] Replace 150-200 lines of search form code with `<MultiFieldSearchForm>`
- [ ] Move search execution logic to parent (keep `OnSearchSubmitted` handler)
- [ ] Remove form state variables (`searchText`, `filters`, `selectedTags`, etc.)
- [ ] Verify search form rendering
- [ ] Verify result pagination works
- [ ] Verify autocomplete/suggestions work
- [ ] Update related tests

---

### Wrapper 3: CalendarEventViewer

**Priority**: MEDIUM (affects 4 components, ~1,000 lines)  
**Complexity**: HIGH (month calculation, day highlighting, modal management)  
**Target Start**: Week 2  
**Components Affected**: StudentEventsSection, CompanyEventsSection, ProfessorEventsSection, ResearchGroupEventsSection

#### Specification

Calendar grid (7 columns × 6 rows), month navigation, event aggregation, modal display.

**Key Responsibilities**:
- Wrapper owns: Month state, calendar grid layout, day highlighting, modal visibility
- Parent owns: Event data, event filtering, event interest submission

#### Component Refactoring Checklist

For each of 4 affected components:
- [ ] Replace 250+ lines of calendar rendering with `<CalendarEventViewer>`
- [ ] Remove calendar state variables (`currentMonth`, `selectedDay`, `eventsByDate`, etc.)
- [ ] Remove all calendar calculation methods
- [ ] Keep event interest submission in parent
- [ ] Keep bulk event status operations in parent
- [ ] Verify calendar navigation works
- [ ] Verify event highlighting works
- [ ] Verify modal opens/closes correctly
- [ ] Update related tests

---

### Wrapper 4: InterestSubmissionButton

**Priority**: MEDIUM (affects 5 components, ~750 lines)  
**Complexity**: MEDIUM (metadata form state, async submission)  
**Target Start**: Week 3  
**Components Affected**: StudentEventsSection, StudentJobsDisplaySection, StudentInternshipsSection, StudentThesisDisplaySection, CompanyEventsSection

#### Component Refactoring Checklist

For each of 5 affected components:
- [ ] Replace 150+ lines of interest button code with `<InterestSubmissionButton>`
- [ ] Remove button state variables (`isSubmittingInterest`, `showMetadataForm`, etc.)
- [ ] Keep metadata form customization via RenderFragment slot
- [ ] Keep service call in parent `OnInterestSubmitted` handler
- [ ] Verify button loading state works
- [ ] Verify metadata form displays correctly
- [ ] Verify submission success/error handling
- [ ] Update related tests

---

### Wrapper 5: BulkActionConfirmationModal

**Priority**: LOW (affects 9 components, ~1,800 lines)  
**Complexity**: MEDIUM (progress tracking, error display)  
**Target Start**: Week 3  
**Components Affected**: CompanyEventsSection, CompanyJobsSection, CompanyInternshipsSection, CompanyThesesSection, CompanyAnnouncementsSection, ProfessorEventsSection, ProfessorJobsSection, ProfessorInternshipsSection, ProfessorThesesSection, ResearchGroupAnnouncementsSection

#### Component Refactoring Checklist

For each of 9 affected components:
- [ ] Replace 150-200 lines of modal code with `<BulkActionConfirmationModal>`
- [ ] Remove modal state variables (`showBulkActionModal`, `loadingProgress`, etc.)
- [ ] Keep action execution logic in parent `OnConfirm` handler
- [ ] Verify modal displays correct action
- [ ] Verify progress bar works for long operations
- [ ] Verify error messages display
- [ ] Verify success/cancel callbacks
- [ ] Update related tests

---

## Service Expansion Details

### Service 1: ResearchGroupDashboardService (CRITICAL)

**Priority**: CRITICAL (27% reduction in ResearchGroupAnnouncementsSection)  
**Current Size**: 3.8 KB  
**Expanded Size**: ~15 KB  
**New Methods**: 16  
**Target Start**: Week 1 (Day 1)

#### New Methods Required

```csharp
// Announcements CRUD
Task<AnnouncementAsResearchGroup> CreateAnnouncementAsync(
  AnnouncementAsResearchGroup announcement, 
  CancellationToken cancellationToken = default);

Task<AnnouncementAsResearchGroup> UpdateAnnouncementAsync(
  long announcementId, 
  AnnouncementAsResearchGroup updates, 
  CancellationToken cancellationToken = default);

Task<bool> DeleteAnnouncementAsync(
  long announcementId, 
  CancellationToken cancellationToken = default);

Task<List<AnnouncementAsResearchGroup>> GetUploadedAnnouncementsAsync(
  string researchGroupEmail, 
  CancellationToken cancellationToken = default);

Task<List<AnnouncementAsResearchGroup>> GetFilteredAnnouncementsAsync(
  string researchGroupEmail, 
  string status, 
  CancellationToken cancellationToken = default);

Task<int> UpdateBulkAnnouncementsStatusAsync(
  List<long> announcementIds, 
  string newStatus, 
  CancellationToken cancellationToken = default);

Task<bool> DeleteBulkAnnouncementsAsync(
  List<long> announcementIds, 
  CancellationToken cancellationToken = default);

// Announcements Viewing
Task<List<AnnouncementAsCompany>> GetPublishedCompanyAnnouncementsAsync(
  CancellationToken cancellationToken = default);

Task<List<AnnouncementAsProfessor>> GetPublishedProfessorAnnouncementsAsync(
  CancellationToken cancellationToken = default);

Task<List<AnnouncementAsResearchGroup>> GetPublishedResearchGroupAnnouncementsAsync(
  CancellationToken cancellationToken = default);

// Events
Task<List<CompanyEvent>> GetCompanyEventsAsync(
  CancellationToken cancellationToken = default);

Task<List<ProfessorEvent>> GetProfessorEventsAsync(
  CancellationToken cancellationToken = default);

Task<List<CompanyEvent>> GetCompanyEventsForMonthAsync(
  int year, int month, 
  CancellationToken cancellationToken = default);

Task<List<ProfessorEvent>> GetProfessorEventsForMonthAsync(
  int year, int month, 
  CancellationToken cancellationToken = default);

Task<HashSet<long>> GetInterestedCompanyEventIdsAsync(
  string researchGroupEmail, 
  CancellationToken cancellationToken = default);

// Searches (basic name/term filtering)
Task<List<Company>> SearchCompaniesAsync(
  CompanySearchCriteria criteria, 
  CancellationToken cancellationToken = default);

Task<List<Professor>> SearchProfessorsAsync(
  ProfessorSearchCriteria criteria, 
  CancellationToken cancellationToken = default);

// Statistics
Task<ResearchGroupStatisticsDTO> GetStatisticsAsync(
  string researchGroupEmail, 
  CancellationToken cancellationToken = default);
```

#### Component Changes

**Before** (ResearchGroupAnnouncementsSection):
```csharp
[Inject] private IDbContextFactory<AppDbContext> DbContextFactory { get; set; }

private async Task SaveResearchGroupAnnouncementAsPublished()
{
  // ... validation ...
  await using var context = await DbContextFactory.CreateDbContextAsync();
  
  var announcement = new AnnouncementAsResearchGroup { /* ... */ };
  context.Add(announcement);
  await context.SaveChangesAsync(); // ← FORBIDDEN in component
  
  await LoadAnnouncementsData(); // Reload manually
}

private async Task LoadAnnouncementsData()
{
  await using var context = await DbContextFactory.CreateDbContextAsync();
  
  announcements = await context.AnnouncementsAsCompany
    .Where(a => a.CompanyAnnouncementStatus == "Δημοσιευμένη")
    .OrderByDescending(a => a.CompanyAnnouncementUploadDate)
    .ToListAsync(); // ← FORBIDDEN in component
  
  // 5 more queries like this
}
```

**After** (ResearchGroupAnnouncementsSection):
```csharp
[Inject] private IResearchGroupDashboardService ResearchGroupDashboardService { get; set; }

private async Task SaveResearchGroupAnnouncementAsPublished()
{
  // ... validation ...
  var result = await ResearchGroupDashboardService.CreateAnnouncementAsync(
    new AnnouncementAsResearchGroup { /* ... */ });
  
  if (result != null)
  {
    ResearchGroupAnnouncements.Add(result);
    showSuccessMessage = true;
  }
  StateHasChanged();
}

private async Task LoadAnnouncementsData()
{
  var (company, professor, researchGroup) = await Task.WhenAll(
    ResearchGroupDashboardService.GetPublishedCompanyAnnouncementsAsync(),
    ResearchGroupDashboardService.GetPublishedProfessorAnnouncementsAsync(),
    ResearchGroupDashboardService.GetPublishedResearchGroupAnnouncementsAsync()
  );
  
  announcements = company.ToList();
  ProfessorAnnouncements = professor.ToList();
  ResearchGroupAnnouncements = researchGroup.ToList();
}
```

**Lines Eliminated**: 238 (27% reduction from 838 → 600)

---

### Service 2: StudentDashboardService

**Priority**: HIGH (affects 3 components)  
**Current Size**: 50.29 KB  
**New Methods**: 5  
**Target Start**: Week 1 (Day 2)

#### New Methods Required

```csharp
Task<List<CompanyEvent>> GetCompanyEventsAsync(
  CancellationToken cancellationToken = default);

Task<List<ProfessorEvent>> GetProfessorEventsAsync(
  CancellationToken cancellationToken = default);

Task<List<CompanyEvent>> GetCompanyEventsForMonthAsync(
  int year, int month, 
  CancellationToken cancellationToken = default);

Task<List<ProfessorEvent>> GetProfessorEventsForMonthAsync(
  int year, int month, 
  CancellationToken cancellationToken = default);

Task<List<Company>> SearchCompaniesAsync(
  CompanySearchCriteria criteria, 
  CancellationToken cancellationToken = default);
```

**Affected Components**:
- StudentEventsSection (~135 lines eliminated)
- StudentCompanySearchSection (~100 lines eliminated)

---

### Service 3: CompanyDashboardService

**Priority**: HIGH (affects 5 components)  
**Current Size**: 122.11 KB  
**New Methods**: 6  
**Target Start**: Week 1 (Day 3)

#### New Methods Required

```csharp
Task<List<CompanyEvent>> GetCompanyEventsForMonthAsync(
  int year, int month, 
  CancellationToken cancellationToken = default);

Task<List<ProfessorEvent>> GetProfessorEventsForMonthAsync(
  int year, int month, 
  CancellationToken cancellationToken = default);

Task<List<Student>> SearchStudentsAsync(
  StudentSearchCriteria criteria, 
  CancellationToken cancellationToken = default);

Task<List<Professor>> SearchProfessorsAsync(
  ProfessorSearchCriteria criteria, 
  CancellationToken cancellationToken = default);

Task<List<ResearchGroup>> SearchResearchGroupsAsync(
  ResearchGroupSearchCriteria criteria, 
  CancellationToken cancellationToken = default);

Task<List<string>> GetStudentNameSuggestionsAsync(
  string prefix, 
  CancellationToken cancellationToken = default);
```

**Affected Components**:
- CompanyEventsSection (~100 lines)
- CompanyStudentSearchSection (~150 lines)
- CompanyProfessorSearchSection (~150 lines)
- CompanyResearchGroupSearchSection (~150 lines)

---

### Service 4: ProfessorDashboardService

**Priority**: HIGH (affects 6 components)  
**Current Size**: 73.14 KB  
**New Methods**: 6  
**Target Start**: Week 2 (Day 1)

#### New Methods Required

```csharp
Task<List<CompanyEvent>> GetCompanyEventsForMonthAsync(
  int year, int month, 
  CancellationToken cancellationToken = default);

Task<List<ProfessorEvent>> GetProfessorEventsForMonthAsync(
  int year, int month, 
  CancellationToken cancellationToken = default);

Task<List<Student>> SearchStudentsAsync(
  StudentSearchCriteria criteria, 
  CancellationToken cancellationToken = default);

Task<List<Company>> SearchCompaniesAsync(
  CompanySearchCriteria criteria, 
  CancellationToken cancellationToken = default);

Task<List<ResearchGroup>> SearchResearchGroupsAsync(
  ResearchGroupSearchCriteria criteria, 
  CancellationToken cancellationToken = default);

Task LogPlatformActionAsync(
  string userRole, string forWhat, string hashedPositionRng, 
  string typeOfAction, DateTime actionTime, 
  CancellationToken cancellationToken = default);
```

**Affected Components**:
- ProfessorEventsSection (~100 lines)
- ProfessorStudentSearchSection (~150 lines)
- ProfessorCompanySearchSection (~150 lines)
- ProfessorResearchGroupSearchSection (~150 lines)

---

## Step-by-Step Roadmap

### Week 1 (Days 1-5): High-Impact Targets

#### Day 1: ResearchGroupDashboardService Expansion
- [ ] Add 16 new methods to `IResearchGroupDashboardService`
- [ ] Implement methods in `ResearchGroupDashboardService`
- [ ] Write unit tests for new methods
- [ ] Create 3 DTOs: `AnnouncementCreateRequest`, `AnnouncementUpdateRequest`, `ResearchGroupStatisticsDTO`

**Validation Checklist**:
- [ ] No UI component references in service
- [ ] No IQueryable exposed to components
- [ ] All SaveChangesAsync centralized in service
- [ ] DTOs have no lazy-loaded navigation properties

#### Days 2-3: StudentDashboardService + CompanyDashboardService
- [ ] Expand `IStudentDashboardService` with 5 methods
- [ ] Expand `ICompanyDashboardService` with 6 methods
- [ ] Implement all methods
- [ ] Write unit tests
- [ ] Create necessary request/response DTOs

#### Day 4: Begin CRUDListManager Wrapper
- [ ] Create `Components/Shared/CRUDListManager.razor`
- [ ] Implement all state management (pagination, bulk edit, filtering)
- [ ] Write RenderFragment contract
- [ ] Create demo page with sample data

#### Day 5: Refactor First Component (CompanyJobsSection)
- [ ] Remove 300+ lines of pagination code
- [ ] Replace with `<CRUDListManager>`
- [ ] Verify pagination works
- [ ] Run component tests
- [ ] Document any changes needed for other components

### Week 2 (Days 6-10): Medium-Impact Targets

#### Day 6: Refactor Remaining CRUD Components (9 total)
- [ ] CompanyInternshipsSection
- [ ] CompanyThesesSection
- [ ] CompanyAnnouncementsSection
- [ ] ProfessorJobsSection (if exists)
- [ ] ProfessorInternshipsSection
- [ ] ProfessorThesesSection
- [ ] ProfessorAnnouncementsSection
- [ ] ResearchGroupAnnouncementsSection
- [ ] CompanyEventsSection

#### Day 7: Extract MultiFieldSearchForm
- [ ] Create `Components/Shared/MultiFieldSearchForm.razor`
- [ ] Implement form state management
- [ ] Implement result pagination
- [ ] Create demo page

#### Day 8: Refactor Search Components (9 total)
- [ ] StudentCompanySearchSection
- [ ] CompanyStudentSearchSection
- [ ] CompanyProfessorSearchSection
- [ ] CompanyResearchGroupSearchSection
- [ ] ProfessorStudentSearchSection
- [ ] ProfessorCompanySearchSection
- [ ] ProfessorResearchGroupSearchSection
- [ ] ResearchGroupCompanySearchSection
- [ ] ResearchGroupProfessorSearchSection

#### Days 9-10: Extract CalendarEventViewer
- [ ] Create `Components/Shared/CalendarEventViewer.razor`
- [ ] Implement month navigation state
- [ ] Implement calendar grid rendering
- [ ] Implement event aggregation
- [ ] Refactor all 4 event components

### Week 3 (Days 11-15): Wrap-Up

#### Days 11-12: Extract InterestSubmissionButton + BulkActionConfirmationModal
- [ ] Create `Components/Shared/InterestSubmissionButton.razor`
- [ ] Create `Components/Shared/BulkActionConfirmationModal.razor`
- [ ] Refactor 5 + 9 affected components

#### Days 13-15: Testing & Documentation
- [ ] Full component integration tests
- [ ] Service unit tests
- [ ] Wrapper component tests
- [ ] Documentation updates
- [ ] Performance testing (ensure no regressions)

---

## Risk Mitigation

### Risk 1: Wrapper Contract Misalignment (HIGH)

**Problem**: Parent components don't pass required data or events to wrappers.

**Mitigation**:
- Create a `WrapperContractValidator` utility to check required parameters
- Add detailed XML comments to each wrapper parameter
- Create example implementations for each wrapper type
- Use TypeScript-style strict null checking

### Risk 2: Performance Regression (MEDIUM)

**Problem**: Wrappers add rendering overhead, event callback chains slow components.

**Mitigation**:
- Benchmark before/after with Blazor profiler
- Keep state management in wrapper (no unnecessary parent re-renders)
- Use `@key` directive for list items
- Consider virtual scrolling for large lists (future optimization)

### Risk 3: State Management Confusion (HIGH)

**Problem**: Components unclear about who owns which state (wrapper vs. parent).

**Mitigation**:
- Document state ownership clearly for each wrapper
- Use immutable patterns where possible
- Create a "State Ownership Reference" document

### Risk 4: Service Method Signature Instability (MEDIUM)

**Problem**: Service method signatures don't match what components expect.

**Mitigation**:
- Define DTO contracts before implementation
- Write integration tests for each service method
- Create adapters if needed (service method → component expectation)

### Risk 5: DbContext Still Leaking (HIGH)

**Problem**: Components still inject DbContextFactory accidentally.

**Mitigation**:
- Remove all `IDbContextFactory<AppDbContext>` from components
- Add linting rule to catch `DbContextFactory` injections
- Code review checklist: "DbContext injection removed?"
- Add CI/CD step to grep for forbidden patterns

---

## Testing Strategy

### Unit Tests (Per Wrapper)

**CRUDListManager Tests**:
```csharp
[TestClass]
public class CRUDListManagerTests
{
  [TestMethod]
  public void Pagination_NavigatesToCorrectPage()
  {
    // Arrange
    var items = Enumerable.Range(1, 50).Select(i => new MockEntity { Id = i });
    var component = new CRUDListManager<MockEntity> { Items = items, PageSize = 10 };

    // Act
    component.GoToPage(2);

    // Assert
    Assert.AreEqual(2, component.CurrentPage);
    var paginated = component.FilteredItems.ToList();
    Assert.AreEqual(10, paginated.Count);
    Assert.AreEqual(11, paginated.First().Id);
  }

  [TestMethod]
  public void BulkSelect_SelectsAndDeselectsCorrectly()
  {
    // Arrange
    var items = Enumerable.Range(1, 10).Select(i => new MockEntity { Id = i });
    var component = new CRUDListManager<MockEntity> { Items = items };

    // Act
    component.HandleItemCheckboxChanged(items.First(), true);
    component.HandleItemCheckboxChanged(items.First(), false);

    // Assert
    Assert.AreEqual(0, component.SelectedItemIds.Count);
  }
}
```

**MultiFieldSearchForm Tests**:
```csharp
[TestMethod]
public async Task Search_ExecutesCallback()
{
  // Arrange
  var searchExecuted = false;
  var component = new MultiFieldSearchForm<Company>
  {
    OnSearchSubmitted = EventCallback.Factory.Create<SearchCriteria>(
      this, 
      _ => { searchExecuted = true; return Task.CompletedTask; })
  };

  // Act
  await component.ExecuteSearch();

  // Assert
  Assert.IsTrue(searchExecuted);
  Assert.IsTrue(component.HasSearched);
}
```

### Integration Tests (Per Component Refactoring)

**CompanyJobsSection Integration Test**:
```csharp
[TestClass]
public class CompanyJobsSectionRefactoringTests
{
  private ICompanyDashboardService _service;

  [TestInitialize]
  public void Setup()
  {
    _service = new MockCompanyDashboardService();
  }

  [TestMethod]
  public async Task Component_LoadsInitialData_ViaService()
  {
    // Arrange
    var component = new CompanyJobsSection { Service = _service };

    // Act
    await component.OnInitializedAsync();

    // Assert
    Assert.IsNotNull(component.UploadedJobs);
    Assert.IsTrue(component.UploadedJobs.Count > 0);
  }

  [TestMethod]
  public async Task Component_PaginationWorks_WithWrapper()
  {
    // Arrange & Act
    // (Test using Bunit renderer)

    // Assert
    // Verify pagination controls present
    // Verify data displayed on correct page
  }
}
```

### Service Tests

**ResearchGroupDashboardService Tests**:
```csharp
[TestClass]
public class ResearchGroupDashboardServiceTests
{
  private IResearchGroupDashboardService _service;
  private AppDbContext _dbContext;

  [TestInitialize]
  public void Setup()
  {
    _dbContext = new TestDbContextFactory().CreateDbContext();
    _service = new ResearchGroupDashboardService(_dbContext);
  }

  [TestMethod]
  public async Task CreateAnnouncement_SavesAndReturns()
  {
    // Arrange
    var announcement = new AnnouncementAsResearchGroup
    {
      ResearchGroupAnnouncementTitle = "Test",
      ResearchGroupAnnouncementDescription = "Test Description"
    };

    // Act
    var result = await _service.CreateAnnouncementAsync(announcement);

    // Assert
    Assert.IsNotNull(result);
    Assert.IsTrue(result.ResearchGroupAnnouncementId > 0);
  }

  [TestMethod]
  public async Task GetPublishedAnnouncements_ReturnsOnlyPublished()
  {
    // Arrange
    // Add both published and unpublished announcements

    // Act
    var results = await _service.GetPublishedCompanyAnnouncementsAsync();

    // Assert
    Assert.IsTrue(results.All(a => a.CompanyAnnouncementStatus == "Δημοσιευμένη"));
  }
}
```

### End-to-End Tests

**Full Component Flow Test**:
```csharp
[TestClass]
public class E2E_CompanyJobsSectionTests
{
  [TestMethod]
  public async Task UserFlow_CreateJob_EditJob_PublishJob()
  {
    // Arrange
    var app = new TestApplication();
    var page = await app.NavigateTo("/company/jobs");

    // Act & Assert
    // 1. User fills job form
    var form = page.Find("form.create-job-form");
    form.Fill("job-title", "Senior Developer");
    form.Fill("job-description", "Requirements...");

    // 2. User submits form (service called)
    var saveButton = page.Find("button.save-job");
    await saveButton.ClickAsync();

    // 3. Verify job appears in list (via wrapper)
    var jobList = page.Find(".crud-list-container");
    var jobRow = jobList.Find("tr[data-id='1']");
    Assert.IsNotNull(jobRow);

    // 4. User selects job for bulk publish
    var checkbox = jobRow.Find("input[type='checkbox']");
    await checkbox.ClickAsync();

    // 5. User executes bulk publish
    var bulkPublish = page.Find("button.bulk-publish");
    await bulkPublish.ClickAsync();

    // 6. Verify modal appears and execution works
    // ...
  }
}
```

---

## Success Metrics

### Code Quality Metrics

| Metric | Before | After | Target |
|--------|--------|-------|--------|
| **Component Lines** | 26,629 | 19,284 | ✓ 28% reduction |
| **Wrapper Components** | 0 | 5 | ✓ 100% coverage |
| **DbContext Injections** | 12 | 0 | ✓ 100% elimination |
| **SaveChangesAsync in Components** | 6 | 0 | ✓ 100% elimination |
| **Service Methods** | ~80 | 113 | ✓ +33 methods |
| **Code Duplication** | 24% | 0% | ✓ In wrappers |

### Performance Metrics

| Metric | Baseline | Target | Measurement |
|--------|----------|--------|-------------|
| **Initial Page Load** | <2s | <2s | Same or faster |
| **Component Render Time** | N/A | <50ms per component | Blazor profiler |
| **Memory Usage** | N/A | <100MB peak | Browser DevTools |
| **Search Pagination** | N/A | <100ms per page | Stopwatch in test |

### Maintainability Metrics

| Metric | Before | After | Impact |
|--------|--------|-------|--------|
| **Time to Fix Pagination Bug** | 10 edits × 15min = 150min | 1 edit × 15min = 15min | **90% reduction** |
| **Time to Add Search Field** | 9 edits × 20min = 180min | 1 edit × 20min = 20min | **89% reduction** |
| **New Dev Onboarding** | 1 week | 3 days | **57% reduction** |
| **Test Coverage** | ~60% | >90% | **30% improvement** |

### Risk Reduction Metrics

| Category | Before | After | Target |
|----------|--------|-------|--------|
| **Untestable Components** | 19 | 0 | ✓ 100% testable |
| **Critical Dependencies** | 12 DbContext injections | 0 | ✓ No violations |
| **State Management Issues** | Scattered across 28 components | Centralized in 5 wrappers | ✓ Single source of truth |

---

## Timeline Estimates

### Detailed Week Breakdown

#### Week 1: Foundation & High-Impact Work

| Day | Task | Estimated Time | Buffer |
|-----|------|-----------------|--------|
| Mon | ResearchGroupDashboardService (16 methods + DTOs) | 4h | 1h |
| Tue | StudentDashboardService (5 methods) + CompanyDashboardService (6 methods) | 4h | 1h |
| Wed | CRUDListManager Wrapper + demo page | 5h | 1h |
| Thu | Refactor CompanyJobsSection (first component) | 2h | 30m |
| Fri | Resolve any issues from CompanyJobsSection | 2h | 1h |
| **Week 1 Total** | | **17 hours** | **4.5 hours** |

#### Week 2: Scaling & New Patterns

| Day | Task | Estimated Time | Buffer |
|-----|------|-----------------|--------|
| Mon | Refactor 9 CRUD components (batch) | 6h | 1h |
| Tue | MultiFieldSearchForm Wrapper + demo | 5h | 1h |
| Wed | Refactor 9 Search components (batch) | 6h | 1h |
| Thu | CalendarEventViewer Wrapper + demo | 5h | 1h |
| Fri | Refactor 4 Event components | 3h | 1h |
| **Week 2 Total** | | **25 hours** | **5 hours** |

#### Week 3: Wrap-Up & Testing

| Day | Task | Estimated Time | Buffer |
|-----|------|-----------------|--------|
| Mon | InterestSubmissionButton + BulkActionConfirmationModal wrappers | 4h | 1h |
| Tue | Refactor 14 affected components (batch) | 4h | 1h |
| Wed | Unit tests for all wrappers | 5h | 1h |
| Thu | Service integration tests | 4h | 1h |
| Fri | E2E tests + performance validation | 4h | 1h |
| **Week 3 Total** | | **21 hours** | **5 hours** |

### Total Effort Estimate

| Phase | Hours | Days (8h/day) |
|-------|-------|---------------|
| Planning & Setup | 2h | 0.25 days |
| Wrapper Extraction | 25h | 3.1 days |
| Service Expansion | 10h | 1.25 days |
| Component Refactoring | 22h | 2.75 days |
| Testing & Validation | 13h | 1.6 days |
| **TOTAL** | **72 hours** | **~9 days (2 weeks at 8h/day)** |

**Buffer for Issues**: +20% = 14.4 hours  
**Grand Total with Buffer**: 86.4 hours ≈ **11 days (2-2.5 weeks)**

---

## Completion Checklist

### Code Delivery

- [ ] 5 Wrapper components created and fully documented
- [ ] 4 Service interfaces updated with 33 new methods
- [ ] 4 Service implementations completed with all methods
- [ ] 28 Components refactored to use services and wrappers
- [ ] All DTOs created (search criteria, responses, statistics)
- [ ] Zero DbContext injections in components

### Testing

- [ ] Wrapper component tests (unit) - 90%+ coverage
- [ ] Service unit tests - 90%+ coverage
- [ ] Component integration tests - 80%+ coverage
- [ ] E2E tests for critical user flows
- [ ] Performance regression tests pass
- [ ] No breaking changes to existing functionality

### Documentation

- [ ] Wrapper usage guide (examples for each)
- [ ] Service method documentation (XML comments)
- [ ] State ownership reference document
- [ ] Migration guide for developers
- [ ] Architecture decision record (ADR) for Phase 1

### Validation

- [ ] All components build without errors
- [ ] No compiler warnings
- [ ] No linting violations
- [ ] All tests pass (green CI/CD)
- [ ] No performance regression
- [ ] Code review sign-off

---

## Next Steps After Phase 1

After Phase 1 completion, evaluate Phase 2 options:

1. **Architecture Consolidation** (4-8 weeks)
   - Merge role models
   - Unify search logic
   - Extract shared services for Announcements/Events

2. **Feature Enhancement**
   - Add real-time notifications
   - Implement advanced search filters
   - Add bulk export functionality

3. **Performance Optimization**
   - Implement virtual scrolling for large lists
   - Add query result caching
   - Optimize database indexes

Choose based on next priority from your roadmap.

