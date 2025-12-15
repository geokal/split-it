# Diff Analysis Plan for New MainLayout.razor

## Strategy: Incremental Wiring Based on Diff

When the new `MainLayout.razor` arrives, we'll use a diff-based approach to minimize overhead and only wire what's actually changed.

## Step-by-Step Process

### 1. Initial Comparison
- Compare new `MainLayout.razor` with current one
- Use anchor divs to identify sections:
  - `<div id="companies-start"></div>` to `<div id="companies-end"></div>`
  - `<div id="students-tab-table-start"></div>` to `<div id="students-tab-table-end"></div>`
  - `<div id="professors-tab-table-start"></div>` to `<div id="professors-tab-table-end"></div>`
  - `<div id="admin-table-end"></div>`

### 2. Identify Changes
**What to look for:**
- ✅ Sections that now use components (e.g., `<Shared.Company.CompanyAnnouncementsManagementSection />`)
- ✅ Sections that still have inline code (need extraction)
- ✅ Sections that reference properties/methods from `MainLayout.razor.cs`
- ✅ New sections that weren't in the old file

### 3. Categorize Changes

#### Category A: Already Wired Components
- Components that already have `[Parameter]` declarations
- Components that already have parameter passing in role files
- **Action:** Verify parameters match, adjust if needed

#### Category B: New Component References
- New `<Shared.*>` component references
- **Action:** Check if component exists, wire if needed

#### Category C: Inline Code Sections
- Large blocks of inline code that should be components
- **Action:** Extract to component first, then wire

#### Category D: Direct Property/Method References
- Direct references to `MainLayout.razor.cs` properties/methods
- **Action:** These should work automatically (same partial class)

### 4. Efficient Wiring Strategy

**Priority Order:**
1. **Verify existing wiring** - Check if already-wired components work
2. **Wire new component references** - Add parameters to new components
3. **Extract remaining inline code** - Only if substantial
4. **Fix broken references** - Address any compilation errors

## Tools for Diff Analysis

### Anchor Divs (Section Markers)
Use these to identify sections in the diff:
- `companies-start` / `companies-end`
- `students-tab-table-start` / `students-tab-table-end`
- `professors-tab-table-start` / `professors-tab-table-end`
- `jobs-tab-companies-end`
- `internships-tab-companies-start` / `internships-tab-companies-end`
- `thesis-companies-start` / `company-thesis-end`
- `company-events-start` / `company-events-end`
- `company-student-search-start` / `company-student-search-end`
- `company-for-professor-search-tab-start`

### Component Inventory
**Already Prepared:**
- ✅ `CompanyAnnouncementsManagementSection` - Fully parameterized
- ✅ `ResearchGroupSearchSection` - Fully wired
- ✅ `CompanyInternshipsSection` - Extracted
- ✅ `CompanyEventsSection` - Extracted
- ✅ `CompanyThesesSection` - Extracted
- ✅ `ProfessorAnnouncementsManagementSection` - Extracted
- ✅ `ProfessorThesesSection` - Extracted
- ✅ `StudentThesisDisplaySection` - Extracted
- ✅ `StudentJobsDisplaySection` - Extracted
- ✅ `StudentAppliedInternshipsSection` - Extracted
- ✅ `StudentInternshipsDisplaySection` - Extracted

## Expected Outcomes

### Minimal Overhead Scenarios
- ✅ New MainLayout.razor uses same component structure → Just verify wiring
- ✅ New MainLayout.razor has minor changes → Adjust only changed sections
- ✅ Components already parameterized → Just verify parameter passing

### Moderate Overhead Scenarios
- ⚠️ New component references → Wire new components
- ⚠️ Changed component usage → Update parameter passing

### Higher Overhead Scenarios
- ❌ Major structural changes → May need re-extraction
- ❌ New inline code sections → Extract first, then wire

## Quick Reference: What's Already Done

### Company.razor Components
- `CompanyAnnouncementsManagementSection` - ✅ 50+ parameters ready
- `CompanyInternshipsSection` - ⚠️ Needs wiring
- `CompanyEventsSection` - ⚠️ Needs wiring
- `CompanyThesesSection` - ⚠️ Needs wiring
- `ResearchGroupSearchSection` - ✅ Fully wired
- `CompanyAnnouncementsSection` - ✅ Already wired
- `ProfessorAnnouncementsSection` - ✅ Already wired
- `ResearchGroupAnnouncementsSection` - ✅ Already wired

### Professor.razor Components
- `ProfessorAnnouncementsManagementSection` - ⚠️ Needs wiring
- `ProfessorThesesSection` - ⚠️ Needs wiring

### Student.razor Components
- `StudentThesisDisplaySection` - ⚠️ Needs wiring
- `StudentJobsDisplaySection` - ⚠️ Needs wiring
- `StudentAppliedInternshipsSection` - ⚠️ Needs wiring
- `StudentInternshipsDisplaySection` - ⚠️ Needs wiring
- `StudentCompanySearchSection` - ✅ Already wired

## Next Steps When New File Arrives

1. **Split the new MainLayout.razor** (if it contains `@code` blocks)
   - Extract all `@code { }` blocks to `MainLayout.razor.cs`
   - Keep only markup in `MainLayout.razor`
   - See `SPLITTING_GUIDE.md` for detailed instructions
2. **Save current MainLayout.razor as backup**
3. **Run diff comparison**
4. **Identify changed sections using anchor divs**
5. **Categorize changes (A, B, C, D)**
6. **Prioritize wiring based on categories**
7. **Execute incremental wiring**
8. **Test and validate**

