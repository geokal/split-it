# Wiring Test Plan - Code-Context MCP Tool

## Purpose
Test if the code-context MCP tool can help wire extracted components to `MainLayout.razor.cs` by finding symbol dependencies.

## Manual Test Results (Using grep)

### ✅ Test 1: Method Mapping
- **Component usage**: `@onclick="ToggleFormVisibilityForUploadCompanyJobs"` in `CompanyJobsSection.razor`
- **Code-behind**: `private void ToggleFormVisibilityForUploadCompanyJobs()` at line 5448 in `MainLayout.razor.cs`
- **Result**: ✅ Successfully mapped

### ✅ Test 2: Property Mapping
- **Component usage**: `@if (isForm1Visible)` in `CompanyJobsSection.razor`
- **Code-behind**: `private bool isForm1Visible = false;` at line 324 in `MainLayout.razor.cs`
- **Result**: ✅ Successfully mapped

### ✅ Test 3: Binding Pattern
- **Component usage**: `@bind="job.PositionType"` in `CompanyJobsSection.razor`
- **Code-behind needs**: `job` property (Job model instance)
- **Result**: ✅ Pattern identified

## Semantic Search Tests (Once Indexing Completes)

### Test 1: Find All Methods Used in Component
**Query**: `"What methods does CompanyJobsSection call?"`
**Expected**: 
- List of all `@onclick`, `@onchange`, `@oninput` handlers
- Methods like: `ToggleFormVisibilityForUploadCompanyJobs`, `CheckCharacterLimitInJobFieldUploadAsCompany`, etc.

### Test 2: Find All Properties Bound
**Query**: `"What properties are bound in CompanyJobsSection?"`
**Expected**:
- List of all `@bind` directives
- Properties like: `job.PositionType`, `job.PositionTitle`, `isForm1Visible`, etc.

### Test 3: Find Method Definitions
**Query**: `"Where is ToggleFormVisibilityForUploadCompanyJobs defined in MainLayout.razor.cs?"`
**Expected**:
- Exact location (line number) in `MainLayout.razor.cs`
- Method signature and implementation

### Test 4: Find All Dependencies for a Component
**Query**: `"What symbols from MainLayout.razor.cs does CompanyJobsSection depend on?"`
**Expected**:
- Complete list of:
  - Properties (e.g., `isForm1Visible`, `job`, `showErrorMessageforUploadingjobsAsCompany`)
  - Methods (e.g., `ToggleFormVisibilityForUploadCompanyJobs`, `CheckCharacterLimitInJobFieldUploadAsCompany`)
  - Models (e.g., `Job` class)

### Test 5: Group Related Functionality
**Query**: `"How are company job-related methods organized in MainLayout.razor.cs?"`
**Expected**:
- Grouped methods for:
  - Job creation/upload
  - Job editing
  - Job validation
  - Job display/listing

## Success Criteria

The tool is useful for wiring if it can:
1. ✅ Find all symbols used in a component (methods, properties, models)
2. ✅ Locate their definitions in `MainLayout.razor.cs`
3. ✅ Group related functionality together
4. ✅ Handle semantic queries (not just exact text matches)

## Next Steps

1. Wait for indexing to complete
2. Run Test 1-5 queries
3. Evaluate results quality
4. If successful, proceed with systematic wiring using semantic search
5. If not sufficient, fall back to grep-based approach with automation

## Current Status

- **Indexing**: In progress (background)
- **Manual tests**: ✅ All passed
- **Semantic tests**: ⏳ Waiting for indexing

