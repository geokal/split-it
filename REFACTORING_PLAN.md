# Refactoring Plan: Split-It Project

## Current Status Summary

### ✅ Completed
- Role-specific files extracted (Student.razor, Company.razor, Professor.razor, Admin.razor)
- Common components extracted (Pagination, NewsSection, LoadingIndicator, RegistrationPrompt)
- Some role-specific components extracted (CompanyJobsSection, CompanyInternshipsSection, etc.)
- Files renamed (MainLayout.razor, MainLayout.razor.cs)

### ⚠️ Partially Completed
- Pagination: Professor.razor still uses manual pagination
- ResearchGroupSearchSection: Component created but not integrated into Company.razor
- Professor.razor: Still has inline announcement sections (should use components)

### ❌ Not Started
- Wiring code-behind (MainLayout.razor.cs) to extracted components
- Extracting remaining large inline sections
- Admin.razor component extraction

---

## Strategic Decision: Extract First vs Wire First?

### Option A: Continue Extracting Components (Recommended First)
**Pros:**
- Reduces file sizes further, making wiring easier
- Creates clear component boundaries
- Easier to test components in isolation
- Better separation of concerns

**Cons:**
- More components to wire later
- May need to refactor components during wiring

### Option B: Wire Existing Components First
**Pros:**
- Validates current component structure
- Can identify missing dependencies early
- Incremental progress

**Cons:**
- Large files still hard to work with
- May need to extract more during wiring
- More complex wiring with large inline sections

**Recommendation:** **Extract First, Then Wire** - This approach will make the wiring phase cleaner and more manageable.

---

## Priority-Based Action Plan

### Phase 1: Complete Incomplete Tasks (HIGH PRIORITY)
**Goal:** Finish what's already started

#### 1.1 Fix Professor.razor Pagination (Priority: HIGH)
- **Status:** Partially completed (Task 3)
- **Action:** Replace all manual pagination controls in Professor.razor with `Shared.Pagination`
- **Impact:** Standardizes pagination, reduces code duplication
- **Estimated Effort:** Medium
- **Dependencies:** None

#### 1.2 Integrate ResearchGroupSearchSection (Priority: HIGH)
- **Status:** Component created but not integrated (Task 8)
- **Action:** Replace inline research group search code in Company.razor (line ~8495) with `Shared.Company.ResearchGroupSearchSection`
- **Impact:** Completes Task 8, reduces Company.razor size
- **Estimated Effort:** Low
- **Dependencies:** None

#### 1.3 Extract Professor.razor Announcement Sections (Priority: HIGH)
- **Status:** Inline code exists, components available
- **Action:** 
  - Replace inline "Platform research group announcements" with `Shared.Company.ResearchGroupAnnouncementsSection`
  - Consider extracting company/professor announcement sections if they differ from Company.razor versions
- **Impact:** Reduces Professor.razor size, reuses existing components
- **Estimated Effort:** Medium
- **Dependencies:** Verify component compatibility

---

### Phase 2: Extract Large Inline Sections (MEDIUM-HIGH PRIORITY)
**Goal:** Break down remaining large sections into components

#### 2.1 Company.razor - Internships Section (Priority: MEDIUM-HIGH)
- **Location:** Lines ~840-2776 (internships tab)
- **Sections to Extract:**
  - `<!-- ΔΗΜΙΟΥΡΓΙΑ ΠΡΑΚΤΙΚΉΣ ΑΣΚΗΣΗΣ ΩΣ COMPANY USER-->` - Internship creation form
  - `<!-- VIEW UPLOADED INTERNSHIPS AS COMPANY -->` - Internship viewing/management
- **Target:** `Shared/Company/CompanyInternshipCreationSection.razor`, `Shared/Company/CompanyInternshipManagementSection.razor`
- **Estimated Effort:** High (large forms)
- **Dependencies:** None

