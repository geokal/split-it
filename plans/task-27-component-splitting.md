# Task 27: Further Component Splitting for Professor, Student, and ResearchGroup

## Status: In Progress

**Context**: This task continues the component extraction pattern established in Professor and Company sections, applying it to further split large components into smaller, more maintainable pieces.

---

## Background

Previous component extraction work has established successful patterns:

### Professor Sections (Already Started)
- ✅ ProfessorStudentDetailModal.razor (265 lines)
- ✅ ProfessorInternshipDetailModal.razor (233 lines)
- ✅ ProfessorResearchGroupDetailModal.razor (272 lines)
- ✅ ProfessorCompanyDetailModal.razor (270 lines)
- Parent files reduced significantly (-1119 lines total)

### Company Sections (User reported "huge progress")
- Multiple components already extracted and refactored
- Pattern established for modal extraction

---

## Objectives

Continue component splitting to:
1. **Reduce component file sizes** - Target: Keep components under 500 lines
2. **Improve maintainability** - Smaller, focused components
3. **Extract reusable UI components** - Create shared components where possible
4. **Maintain Pattern 2 architecture** - Keep .razor + .razor.cs separation
5. **Ensure data access through services** - No direct database access in UI

---

## Target Components for Splitting

### A. Professor Sections (Further Splitting)

**1. ProfessorEventsSection.razor**
- Current state: Has event loading and calendar logic
- Potential extractions:
  - Extract event creation form to ProfessorEventCreateForm.razor
  - Extract calendar view to ProfessorEventsCalendar.razor (already exists)
  - Extract event detail modals to ProfessorEventsDetailModals.razor (already exists)
  - Extract event table to ProfessorEventsTable.razor (already exists)
- Goal: Reduce parent component to ~300-400 lines

**2. ProfessorInternshipsSection.razor**
- Current state: Already has ProfessorInternshipDetailModal extracted
- Potential extractions:
  - Extract internship creation form to ProfessorInternshipCreateForm.razor
  - Extract internship list/table to ProfessorInternshipsTable.razor
- Goal: Reduce parent component to ~300-400 lines

**3. ProfessorThesesSection.razor**
- Current state: May be large with thesis CRUD logic
- Potential extractions:
  - Extract thesis creation form to ProfessorThesisCreateForm.razor
  - Extract thesis detail modal to ProfessorThesisDetailModal.razor
  - Extract thesis list/table to ProfessorThesesTable.razor
- Goal: Reduce parent component to ~300-400 lines

**4. ProfessorAnnouncementsManagementSection.razor**
- Current state: May have announcement CRUD logic
- Potential extractions:
  - Extract announcement creation form to ProfessorAnnouncementCreateForm.razor
  - Extract announcement list/table to ProfessorAnnouncementsTable.razor
- Goal: Reduce parent component to ~300-400 lines

**5. ProfessorSearch Sections (Student, Company, ResearchGroup)**
- Current state: Have pagination and filtering logic
- Potential extractions:
  - Extract search results table to reusable component
  - Extract filter controls to reusable component
- Goal: Reduce each search section to ~200-300 lines

### B. Student Sections (New Extractions)

**1. StudentEventsSection.razor**
- Current state: May have event display logic
- Potential extractions:
  - Extract event detail modal to StudentEventDetailModal.razor
  - Extract event registration form to StudentEventRegistrationForm.razor
  - Extract event list/table to StudentEventsTable.razor
- Goal: Reduce parent component to ~300-400 lines

**2. StudentInternshipsSection.razor**
- Current state: May have internship application logic
- Potential extractions:
  - Extract internship detail modal to StudentInternshipDetailModal.razor
  - Extract application form to StudentInternshipApplicationForm.razor
  - Extract internship list/table to StudentInternshipsTable.razor
- Goal: Reduce parent component to ~300-400 lines

**3. StudentJobsDisplaySection.razor**
- Current state: May have job application logic
- Potential extractions:
  - Extract job detail modal to StudentJobDetailModal.razor
  - Extract application form to StudentJobApplicationForm.razor
  - Extract job list/table to StudentJobsTable.razor
- Goal: Reduce parent component to ~300-400 lines

**4. StudentThesisDisplaySection.razor**
- Current state: May have thesis application logic
- Potential extractions:
  - Extract thesis detail modal to StudentThesisDetailModal.razor
  - Extract application form to StudentThesisApplicationForm.razor
  - Extract thesis list/table to StudentThesesTable.razor
- Goal: Reduce parent component to ~300-400 lines

**5. StudentCompanySearchSection.razor**
- Current state: Has search and filtering logic
- Potential extractions:
  - Extract search results table to reusable component
  - Extract filter controls to reusable component
  - Extract company detail modal to StudentCompanyDetailModal.razor
- Goal: Reduce parent component to ~200-300 lines

### C. ResearchGroup Sections (New Extractions)

**1. ResearchGroupEventsSection.razor**
- Current state: Has event loading and calendar logic
- Potential extractions:
  - Extract event creation form to ResearchGroupEventCreateForm.razor
  - Extract event detail modal to ResearchGroupEventDetailModal.razor
  - Extract event list/table to ResearchGroupEventsTable.razor
- Goal: Reduce parent component to ~300-400 lines

**2. ResearchGroupAnnouncementsSection.razor**
- Current state: Uses ResearchGroupDashboardService.UpdateAnnouncementAsync
- Potential extractions:
  - Extract announcement creation form to ResearchGroupAnnouncementCreateForm.razor
  - Extract announcement list/table to ResearchGroupAnnouncementsTable.razor
