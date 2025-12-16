# Task Completion Checklist

## After Component Extraction
1. ✅ Verify component boundaries using anchor divs or tab-pane markers
2. ✅ Check component starts with proper opening tag (not closing tags/braces)
3. ✅ Verify no overlapping sections between components
4. ✅ Check for missing content (gaps between components)
5. ✅ Verify extracted markup matches source (MD5 checksum if needed)
6. ✅ Update `REFACTORING_PLAN.md` with extraction status
7. ✅ Update `COMPONENT_DEPENDENCIES.md` with component parameters

## After Component Wiring
1. ✅ Identify all `[Parameter]` declarations in component
2. ✅ Find corresponding properties/methods in `MainLayout.razor.cs`
3. ✅ Add parameter passing in parent component (e.g., `Company.razor`)
4. ✅ Verify EventCallbacks are properly connected
5. ✅ Test component functionality (if possible)
6. ✅ Update `COMPONENT_DEPENDENCIES.md` with wiring status

## Before Committing
1. ✅ Verify all components are properly formatted
2. ✅ Check for syntax errors (Razor markup)
3. ✅ Ensure no duplicate code
4. ✅ Update documentation files (`PROGRESS.md`, `AGENTS.md`)
5. ✅ Review changes with `git diff`
6. ✅ Commit with descriptive message

## Code Quality Checks
- No hardcoded values (use parameters)
- Proper error handling (if applicable)
- Consistent naming conventions
- Proper component boundaries
- No orphaned code