#### 2.2 Company.razor - Events Section (Priority: MEDIUM)
- **Location:** Lines ~5667-8269 (company events tab)
- **Section:** `<!-- VIEW UPLOADED EVENTS AS COMPANY -->`
- **Target:** `Shared/Company/CompanyEventsSection.razor`
- **Estimated Effort:** Medium
- **Dependencies:** None

#### 2.3 Company.razor - Theses Section (Priority: MEDIUM)
- **Location:** Lines ~2780-5657 (thesis tab)
- **Sections:**
  - Thesis creation/viewing
  - `<!-- SEARCH PROFESSOR THESES AS COMPANY -->`
- **Target:** `Shared/Company/CompanyThesesSection.razor`
- **Estimated Effort:** Medium
- **Dependencies:** None

#### 2.4 Company.razor - Announcements Management (Priority: MEDIUM)
- **Location:** Lines ~47-260
- **Sections:**
  - `<!-- CREATE ANNOUNCEMENTS TAB AS COMPANY -->`
  - `<!-- VIEW UPLOADED ANNOUNCEMENTS AS COMPANY -->`
- **Target:** `Shared/Company/CompanyAnnouncementCreationSection.razor`, `Shared/Company/CompanyAnnouncementManagementSection.razor`
- **Estimated Effort:** Medium
- **Dependencies:** None

#### 2.5 Professor.razor - Announcements Management (Priority: MEDIUM)
- **Location:** Lines ~54-822
- **Sections:**
  - `<!-- CREATE ANNOUNCEMENTS TAB AS PROFESSOR -->`
  - `<!-- SHOW ANNOUNCEMENT AS PROFESSOR - PEDIA INPUTS -->`
  - `<!-- VIEW UPLOADED ANNOUNCEMENTS AS PROFESSOR -->`
- **Target:** `Shared/Professor/ProfessorAnnouncementCreationSection.razor`, `Shared/Professor/ProfessorAnnouncementManagementSection.razor`
- **Estimated Effort:** Medium
- **Dependencies:** None

#### 2.6 Professor.razor - Theses Management (Priority: MEDIUM)
- **Location:** Lines ~1728+
- **Section:** `<!-- VIEW UPLOADED THESES AS PROFESSOR -->`
- **Target:** `Shared/Professor/ProfessorThesesSection.razor`
- **Estimated Effort:** Medium
- **Dependencies:** None

#### 2.7 Student.razor - Display Sections (Priority: MEDIUM)
- **Sections:**
  - `<!-- DISPLAY THESIS POSITIONS -->` (line ~1211)
  - `<!-- DISPLAY JOB POSITIONS -->` (line ~3274)
  - `<!-- DISPLAY INTERNSHIPS -->` (line ~5325)
  - `<!-- SHOW MY APPLIED INTERNSHIPS AS STUDENT -->` (line ~3890)
- **Target:** `Shared/Student/StudentThesisDisplaySection.razor`, `Shared/Student/StudentJobsDisplaySection.razor`, `Shared/Student/StudentInternshipsDisplaySection.razor`
- **Estimated Effort:** High (multiple sections)
- **Dependencies:** None

---

### Phase 3: Wire Code-Behind to Components (HIGH PRIORITY)
**Goal:** Connect MainLayout.razor.cs with extracted components

#### 3.1 Analyze Code-Behind Dependencies (Priority: HIGH)
- **Action:** 
  - Map methods/properties in MainLayout.razor.cs to components
  - Identify which components need which properties/methods
  - Document parameter requirements for each component
- **Estimated Effort:** High (large code-behind file)
- **Dependencies:** Phase 1 & 2 should be mostly complete

#### 3.2 Create Component Parameter Contracts (Priority: HIGH)
- **Action:**
  - Define required parameters for each component
  - Document EventCallbacks needed
  - Create interface/contract documentation
- **Estimated Effort:** Medium
- **Dependencies:** 3.1

