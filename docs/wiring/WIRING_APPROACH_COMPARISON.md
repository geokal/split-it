# Wiring Approach Comparison

## Current Situation

**Company.razor** (12,391 lines):
- Contains all inline code (not using components yet)
- Has tab-panes for different sections
- Each section needs to be replaced with component references

**Components** (9 components extracted):
- All components exist in `Shared/Company/`
- Components are NOT yet integrated into Company.razor
- Components need parameters to be wired

---

## Option 1: Wire Components First (Bottom-Up) ⭐ RECOMMENDED

### Process:
1. **Wire each component individually**:
   - Add `@code` section with `[Parameter]` declarations
   - Update component markup to use parameters
   - Test component in isolation (if possible)

2. **Then wire Company.razor**:
   - Replace inline sections with component references
   - Pass parameters from MainLayout.razor.cs to components
   - Test full Company role functionality

### Pros:
✅ **Incremental Progress**: Wire and test one component at a time
✅ **Easier Debugging**: Issues isolated to specific components
✅ **Clear Dependencies**: Each component's needs are explicit
✅ **Reusable**: Components can be tested/used independently
✅ **Manageable**: Work with smaller files (600-3000 lines vs 12,391)
✅ **Matches Extraction Pattern**: We extracted components, now we wire them

### Cons:
❌ **Two-Step Process**: Need to wire components, then wire Company.razor
❌ **Parameter Passing**: Need to ensure Company.razor passes all parameters correctly

### Example Flow:
```
1. Wire CompanyAnnouncementsSection.razor
   → Add @code with [Parameter] declarations
   → Update markup to use parameters
   → Verify component structure

2. Wire CompanyJobsSection.razor
   → Same process...

3. Wire Company.razor
   → Replace inline announcements section with <CompanyAnnouncementsSection />
   → Pass all required parameters
   → Repeat for all 9 components
```

---

## Option 2: Wire Company.razor First (Top-Down)

### Process:
1. **Wire Company.razor directly**:
   - Keep all inline code
   - Add parameter passing logic
   - Replace sections with component references
   - Pass parameters to components

2. **Then wire components**:
   - Add `@code` sections to components
   - Components receive parameters from Company.razor

### Pros:
✅ **Single Integration Point**: Company.razor connects everything
✅ **See Full Picture**: Understand how all components interact
✅ **Faster Integration**: Replace sections and wire in one go

### Cons:
❌ **Large File**: 12,391 lines is hard to navigate and modify
❌ **Harder to Test**: Can't test components independently
❌ **Harder to Debug**: Issues could be in Company.razor or components
❌ **Risk of Breaking**: Large changes to Company.razor could break multiple things
❌ **Less Incremental**: All-or-nothing approach

---

## Recommendation: **Option 1 - Wire Components First** ⭐

### Why?

1. **Company.razor is still large** (12,391 lines)
   - Harder to work with
   - More error-prone
   - Slower to navigate

2. **Components are already extracted**
   - They exist as separate files
   - Natural next step is to wire them
   - Matches our extraction workflow

3. **Incremental and Safe**
   - Wire one component → test → move to next
   - If something breaks, easy to identify and fix
   - Can commit after each component

4. **Better for Collaboration**
   - Each component is self-contained
   - Clear parameter contracts
   - Easier for others to understand

5. **Company.razor becomes simpler**
   - After wiring components, Company.razor just becomes:
     ```razor
     <CompanyAnnouncementsSection 
         IsLoadingUploadedAnnouncements="@isLoadingUploadedAnnouncements"
         IsUploadedAnnouncementsVisible="@isUploadedAnnouncementsVisible"
         ... />
     ```
   - Much cleaner and easier to maintain

---

## Recommended Execution Plan

### Phase 1: Wire Components (One at a time)
1. ✅ Create wiring plan for CompanyAnnouncementsSection
2. ⏳ Wire CompanyAnnouncementsSection.razor
3. ⏳ Wire CompanyJobsSection.razor
4. ⏳ Wire CompanyInternshipsSection.razor
5. ⏳ Wire CompanyThesesSection.razor
6. ⏳ Wire CompanyEventsSection.razor
7. ⏳ Wire CompanyStudentSearchSection.razor
8. ⏳ Wire CompanyProfessorSearchSection.razor
9. ⏳ Wire CompanyResearchGroupSearchSection.razor

### Phase 2: Wire Company.razor
1. Replace inline announcements section with `<CompanyAnnouncementsSection />`
2. Pass all required parameters
3. Repeat for all 9 components
4. Test full Company role functionality

---

## Decision Matrix

| Factor | Wire Components First | Wire Company.razor First |
|--------|----------------------|-------------------------|
| **File Size** | Small (600-3000 lines) | Large (12,391 lines) |
| **Testability** | High (isolated) | Low (integrated) |
| **Debugging** | Easy (isolated) | Hard (integrated) |
| **Incremental** | Yes | No |
| **Risk** | Low | High |
| **Maintainability** | High | Medium |
| **Speed** | Slower (more steps) | Faster (one step) |

**Winner: Wire Components First** (6-1)

---

## Conclusion

**Recommendation: Wire Components First (Bottom-Up)**

This approach is:
- ✅ Safer (incremental, testable)
- ✅ More maintainable (clear boundaries)
- ✅ Better aligned with extraction pattern
- ✅ Easier to debug and fix issues

The only downside is it takes more steps, but the benefits far outweigh this.

