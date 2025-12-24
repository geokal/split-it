# Bootstrap UI Patterns and Best Practices

## Bootstrap Tabs Pattern

### Correct Structure
```html
<!-- Parent Component (e.g., CompanySection.razor) -->
<div class="tab-content mt-3" id="myTabContent">
    <div class="tab-pane fade show active" id="tab-id" role="tabpanel">
        <ChildComponent />
    </div>
</div>
```

### Key Rules
1. **Parent wraps, child doesn't**: Only the parent component should have tab-pane divs
2. **No nested tab-panes**: Child components should render content directly, not wrapped in tab-pane
3. **Bootstrap handles automatically**: No custom JavaScript initialization needed
4. **Use data attributes**: `data-bs-toggle="tab"` and `data-bs-target="#targetId"` on buttons

### Common Mistakes to Avoid
- ❌ Adding tab-pane wrappers in child components (creates nested structure)
- ❌ Custom JavaScript initialization (Bootstrap handles it automatically)
- ❌ CSS rules with `!important` (interferes with Bootstrap's dynamic class management)
- ❌ Missing render mode configuration (needed for .NET 8 Blazor Server)

### Fixed Components (January 2025)
- CompanyInternshipsSection.razor - Removed nested tab-pane wrapper
- CompanyThesesSection.razor - Removed nested tab-pane wrapper
- CompanyStudentSearchSection.razor - Removed nested tab-pane wrapper
- CompanyProfessorSearchSection.razor - Removed nested tab-pane wrapper
- CompanyResearchGroupSearchSection.razor - Removed nested tab-pane wrapper

## Toggle Buttons Pattern

### Structure
```html
<div class="mb-3 row-dark-gray">
    <div class="d-flex justify-content-between align-items-center" 
         @onclick="ToggleMethod" 
         style="cursor: pointer;">
        <label>• Section Title •</label>
        <button class="btn btn-link" 
                @onclick="ToggleMethod" 
                @onclick:stopPropagation="true">
            @if (isVisible) { <span>&#8722;</span> } 
            else { <span>&#43;</span> }
        </button>
    </div>
</div>

@if (isVisible) {
    <!-- Content here -->
}
```

### Implementation
- Use boolean flags in code-behind (e.g., `isUploadCompanyJobsFormVisible`)
- Toggle method calls `StateHasChanged()` after updating flag
- Button uses `@onclick:stopPropagation="true"` to prevent double-triggering

## Render Mode Configuration

### Required for .NET 8 Blazor Server
1. Add to `_Imports.razor`:
   ```razor
   @using static Microsoft.AspNetCore.Components.Web.RenderMode
   ```

2. Use in `App.razor`:
   ```html
   <HeadOutlet @rendermode="InteractiveServer" />
   <Routes @rendermode="InteractiveServer" />
   ```

## Reference Implementation
- JobFinder-refactored project at `/Users/georgek/Downloads/JobFinder-refactored`
- Uses no custom JavaScript for Bootstrap tabs
- Uses no custom CSS for tab visibility
- Components follow parent-wraps, child-doesn't pattern
