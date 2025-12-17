# UserRole Analysis - MainLayout.razor.cs

## Overview
This document analyzes where and how `UserRole` is defined, initialized, and used in the MainLayout code-behind file.

## UserRole Definition

**Location:** `MainLayout.razor.cs`, Line 164

```csharp
// ============================================================================
// USER ROLE MANAGEMENT
// ============================================================================
// UserRole: Stores the current user's role (e.g., "Student", "Company", "Professor", "Admin", "Research Group")
// - Defined here: Line 164
// - Initialized in: OnInitializedAsync() at line 1295 from authentication claims
// - Used in: ShouldShowAdminTable() at line 178, and in MainLayout.razor for conditional rendering
// - Note: This is a private field. If components need access, consider making it public or passing as parameter
private string UserRole = "";
```

**Type:** `private string`  
**Initial Value:** `""` (empty string)  
**Visibility:** Private (appropriate for code-behind pattern)

## UserRole Initialization

**Location:** `MainLayout.razor.cs`, Line 1295  
**Method:** `OnInitializedAsync()`

```csharp
// ============================================================================
// USER ROLE INITIALIZATION
// ============================================================================
// UserRole is initialized here from the authentication claims
// Possible values: "Student", "Company", "Professor", "Admin", "Research Group"
// This value is used throughout the application for role-based UI rendering
UserRole = user.FindFirst(ClaimTypes.Role)?.Value; // Get user's role
```

**Source:** Authentication claims (`ClaimTypes.Role`)  
**Timing:** During component initialization (OnInitializedAsync)

## UserRole Usage

### 1. In Code-Behind (MainLayout.razor.cs)

**Location:** Line 178  
**Method:** `ShouldShowAdminTable()`

```csharp
// ============================================================================
// USER ROLE USAGE: Admin Table Visibility
// ============================================================================
// Checks if admin table should be displayed based on UserRole
// Used by: Admin.razor component via ShouldShowAdminTable() method
private bool ShouldShowAdminTable()
{
    return UserRole == "Admin" && !NavigationManager.Uri.Contains("/profile", StringComparison.OrdinalIgnoreCase);
}
```

**Purpose:** Determines if the Admin table should be displayed

### 2. In Razor Markup (MainLayout.razor)

**Location:** Line 1553

```razor
@if (UserRole == "Research Group")
{
    @if (!isInitializedAsResearchGroupUser)
    {
        <!-- Research Group loading and content -->
    }
}
```

**Purpose:** Conditionally renders Research Group content

## Possible UserRole Values

Based on code analysis, the following role values are used:

1. **"Student"** - Student users
2. **"Company"** - Company users
3. **"Professor"** - Professor users
4. **"Admin"** - Administrator users
5. **"Research Group"** - Research Group users

## Code-Behind File Structure

**Status:** ✅ Correct

- **Class Declaration:** `public partial class MainLayout : LayoutComponentBase`
- **Pattern:** Code-behind pattern (separation of markup and logic)
- **UserRole Visibility:** Private (appropriate - components don't need direct access)
- **Component Access:** Components receive data via `[Parameter]` declarations, not direct field access

## Recommendations

### Current State
- ✅ UserRole is properly defined as a private field
- ✅ UserRole is initialized from authentication claims
- ✅ UserRole is used appropriately for conditional rendering
- ✅ Code-behind structure is correct

### No Changes Needed
The current implementation is appropriate for a code-behind file:
- Components (`Student.razor`, `Company.razor`, `Professor.razor`, `Admin.razor`) are conditionally rendered in `MainLayout.razor` based on `UserRole`
- Components receive data via parameters, not direct field access
- The private visibility of `UserRole` is appropriate for encapsulation

### Future Considerations
If components need to know the current user's role:
1. **Option 1:** Pass `UserRole` as a parameter to components (if needed)
2. **Option 2:** Make `UserRole` public (not recommended - breaks encapsulation)
3. **Option 3:** Use `AuthenticationStateProvider` directly in components (recommended for role checks)

## Related Files

- `MainLayout.razor` - Uses `UserRole` for conditional rendering
- `Student.razor` - Rendered when `UserRole == "Student"` (handled by parent)
- `Company.razor` - Rendered when `UserRole == "Company"` (handled by parent)
- `Professor.razor` - Rendered when `UserRole == "Professor"` (handled by parent)
- `Admin.razor` - Uses `ShouldShowAdminTable()` which checks `UserRole == "Admin"`

## Notes

- The `UserRole_PerformedAction` properties found in grep results are **NOT** the same as `UserRole` - they are properties of the `PlatformActions` entity used for logging actions
- All role-specific components are now extracted and wired via parameters, so direct `UserRole` access in components is not needed

