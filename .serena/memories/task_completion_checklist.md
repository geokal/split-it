# Task Completion Checklist

## Component Extraction (from EXTRACTION_STANDARD.md)
1. ✅ Use anchor divs (`<div id="...-start"></div>`) for precise boundaries
2. ✅ Extract using `sed -n 'start,end p'` to preserve exact markup
3. ✅ Verify component starts with proper opening tag (not closing tags)
4. ✅ Check for no overlapping sections between components
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

## Verification Checklist (from EXTRACTION_STANDARD.md)
For each extracted component:
- [ ] Starts with proper opening tag (div, @if, etc.)
- [ ] Does NOT start with closing tags (</div>, }, etc.)
- [ ] Ends with proper closing tag or anchor div
- [ ] No leftover content from previous sections
- [ ] All divs are properly balanced (or intentionally unbalanced if parent provides structure)
- [ ] Component is self-contained and can be integrated into parent

## Common Issues to Avoid (from EXTRACTION_STANDARD.md)
1. **Including Modals from Previous Sections**: Modals often extend beyond tab-pane boundaries. Always use anchor divs to exclude them.
2. **Starting with Closing Tags**: Components should never start with `</div>`, `}`, or other closing tags. If this happens, the start boundary is incorrect.
3. **Missing Content**: Ensure the end boundary includes all content up to (and including) the anchor div or closing tag.
4. **Overlapping Boundaries**: Components should not overlap. Each component should have distinct, non-overlapping boundaries.