#### 3.3 Wire Common Components (Priority: HIGH)
- **Components:** Pagination, NewsSection, LoadingIndicator, RegistrationPrompt
- **Action:** Ensure all usages have proper parameter bindings
- **Estimated Effort:** Low-Medium
- **Dependencies:** 3.2

#### 3.4 Wire Role-Specific Components (Priority: HIGH)
- **Action:** Wire each role's components to MainLayout.razor.cs
  - Start with Company (most components extracted)
  - Then Professor
  - Then Student
  - Finally Admin
- **Estimated Effort:** Very High
- **Dependencies:** 3.2, 3.3

#### 3.5 Testing and Validation (Priority: HIGH)
- **Action:**
  - Test each role's functionality
  - Verify all components work correctly
  - Fix any wiring issues
- **Estimated Effort:** High
- **Dependencies:** 3.4

---

### Phase 4: Admin.razor Refactoring (LOW PRIORITY)
**Goal:** Extract Admin-specific components

#### 4.1 Analyze Admin.razor Structure (Priority: LOW)
- **Action:** Identify sections that can be extracted
- **Estimated Effort:** Low
- **Dependencies:** None

#### 4.2 Extract Admin Components (Priority: LOW)
- **Target:** `Shared/Admin/` folder
- **Estimated Effort:** Medium
- **Dependencies:** 4.1

---

## Recommended Execution Order

### Sprint 1: Complete Incomplete Tasks (1-2 days)
1. Fix Professor.razor pagination (1.1)
2. Integrate ResearchGroupSearchSection (1.2)
3. Extract Professor.razor announcement sections (1.3)

### Sprint 2: Extract Large Company Sections (2-3 days)
1. Company internships section (2.1)
2. Company events section (2.2)
3. Company theses section (2.3)
4. Company announcements management (2.4)

### Sprint 3: Extract Professor & Student Sections (2-3 days)
1. Professor announcements management (2.5)
2. Professor theses management (2.6)
3. Student display sections (2.7)

### Sprint 4: Wire Code-Behind (3-5 days)
1. Analyze dependencies (3.1)
2. Create parameter contracts (3.2)
3. Wire common components (3.3)
4. Wire role-specific components (3.4)
5. Testing and validation (3.5)

### Sprint 5: Admin & Polish (1-2 days)
1. Admin.razor refactoring (4.1, 4.2)
2. Final cleanup and documentation

---

## Success Criteria

### Phase 1 Complete When:
- ✅ All pagination uses Shared.Pagination
- ✅ All created components are integrated
- ✅ No duplicate announcement code

### Phase 2 Complete When:
- ✅ Company.razor < 5000 lines
- ✅ Professor.razor < 3000 lines
- ✅ Student.razor < 5000 lines
- ✅ All major sections extracted to components

### Phase 3 Complete When:
- ✅ All components have proper parameter bindings
- ✅ All functionality works as before
- ✅ No runtime errors
- ✅ All roles functional

---

## Risk Mitigation

### Risk: Component dependencies unclear
**Mitigation:** Create dependency map before wiring (Task 3.1)

### Risk: Large code-behind file hard to navigate
**Mitigation:** Extract components first to reduce complexity

### Risk: Breaking changes during wiring
**Mitigation:** Test incrementally, commit after each role

### Risk: Missing properties/methods
**Mitigation:** Document all component requirements before wiring

---

## Notes

- **File Size Targets:**
  - MainLayout.razor: < 1000 lines (just structure)
  - MainLayout.razor.cs: Keep as-is (33k lines is manageable)
  - Role files: < 5000 lines each
  - Components: < 1000 lines each

- **Component Organization:**
  - Common across roles → `Shared/`
  - Single role only → `Shared/<Role>/`
  - Exactly the same code → Shared component
  - Similar but different → Role-specific component

- **Wiring Strategy:**
  - Start with simplest components (Pagination, LoadingIndicator)
  - Progress to more complex (forms, data displays)
  - Test after each major component group

