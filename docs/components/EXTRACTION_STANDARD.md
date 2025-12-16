# Standard Component Extraction Approach

## Overview
This document defines the standard approach for extracting components from role-specific Razor files (`Company.razor`, `Professor.razor`, `Student.razor`, `ResearchGroup.razor`).

## Principle: Use Anchor Divs for Boundaries

**Always use anchor divs (`<div id="...-start"></div>` and `<div id="...-end"></div>`) to determine component boundaries.**

### Why Anchor Divs?
1. **Precise Boundaries**: Anchor divs mark exact start/end points, avoiding leftover content from previous sections
2. **No Modal Contamination**: Modals and other content that extend beyond tab-panes are properly excluded
3. **Consistent Structure**: All components follow the same extraction pattern
4. **Verifiable**: Easy to verify correctness by checking anchor div presence

## Extraction Process

### Step 1: Identify Anchor Divs
```bash
grep -n 'id=".*-start"\|id=".*-end"' <Role>.razor
```

### Step 2: Map Components to Anchors
For each component:
- Find the corresponding start anchor div
- Find the corresponding end anchor div
- Extract content from start anchor line to end anchor line (inclusive)

### Step 3: Extract Component
```bash
sed -n '<start_line>,<end_line>p' <Role>.razor > Shared/<Role>/<ComponentName>.razor
```

### Step 4: Verify
- Component starts with anchor div or proper opening tag
- Component ends with anchor div or proper closing tag
- No leftover content from previous sections
- No missing content from current section

## Anchor Div Mappings

### Company.razor
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

### Component Boundaries (Company)
1. **CompanyAnnouncementsManagementSection**: Lines 75-703 (tab-pane boundaries)
2. **CompanyAnnouncementsSection**: Lines 711-1342 (after management section)
3. **CompanyJobsSection**: Lines 1343-1752 (before `energes-theseis-ergasias-company` anchor)
4. **CompanyInternshipsSection**: Lines 2984-4965 (between `internships-tab-companies-start` and `internships-tab-companies-end` anchors)
5. **CompanyThesesSection**: Lines 4966-7880 (between `thesis-companies-start` and `company-thesis-end` anchors)
6. **CompanyEventsSection**: Lines 7890-10492 (between `company-events-start` and `company-events-end` anchors)
7. **CompanyStudentSearchSection**: Lines 10497-11168 (between `company-student-search-start` and `company-student-search-end` anchors)
8. **CompanyProfessorSearchSection**: Lines 11172-11699 (between `company-for-professor-search-tab-start` anchor and researchgroup-search tab-pane)
9. **CompanyResearchGroupSearchSection**: Lines 11700-12379 (researchgroup-search tab-pane, no specific end anchor)

### Professor.razor
- `professors-tab-table-start` (line 1) - Start of Professor section
- `professor-ptyxiakes-dimiourgia-ptyxiakis` (line 1555) - Marker for Theses section
- `show-my-uploaded-internships-as-professor` (line 3590) - Marker for Internships section
- `professors-tab-table-end` (line 11246) - End of Professor section

**Note**: Professor components use tab-pane boundaries as anchors are limited. All components should be within `professors-tab-table-start` and `professors-tab-table-end`.

### Student.razor
- `students-tab-table-start` (line 1) - Start of Student section
- `students-tab-table-end` (line 10010) - End of Student section

**Note**: Student components use tab-pane boundaries as anchors are limited. All components should be within `students-tab-table-start` and `students-tab-table-end`.

### ResearchGroup.razor
**Note**: ResearchGroup has no anchor divs. Components were extracted using tab-pane boundaries as fallback approach.

## When Anchor Divs Are Missing

If a section doesn't have anchor divs:
1. Use tab-pane boundaries as fallback
2. Look for clear section markers (comments, specific div IDs)
3. Ensure no content from previous sections is included
4. Verify component starts with proper opening tag (not closing tags)

## Common Issues to Avoid

1. **Including Modals from Previous Sections**: Modals often extend beyond tab-pane boundaries. Always use anchor divs to exclude them.

2. **Starting with Closing Tags**: Components should never start with `</div>`, `}`, or other closing tags. If this happens, the start boundary is incorrect.

3. **Missing Content**: Ensure the end boundary includes all content up to (and including) the anchor div or closing tag.

4. **Overlapping Boundaries**: Components should not overlap. Each component should have distinct, non-overlapping boundaries.

## Verification Checklist

For each extracted component:
- [ ] Starts with proper opening tag (div, @if, etc.)
- [ ] Does NOT start with closing tags (</div>, }, etc.)
- [ ] Ends with proper closing tag or anchor div
- [ ] No leftover content from previous sections
- [ ] All divs are properly balanced (or intentionally unbalanced if parent provides structure)
- [ ] Component is self-contained and can be integrated into parent

## Example: Correct Extraction

**Before (Incorrect):**
```
CompanyEventsSection.razor starts at line 7575
- Includes 316 lines of thesis modal content (WRONG)
- Events tab-pane actually starts at line 7892
```

**After (Correct):**
```
CompanyEventsSection.razor: Lines 7890-10492
- Starts with: <div id="company-events-start"></div>
- Includes: Events tab-pane and all events content
- Ends with: <div id="company-events-end"></div>
- No thesis content included (CORRECT)
```

## Maintenance

When adding new sections:
1. Add anchor divs at section boundaries
2. Update this document with new anchor mappings
3. Re-extract affected components using new boundaries
4. Verify all components still work correctly
