# Component Dependency Extraction Strategy

## Overview
This document outlines the strategy for extracting missing properties and methods from the original `MainLayout.razor.cs` to individual component code-behind files after MainLayout minimization.

## The Problem

### What Happened
1. Components were originally written to reference `MainLayout`'s properties and methods directly
2. `MainLayout.razor.cs` was minimized from 34,017 lines to 127 lines
3. All MainLayout properties/methods that components depended on were removed
4. This broke all component references, creating ~5,600 CS0103 errors

### Why It Happened
- Components were tightly coupled to MainLayout
- Minimization happened before dependency extraction
- Large component files (2000+ lines) with many MainLayout references = hundreds of errors per file

## The Solution

### Source File
- **Location**: `backups/MainLayout.razor.cs.backup`
- **Size**: 33,977 lines
- **Content**: Complete original MainLayout implementation
- **Purpose**: Reference file to extract implementations from

### Target Files
- **Location**: `Components/Layout/[Role]/[Component]Section.razor.cs`
- **Pattern**: Each component has its own code-behind file
- **Purpose**: Contain component-specific properties and methods

## Extraction Process

### Step 1: Identify Missing Dependencies
```bash
# Build and check for CS0103 errors
dotnet build 2>&1 | grep "error CS0103" | grep "[ComponentName]"

# Example output:
# error CS0103: The name 'SearchCompaniesAsProfessor' does not exist in the current context
# error CS0103: The name 'Regions' does not exist in the current context
# error CS0103: The name 'showCompanyDetailsModal' does not exist in the current context
```

### Step 2: Search Backup File
```bash
# Search for method/property in backup file
grep -n "private void SearchCompaniesAsProfessor" backups/MainLayout.razor.cs.backup
grep -n "private List<string> Regions" backups/MainLayout.razor.cs.backup
grep -n "private bool showCompanyDetailsModal" backups/MainLayout.razor.cs.backup
```

### Step 3: Extract Implementation
```csharp
// From backups/MainLayout.razor.cs.backup (around line XXXX):
private List<string> Regions = new List<string>
{
    "Ανατολική Μακεδονία και Θράκη",
    "Κεντρική Μακεδονία",
    // ... etc
};

private void SearchCompaniesAsProfessor()
{
    // Implementation logic
    searchResultsAsProfessorToFindCompany = dbContext.Companies
        .Where(c => /* search criteria */)
        .ToList();
}

// Copy to Component.razor.cs:
private List<string> Regions = new List<string>
{
    "Ανατολική Μακεδονία και Θράκη",
    "Κεντρική Μακεδονία",
    // ... etc
};

private void SearchCompaniesAsProfessor()
{
    // Implementation logic (may need DbContext injection if not already present)
    searchResultsAsProfessorToFindCompany = dbContext.Companies
        .Where(c => /* search criteria */)
        .ToList();
}
```

### Step 4: Verify Compilation
```bash
# Build to check if errors are fixed
dotnet build 2>&1 | grep "[ComponentName]" | grep "error CS0103" | wc -l

# Should show reduced error count
```

### Step 5: Commit Changes
```bash
# Make targeted commit
git add Components/Layout/[Role]/[Component]Section.razor.cs
git commit -m "fix: Extract missing properties/methods to [Component]Section (X→Y CS0103 errors)"
```

## Common Extraction Patterns

### Pattern 1: Search Properties
```csharp
// Common properties for search functionality
private List<string> Regions = new List<string> { ... };
private Dictionary<string, List<string>> RegionToTownsMap = ...;
private List<string> ForeasType = new List<string> { ... };
private string searchInput = string.Empty;
private List<Entity> searchResults = new List<Entity>();
```

### Pattern 2: Pagination Properties
```csharp
private int currentPage = 1;
private int itemsPerPage = 10;
private int[] pageSizeOptions = new[] { 10, 50, 100 };
private int totalPages => (int)Math.Ceiling((double)searchResults.Count / itemsPerPage);

// Pagination methods
private void GoToFirstPage() { currentPage = 1; }
private void PreviousPage() { if (currentPage > 1) currentPage--; }
private void NextPage() { if (currentPage < totalPages) currentPage++; }
private void GoToLastPage() { currentPage = totalPages; }
private void GoToPage(int page) { currentPage = page; }
```

