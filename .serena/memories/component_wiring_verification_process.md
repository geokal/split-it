# Component Wiring Verification Process

## Purpose
This memory documents the mandatory verification process that must be performed for ALL user-role components after wiring them to MainLayout.razor.cs.

## When to Perform
- After adding `@code` section with `[Parameter]` declarations
- After updating component markup to use parameters
- Before marking component as "complete"
- Before moving to the next component

## Verification Steps

### 1. Symbol Existence Verification
Use Serena's `find_symbol` tool to verify ALL symbols referenced in the component markup exist in `MainLayout.razor.cs`:

**Check these symbol types:**
- Properties/Fields (e.g., `isLoadingUploadedAnnouncements`, `FilteredAnnouncements`)
- Methods/EventCallbacks (e.g., `ToggleUploadedAnnouncementsVisibility`, `HandleStatusFilterChange`)
- Computed properties (e.g., `GetPaginatedAnnouncements()`)
- Collections (e.g., `pageSizeOptions_SeeMyUploadedAnnouncementsAsCompany`)

**Example verification:**
```bash
mcp_serena_find_symbol name_path_pattern="isLoadingUploadedAnnouncements" relative_path="MainLayout.razor.cs"
```

### 2. Casing Verification and Fix
**CRITICAL**: MainLayout.razor.cs uses **camelCase** for all properties/fields. Component markup MUST match exactly.

**Common casing issues to check:**
- `IsLoadingUploadedAnnouncements` → `isLoadingUploadedAnnouncements`
- `IsUploadedAnnouncementsVisible` → `isUploadedAnnouncementsVisible`
- `PageSizeForAnnouncements` → `pageSizeForAnnouncements`
- `CurrentPageForAnnouncements` → `currentPageForAnnouncements`
- `TotalPagesForAnnouncements` → `totalPagesForAnnouncements`
- `SelectedStatusFilterForAnnouncements` → `selectedStatusFilterForAnnouncements`
- `TotalCountAnnouncements` → `totalCountAnnouncements`
- `IsBulkEditModeForAnnouncements` → `isBulkEditModeForAnnouncements`
- `SelectedAnnouncementIds` → `selectedAnnouncementIds`

**How to find casing issues:**
1. Search markup for PascalCase patterns: `grep -n "[A-Z][a-z]*[A-Z]" ComponentName.razor`
2. Compare with parameter declarations in `@code` section
3. Fix all mismatches to use camelCase

### 3. Parameter Completeness Check
Verify that ALL symbols used in markup have corresponding `[Parameter]` declarations:

**Check for:**
- Direct property references (e.g., `@isLoadingUploadedAnnouncements`)
- Method calls (e.g., `@ToggleUploadedAnnouncementsVisibility.InvokeAsync()`)
- Computed properties (may need to be passed as `Func<T>` or computed locally)
- Collections used in `@foreach` loops

### 4. EventCallback Verification
For all `EventCallback` parameters, verify:
- Method exists in MainLayout.razor.cs
- Method signature matches (parameters, return type)
- Markup uses `.InvokeAsync()` correctly (or direct assignment for simple cases)

### 5. Create Verification Report
For each component, create a verification report documenting:
- All verified symbols with line numbers in MainLayout.razor.cs
- All casing fixes applied
- Any missing symbols or issues found
- Final status (✅ Complete or ⚠️ Issues Found)

## Verification Checklist

For EACH component, verify:

- [ ] All properties/fields exist in MainLayout.razor.cs
- [ ] All methods/EventCallbacks exist in MainLayout.razor.cs
- [ ] All casing matches (camelCase, not PascalCase)
- [ ] All `[Parameter]` declarations match actual usage
- [ ] No missing symbols in markup
- [ ] EventCallbacks use correct syntax (`.InvokeAsync()` or direct assignment)
- [ ] Computed properties handled correctly (passed as Func or computed locally)
- [ ] Verification report created

## Tools to Use

1. **Serena find_symbol**: Verify symbol existence
   ```bash
   mcp_serena_find_symbol name_path_pattern="symbolName" relative_path="MainLayout.razor.cs"
   ```

2. **grep**: Find casing issues
   ```bash
   grep -n "[A-Z][a-z]*[A-Z]" ComponentName.razor
   ```

3. **read_lints**: Check for compilation errors
   ```bash
   read_lints paths=['ComponentName.razor']
   ```

## Example Verification Report Template

```markdown
# Wiring Verification Report: ComponentName.razor

**Date**: YYYY-MM-DD
**Status**: ✅ All symbols verified and casing fixed

## ✅ Verified Symbols
- `symbolName` (Field/Method, line XXXX)
- ...

## ✅ Casing Fixes Applied
- `OldName` → `newName`
- ...

## ⚠️ Known Issues
- Issue description

## Summary
**Total Parameters**: XX
**Verified Symbols**: ✅ All verified
**Casing Issues**: ✅ All fixed
**Missing Symbols**: ❌ None found
```

## Important Notes

1. **Never skip verification** - This is mandatory for all components
2. **Casing is critical** - Blazor is case-sensitive, mismatches will cause runtime errors
3. **Verify before marking complete** - Don't move to next component until verification passes
4. **Document everything** - Create verification report for each component
5. **Use exact names** - Parameter names must match MainLayout.razor.cs exactly (camelCase)

## Components Requiring Verification

Apply this process to ALL components in:
- `Shared/Company/*.razor` (9 components)
- `Shared/Professor/*.razor` (7 components)
- `Shared/Student/*.razor` (6 components)
- `Shared/ResearchGroup/*.razor` (5 components)
- `Shared/Admin/*.razor` (1 component)

**Total: 28 components requiring verification**
