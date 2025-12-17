# Wiring Pattern Explanation

> **⚠️ NOTE: This document describes Pattern 1 (dumb components with parameters), which has been superseded by Pattern 2 (smart components with code-behind).**  
> The project has migrated to Pattern 2. See Serena memory `pattern2_component_architecture` for current approach.

## What Does "Establish the Pattern" Mean? (Pattern 1 - Deprecated)

When I say "establish the pattern, then apply it to the rest," I mean:

### Step 1: Wire ONE Component Completely (Establish the Pattern)

**For CompanyAnnouncementsSection.razor:**
1. ✅ **Analyze dependencies** (already done)
   - Find all properties/methods used in the component
   - Map them to MainLayout.razor.cs

2. ⏳ **Create parameter contract** (in progress)
   - Define all `[Parameter]` declarations needed
   - Document EventCallbacks

3. ⏳ **Add @code section to component**
   ```razor
   @code {
       [Parameter] public bool IsLoadingUploadedAnnouncements { get; set; }
       [Parameter] public bool IsUploadedAnnouncementsVisible { get; set; }
       [Parameter] public EventCallback ToggleUploadedAnnouncementsVisibility { get; set; }
       // ... all other parameters
   }
   ```

4. ⏳ **Update component markup**
   - Replace direct references like `isLoadingUploadedAnnouncements` with `IsLoadingUploadedAnnouncements`
   - Replace method calls like `ToggleUploadedAnnouncementsVisibility()` with `await ToggleUploadedAnnouncementsVisibility.InvokeAsync()`

5. ⏳ **Test/Verify** (if possible)
   - Check that component structure is correct
   - Verify all parameters are defined

**Result:** CompanyAnnouncementsSection.razor is now a **standalone, parameterized component** ready to receive data from Company.razor.

---

### Step 2: Apply Same Pattern to Other Components

Once we've wired CompanyAnnouncementsSection, we use the **exact same process** for the other 8 Company components:

#### CompanyJobsSection.razor (1,658 lines)
- Follow same 5 steps
- Analyze dependencies → Create parameters → Add @code → Update markup → Verify

#### CompanyInternshipsSection.razor (1,978 lines)
- Follow same 5 steps
- Same process...

#### CompanyThesesSection.razor (2,914 lines)
- Follow same 5 steps
- Same process...

#### CompanyEventsSection.razor (2,602 lines)
- Follow same 5 steps
- Same process...

#### CompanyStudentSearchSection.razor (671 lines)
- Follow same 5 steps
- Same process...

#### CompanyProfessorSearchSection.razor (528 lines)
- Follow same 5 steps
- Same process...

#### CompanyResearchGroupSearchSection.razor (692 lines)
- Follow same 5 steps
- Same process...

#### CompanyAnnouncementsManagementSection.razor (630 lines)
- Follow same 5 steps
- Same process...

---

## Why Establish Pattern First?

### Benefits:
1. **Learn the Process**: First component teaches us:
   - What works
   - What doesn't work
   - Common issues to watch for
   - Best practices

2. **Refine the Approach**: After first component, we can:
   - Adjust the process if needed
   - Create templates/checklists
   - Document common patterns

3. **Build Confidence**: Successfully wiring one component:
   - Proves the approach works
   - Gives us a reference example
   - Makes remaining components easier

4. **Reusable Knowledge**: Patterns discovered:
   - Can be applied to all other components
   - Save time on subsequent components
   - Ensure consistency

---

## Example: Pattern Discovery

### What We Might Learn from CompanyAnnouncementsSection:

**Pattern 1: Visibility Toggles**
```csharp
// Pattern discovered:
[Parameter] public bool IsVisible { get; set; }
[Parameter] public EventCallback ToggleVisibility { get; set; }
```
→ **Apply to**: All components with visibility toggles

**Pattern 2: Pagination**
```csharp
// Pattern discovered:
[Parameter] public int CurrentPage { get; set; }
[Parameter] public int TotalPages { get; set; }
[Parameter] public EventCallback<int> GoToPage { get; set; }
```
→ **Apply to**: All components with pagination

**Pattern 3: Bulk Operations**
```csharp
// Pattern discovered:
[Parameter] public bool IsBulkEditMode { get; set; }
[Parameter] public HashSet<int> SelectedIds { get; set; }
[Parameter] public EventCallback<string> ExecuteBulkStatusChange { get; set; }
```
→ **Apply to**: Components with bulk operations (Jobs, Internships, Theses)

---

## The Complete Flow

```
1. Wire CompanyAnnouncementsSection (ESTABLISH PATTERN)
   ├─ Analyze dependencies
   ├─ Create parameter contract
   ├─ Add @code section
   ├─ Update markup
   └─ Verify structure
   
2. Apply Pattern to CompanyJobsSection
   ├─ Use same process (faster now)
   └─ Reuse discovered patterns
   
3. Apply Pattern to CompanyInternshipsSection
   ├─ Use same process
   └─ Reuse discovered patterns
   
... (repeat for all 9 components)

9. Wire Company.razor (INTEGRATION)
   ├─ Replace inline sections with component references
   ├─ Pass parameters from MainLayout.razor.cs
   └─ Test full Company role
```

---

## What "Apply to the Rest" Means

**It means:**
- Use the **same 5-step process** for each component
- Reuse **patterns discovered** (visibility, pagination, bulk ops)
- Follow the **same structure** (@code section, parameter naming)
- Use **same verification** checklist

**It does NOT mean:**
- Copy-paste code (each component has unique needs)
- Skip analysis (each component needs dependency analysis)
- Assume they're identical (components have different complexities)

---

## Summary

**"Establish the pattern"** = Wire one component completely to learn the process

**"Apply to the rest"** = Use the same proven process for the other 8 components, reusing discovered patterns

This approach ensures:
- ✅ Consistency across all components
- ✅ Faster progress (learn once, apply many times)
- ✅ Better quality (refined process)
- ✅ Easier maintenance (consistent structure)