- Goal: Reduce parent component to ~300-400 lines

**3. ResearchGroupSearch Sections (Company, Professor)**
- Current state: Have pagination and area filtering logic
- Potential extractions:
  - Extract search results table to reusable component
  - Extract filter controls to reusable component
  - Extract detail modals for search results
- Goal: Reduce each search section to ~200-300 lines

---

## Extraction Patterns to Follow

### Pattern 1: Modal Extraction
Extract detail modals from parent sections:
- Create separate .razor component for modal
- Pass data through parameters
- Handle modal state in parent component
- Use EventCallback for user actions

### Pattern 2: Form Extraction
Extract creation/edit forms from parent sections:
- Create separate .razor component for form
- Use EditForm or EditContext pattern
- Pass validation state through parameters
- Handle form submission via EventCallback

### Pattern 3: Table/List Extraction
Extract data display tables from parent sections:
- Create separate .razor component for table
- Use RenderFragment or Template pattern for flexibility
- Handle pagination in parent or component
- Pass data through parameters

### Pattern 4: Reusable Component Extraction
Extract common UI patterns to shared components:
- Identify repeated UI patterns across components
- Create parameterized reusable components
- Place in Components/Helpers/ or appropriate section folder
- Use across multiple parent components

---

## Implementation Steps

### Phase 1: Professor Sections Further Splitting
1. Analyze ProfessorEventsSection.razor for extraction opportunities
2. Extract event creation form to ProfessorEventCreateForm.razor
3. Verify ProfessorEventsCalendar.razor, ProfessorEventsDetailModals.razor, ProfessorEventsTable.razor are properly integrated
4. Analyze ProfessorInternshipsSection.razor for extraction opportunities
5. Extract internship creation form to ProfessorInternshipCreateForm.razor
6. Analyze ProfessorThesesSection.razor for extraction opportunities
7. Extract thesis creation form to ProfessorThesisCreateForm.razor
8. Extract thesis detail modal to ProfessorThesisDetailModal.razor
9. Analyze ProfessorAnnouncementsManagementSection.razor for extraction opportunities
10. Extract announcement creation form to ProfessorAnnouncementCreateForm.razor
11. Test all Professor sections after extractions
12. Create WIP commit for Professor sections splitting

### Phase 2: Student Sections New Extractions
1. Analyze StudentEventsSection.razor for extraction opportunities
2. Extract event detail modal to StudentEventDetailModal.razor
3. Extract event registration form to StudentEventRegistrationForm.razor
4. Analyze StudentInternshipsSection.razor for extraction opportunities
5. Extract internship detail modal to StudentInternshipDetailModal.razor
6. Extract application form to StudentInternshipApplicationForm.razor
7. Analyze StudentJobsDisplaySection.razor for extraction opportunities
8. Extract job detail modal to StudentJobDetailModal.razor
9. Extract application form to StudentJobApplicationForm.razor
10. Analyze StudentThesisDisplaySection.razor for extraction opportunities
11. Extract thesis detail modal to StudentThesisDetailModal.razor
12. Extract application form to StudentThesisApplicationForm.razor
13. Analyze StudentCompanySearchSection.razor for extraction opportunities
14. Extract company detail modal to StudentCompanyDetailModal.razor
15. Test all Student sections after extractions
16. Create WIP commit for Student sections splitting

### Phase 3: ResearchGroup Sections New Extractions
1. Analyze ResearchGroupEventsSection.razor for extraction opportunities
2. Extract event creation form to ResearchGroupEventCreateForm.razor
3. Extract event detail modal to ResearchGroupEventDetailModal.razor
4. Analyze ResearchGroupAnnouncementsSection.razor for extraction opportunities
5. Extract announcement creation form to ResearchGroupAnnouncementCreateForm.razor
6. Analyze ResearchGroupSearch sections for extraction opportunities
7. Extract detail modals for search results
8. Test all ResearchGroup sections after extractions
9. Create WIP commit for ResearchGroup sections splitting

### Phase 4: Reusable Component Extraction
1. Identify common UI patterns across all sections
2. Create parameterized reusable components
3. Update parent components to use reusable components
4. Test reusable components across different contexts
5. Create WIP commit for reusable components

---

## Refactoring Checklist

For each component extraction, verify:
- [ ] New component follows Pattern 2 architecture (.razor + .razor.cs)
- [ ] New component has proper namespace (QuizManager.Components.Layout.*Sections)
- [ ] Parent component reduced in size (target: <500 lines)
- [ ] No direct database access in new component
- [ ] Data access goes through appropriate dashboard service
- [ ] Component is reusable where appropriate
- [ ] Component has proper parameter passing
- [ ] Component uses EventCallback for user actions
- [ ] Component handles error states appropriately
- [ ] Component has proper loading states
- [ ] Build succeeds with no errors
- [ ] Component follows established naming conventions

---

## Success Criteria

- All target components reduced to <500 lines
- All extracted components follow Pattern 2 architecture
- No direct database access in UI components
- Reusable components identified and extracted
- All components use dashboard services for data access
- Build succeeds with no errors
- Component naming follows established patterns
- Code is more maintainable and testable

---

## Next Steps

1. Begin Phase 1: Professor Sections Further Splitting
2. Create targeted WIP commits after each phase
3. Update PROGRESS.md with Task 27 completion
4. Document component extraction patterns in docs/
5. Create reusable component library documentation
