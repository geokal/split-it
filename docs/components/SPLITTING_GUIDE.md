# Guide: Splitting MainLayout.razor into Markup and Code-Behind

## Why Split?

✅ **Current Pattern**: The project already uses this pattern (`MainLayout.razor` + `MainLayout.razor.cs`)  
✅ **Best Practice**: Blazor recommends separating markup from code  
✅ **Maintainability**: Easier to work with large files  
✅ **Tooling**: Better IntelliSense and navigation  
✅ **Consistency**: Matches existing project structure

## How to Split

### Step 1: Create MainLayout.razor (Markup Only)

**Keep in MainLayout.razor:**
- `@inherits LayoutComponentBase` (or remove if in code-behind)
- `@using` directives
- `@inject` directives
- All HTML/Razor markup
- Component references (`<Shared.*>`)
- **Remove**: All `@code { }` blocks

### Step 2: Create MainLayout.razor.cs (Code-Behind)

**Move to MainLayout.razor.cs:**
- All `@code { }` content
- Class declaration: `public partial class MainLayout : LayoutComponentBase`
- All properties, methods, event handlers
- All using statements for C# code

**Structure:**
```csharp
using Microsoft.AspNetCore.Components;

public partial class MainLayout : LayoutComponentBase
{
    // All properties
    private string UserRole = "";
    private bool isInitialized = false;
    // ... etc

    // All methods
    private void SomeMethod() { }
    // ... etc
}
```

### Step 3: Update MainLayout.razor

**Remove from MainLayout.razor:**
- `@code { }` blocks
- Class declaration (if present)

**Keep in MainLayout.razor:**
- `@inherits LayoutComponentBase` (optional - can be in code-behind)
- All `@using` directives
- All `@inject` directives
- All markup

## Current Structure Reference

### MainLayout.razor (Current)
```razor
@inherits LayoutComponentBase
@using LinqKit
@inject FileUploadService FileUploadService
// ... more directives

<style>
    /* CSS */
</style>

<div>
    <!-- All markup here -->
</div>
```

### MainLayout.razor.cs (Current)
```csharp
using Microsoft.AspNetCore.Components;

public partial class MainLayout : LayoutComponentBase
{
    // All properties and methods
    private string UserRole = "";
    // ... 33,000+ lines of code
}
```

## What to Extract from @code Blocks

When you get the new MainLayout.razor, look for:

1. **@code { } blocks** - Move entire content to `.razor.cs`
2. **Properties** - All `private`, `public`, `protected` fields
3. **Methods** - All methods, event handlers, async tasks
4. **Initialization** - `OnInitialized`, `OnAfterRender`, etc.
5. **Lifecycle methods** - All Blazor lifecycle hooks

## What to Keep in .razor File

1. **Directives** - `@using`, `@inject`, `@inherits`
2. **Markup** - All HTML/Razor syntax
3. **Component references** - `<Shared.*>`, `<ComponentName />`
4. **Bindings** - `@bind`, `@onclick`, etc. (these reference code-behind)
5. **Conditional rendering** - `@if`, `@foreach`, etc.

## Verification Checklist

After splitting, verify:

- [ ] `MainLayout.razor.cs` has `public partial class MainLayout : LayoutComponentBase`
- [ ] `MainLayout.razor` has no `@code { }` blocks
- [ ] All properties/methods are in `.razor.cs`
- [ ] All `@using` directives are in both files (as needed)
- [ ] All `@inject` directives are in `.razor` file
- [ ] File compiles without errors
- [ ] IntelliSense works in both files

## Example: Before and After

### Before (Monolithic)
```razor
@inherits LayoutComponentBase
@inject DataService DataService

<div>@UserRole</div>

@code {
    private string UserRole = "";
    
    protected override async Task OnInitializedAsync()
    {
        UserRole = await GetUserRole();
    }
}
```

### After (Split)

**MainLayout.razor:**
```razor
@inherits LayoutComponentBase
@inject DataService DataService

<div>@UserRole</div>
```

**MainLayout.razor.cs:**
```csharp
using Microsoft.AspNetCore.Components;

public partial class MainLayout : LayoutComponentBase
{
    private string UserRole = "";
    
    protected override async Task OnInitializedAsync()
    {
        UserRole = await GetUserRole();
    }
}
```

## Tips

1. **Use Find/Replace**: Search for `@code {` to find all code blocks
2. **Check for nested blocks**: Some `@code` blocks might be nested
3. **Preserve formatting**: Keep indentation consistent
4. **Test incrementally**: Split one section at a time if the file is very large
5. **Backup first**: Always backup before splitting

## Common Issues

**Issue**: Properties not accessible in markup  
**Solution**: Ensure class is `partial` and namespace matches

**Issue**: `@inject` not working  
**Solution**: Keep `@inject` in `.razor` file, not `.razor.cs`

**Issue**: Using statements missing  
**Solution**: Add necessary `using` statements to `.razor.cs`

