# Component Extraction Standards

## Source of Truth
- **Always use `MainLayout.razor.before_split`** as the source for extraction
- This file is the version after Task 0 (anchor div addition) but before role-specific extraction
- Line count: ~39,265 lines

## Extraction Method (from EXTRACTION_STANDARD.md)

### Use Anchor Divs for Boundaries
**Always use anchor divs (`<div id="...-start"></div>` and `<div id="...-end"></div>`) to determine component boundaries.**

### Extraction Process
1. **Identify Anchor Divs**: `grep -n 'id=".*-start"\|id=".*-end"' <Role>.razor`
2. **Map Components to Anchors**: Find start and end anchor divs for each component
3. **Extract Component**: `sed -n '<start_line>,<end_line>p' <Role>.razor > Shared/<Role>/<ComponentName>.razor`
4. **Verify**: Component starts with proper opening tag, no leftover content from previous sections

### Anchor Div Mappings (Company.razor)
- `companies-start` (line 1) - Start of Company section
- `energes-theseis-ergasias-company` (line 1753) - End of Jobs section
- `internships-tab-companies-start` (line 2984) - Start of Internships section
- `thesis-companies-start` (line 4966) - Start of Theses section
- `company-thesis-end` (line 7880) - End of Theses section
- `company-events-start` (line 7890) - Start of Events section
- `company-events-end` (line 10492) - End of Events section
- `company-student-search-start` (line 10497) - Start of Student Search section
- `company-student-search-end` (line 11168) - End of Student Search section
- `company-for-professor-search-tab-start` (line 11172) - Start of Professor Search section

### When Anchor Divs Are Missing
1. Use tab-pane boundaries as fallback
2. Look for clear section markers (comments, specific div IDs)
3. Ensure no content from previous sections is included
4. Verify component starts with proper opening tag (not closing tags)

## Common Issues to Avoid
1. **Including Modals from Previous Sections**: Always use anchor divs to exclude them
2. **Starting with Closing Tags**: Components should never start with `</div>`, `}`, etc.
3. **Missing Content**: Ensure end boundary includes all content up to anchor div
4. **Overlapping Boundaries**: Components should not overlap

## Verification Checklist
- [ ] Starts with proper opening tag (div, @if, etc.)
- [ ] Does NOT start with closing tags (</div>, }, etc.)
- [ ] Ends with proper closing tag or anchor div
- [ ] No leftover content from previous sections
- [ ] All divs are properly balanced
- [ ] Component is self-contained

## Component Organization Rules (from AGENTS.md)
- **Common code across all user roles** → `Shared/`
- **Role-specific sections** → `Shared/<Role>/ComponentName.razor`
- **Only place code in Shared/** if it is exactly the same for all user roles
- **If code is not exactly the same**, it should not be a shared component
- **If a component is only in one user role**, create folder under Shared with that user-role name