### Pattern 3: Modal Management
```csharp
private Entity selectedEntity;
private bool showEntityDetailsModal = false;

private void ShowEntityDetails(Entity entity)
{
    selectedEntity = entity;
    showEntityDetailsModal = true;
    StateHasChanged();
}

private void CloseEntityDetailsModal()
{
    showEntityDetailsModal = false;
    selectedEntity = null;
    StateHasChanged();
}
```

### Pattern 4: Bulk Operations
```csharp
private bool isBulkEditMode = false;
private List<int> selectedIds = new List<int>();
private List<Entity> selectedItems = new List<Entity>();

private void EnableBulkEditMode()
{
    isBulkEditMode = true;
    selectedIds.Clear();
    StateHasChanged();
}

private void CancelBulkEdit()
{
    isBulkEditMode = false;
    selectedIds.Clear();
    selectedItems.Clear();
    StateHasChanged();
}

private void ToggleItemSelection(int id, ChangeEventArgs e)
{
    if ((bool)e.Value!)
    {
        if (!selectedIds.Contains(id))
            selectedIds.Add(id);
    }
    else
    {
        selectedIds.Remove(id);
    }
    StateHasChanged();
}
```

### Pattern 5: Autocomplete/Suggestions
```csharp
private string searchInput = string.Empty;
private List<string> suggestions = new List<string>();

private async Task HandleInput(ChangeEventArgs e)
{
    searchInput = e.Value?.ToString() ?? string.Empty;
    
    if (string.IsNullOrEmpty(searchInput))
    {
        suggestions.Clear();
        return;
    }
    
    // Load suggestions from database
    suggestions = await LoadSuggestionsAsync(searchInput);
    StateHasChanged();
}

private void SelectSuggestion(string suggestion)
{
    searchInput = suggestion;
    suggestions.Clear();
    StateHasChanged();
}
```

## Component-Specific Considerations

### DbContext Injection
If extracted methods use `DbContext`, ensure component already has:
```csharp
[Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;

// Or if using direct DbContext (legacy pattern):
[Inject] private AppDbContext dbContext { get; set; } = default!;
```

### StateHasChanged()
After state-changing operations, call `StateHasChanged()`:
```csharp
private void UpdateList()
{
    // ... update logic
    StateHasChanged(); // Notify Blazor to re-render
}
```

### Async Methods
Ensure async methods are properly awaited:
```csharp
private async Task LoadDataAsync()
{
    await using var context = await DbFactory.CreateDbContextAsync();
    // ... load data
}
```

## Verification Checklist

After extracting properties/methods:

- [ ] Component builds without CS0103 errors (check specific component)
- [ ] No new compilation errors introduced
- [ ] Properties are in correct scope (private/protected/public)
- [ ] Methods have correct signatures (async/void/return types)
- [ ] DbContext injection is available if needed
- [ ] StateHasChanged() is called where appropriate
- [ ] Code follows existing patterns in component file

## Progress Tracking

### Metrics
- **Total CS0103 errors**: Track with `dotnet build 2>&1 | grep "error CS0103" | grep -v "\.g\.cs" | wc -l`
- **Component-specific errors**: `dotnet build 2>&1 | grep "error CS0103" | grep "[ComponentName]" | wc -l`
- **Build status**: Check if build succeeds overall

### Documentation
- Update `docs/COMPONENT_EXTRACTION_PROGRESS.md` after each major fix
- Update `PROGRESS.md` with current error counts
- Commit with descriptive messages including error reduction

## Lessons Learned

### ❌ What We Did Wrong
1. Minimized MainLayout **before** extracting component dependencies
2. This broke all component references at once
3. Created thousands of errors that were hard to manage

### ✅ Correct Approach (For Future)
1. **For each component**:
   - Identify dependencies on MainLayout
   - Extract to component's `.cs` file
   - Test component compiles
   - Move to next component
2. **After ALL components are fixed**:
   - Minimize MainLayout
   - Verify everything still works

### Why This Matters
- Incremental fixes are more manageable
- Each component fix can be verified independently
- Easier to track progress and identify issues
- Reduces risk of introducing new bugs

## Next Steps

1. Continue extracting remaining properties/methods (632 CS0103 errors remaining)
2. Fix any Razor syntax errors (RZ9980, RZ9981)
3. Fix other compilation errors (CS1061, CS1503, etc.)
4. Test components after all errors resolved
5. Document final component structure

