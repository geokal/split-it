# Refactoring Plan: Split-It Project

## 🎯 Project End Goal

**Transform the monolithic Blazor application into a modular architecture with minimal MainLayout files.**

### Target State

**MainLayout.razor** → Minimal file (~100-200 lines) containing only:
- Basic layout structure
- Role-based component routing: `<Student />`, `<Company />`, `<Professor />`, `<Admin />`, `<ResearchGroup />`

**MainLayout.razor.cs** → Minimal file (~500-1000 lines) containing only:
- Layout-specific logic
- UserRole management
- Basic initialization
- **NO role-specific business logic** (logic should be in role component code-behinds or services)

**Role Components** (Student.razor, Company.razor, etc.) → Use subcomponents:
- `Company.razor` uses `<CompanyJobsSection />`, `<CompanyAnnouncementsSection />`, etc.
- `Student.razor` uses `<StudentCompanySearchSection />`, `<StudentThesisDisplaySection />`, etc.

**Subcomponents** → Located in `Shared/[Role]/` folders:
- Receive data via `[Parameter]` declarations
- Business logic wired from MainLayout.razor.cs (current phase) or role-specific services (future phase)

**Shared Components** → Located in `Shared/` folder (root level):
- Reusable across ALL user roles (e.g., `Pagination`, `LoadingIndicator`, `NewsSection`)
- No role-specific logic - truly shared functionality
- Can be used by Company, Student, Professor, Admin, ResearchGroup components

**Service Layer** (Future Phase):
- **Centralized Database Calls**: All DbContext operations go through service classes
- **Role-Specific Services**: `CompanyService`, `StudentService`, `ProfessorService` (handle role-specific business logic)
- **Shared Services**: `AnnouncementService`, `ThesisService`, `JobService` (handle shared business logic)
- Services eliminate direct DbContext access from components and MainLayout.razor.cs

This modular architecture makes the application:
- ✅ **Maintainable**: Easy to find and modify code
- ✅ **Scalable**: Easy to add new features without touching MainLayout
- ✅ **Testable**: Components and services can be tested in isolation
- ✅ **Standard**: Follows Blazor Server best practices
- ✅ **Centralized**: All database calls go through services (not scattered)
- ✅ **Reusable**: Components and services can be shared across roles

---

## Current Status Summary

### ✅ Completed
- Role-specific files extracted (Student.razor, Company.razor, Professor.razor, Admin.razor, ResearchGroup.razor)
- Common components extracted (Pagination, NewsSection, LoadingIndicator, RegistrationPrompt)
- **All components extracted** (28 components total across all roles) ✅
- Files renamed (MainLayout.razor, MainLayout.razor.cs)
- **Pattern 2 Architecture Migration**:
  - ✅ All Professor components (7/7) converted to Pattern 2 (code-behind with services)
  - ✅ All Student components (6/6) converted to Pattern 2 (code-behind with services)

### ⏳ In Progress
- Converting Company components (0/9) to Pattern 2
- Converting ResearchGroup components (0/5) to Pattern 2
- Converting Admin components (0/1) to Pattern 2

### ❌ Not Started
- Extract remaining business logic from MainLayout.razor.cs into service classes (future phase)

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

#### 2.1 Company.razor - Components Extraction (Priority: HIGH) ✅ COMPLETED
- **Status:** ✅ All Company components extracted
- **Location:** Lines 75-12391 (entire Company.razor)
- **Sections Extracted:**
  - `CompanyAnnouncementsManagementSection.razor` (638 lines) - CREATE announcements tab
  - `CompanyAnnouncementsSection.razor` (609 lines) - VIEW uploaded announcements
  - `CompanyJobsSection.razor` (1,658 lines) - CREATE and VIEW jobs
  - `CompanyInternshipsSection.razor` (1,978 lines) - CREATE and VIEW internships
  - `CompanyThesesSection.razor` (2,914 lines) - CREATE and VIEW theses
  - `CompanyEventsSection.razor` (2,602 lines) - VIEW events
  - `CompanyStudentSearchSection.razor` (671 lines) - Student search
  - `CompanyProfessorSearchSection.razor` (528 lines) - Professor search
  - `CompanyResearchGroupSearchSection.razor` (692 lines) - Research group search
- **Target:** `Shared/Company/` (9 components, 12,290 lines total)
- **Status:** ✅ Completed (2025-12-16)
- **Impact:** Company.razor ready for component integration

#### 2.5 Professor.razor - Components Extraction (Priority: HIGH) ✅ COMPLETED
- **Status:** ✅ All Professor components extracted
- **Location:** Lines 75-11245 (entire Professor.razor)
- **Sections Extracted:**
  - `ProfessorAnnouncementsManagementSection.razor` (854 lines) - CREATE announcements tab
  - `ProfessorThesesSection.razor` (1,755 lines) - VIEW uploaded theses
  - `ProfessorInternshipsSection.razor` (1,841 lines) - Internships management
  - `ProfessorEventsSection.razor` (2,868 lines) - Events calendar and management
  - `ProfessorStudentSearchSection.razor` (623 lines) - Student search
  - `ProfessorCompanySearchSection.razor` (385 lines) - Company search
  - `ProfessorResearchGroupSearchSection.razor` (2,356 lines) - Research group search
- **Target:** `Shared/Professor/` (7 components, 10,682 lines total)
- **Status:** ✅ Completed (2025-12-16)
- **Impact:** Professor.razor ready for component integration

#### 2.7 Student.razor - Components Extraction (Priority: HIGH) ✅ COMPLETED
- **Status:** ✅ All Student components extracted
- **Location:** Lines 80-10009 (entire Student.razor content)
- **Sections Extracted:**
  - `StudentCompanySearchSection.razor` (680 lines) - Company search
  - `StudentAnnouncementsSection.razor` (513 lines) - Announcements display
  - `StudentThesisDisplaySection.razor` (2,372 lines) - DISPLAY thesis positions
  - `StudentJobsDisplaySection.razor` (1,614 lines) - DISPLAY job positions
  - `StudentInternshipsSection.razor` (2,423 lines) - Internships (includes applied internships)
  - `StudentEventsSection.razor` (2,328 lines) - Events calendar
- **Target:** `Shared/Student/` (6 components, 8,930 lines total)
- **Status:** ✅ Completed (2025-12-16)
- **Impact:** Student.razor ready for component integration

#### 2.8 ResearchGroup.razor - Components Extraction (Priority: HIGH) ✅ COMPLETED
- **Status:** ✅ All ResearchGroup components extracted
- **Location:** Lines 68-3879 (entire ResearchGroup.razor content)
- **Sections Extracted:**
  - `ResearchGroupAnnouncementsSection.razor` (525 lines) - Announcements display and management
  - `ResearchGroupThesesSection.razor` (495 lines) - Theses management (nested in announcements)
  - `ResearchGroupJobsSection.razor` (500 lines) - Jobs management (nested in announcements)
  - `ResearchGroupInternshipsSection.razor` (500 lines) - Internships management (nested in announcements)
  - `ResearchGroupEventsSection.razor` (500 lines) - Events management
  - `ResearchGroupSearchSection.razor` (1,286 lines) - Company and Professor search
- **Target:** `Shared/ResearchGroup/` (6 components, 3,806 lines total)
- **Status:** ✅ Completed (2025-12-16)
- **Impact:** ResearchGroup.razor ready for component integration

#### 2.9 Admin.razor - Components Extraction (Priority: LOW) ✅ COMPLETED
- **Status:** ✅ Admin component extracted
- **Location:** Lines 1-183 (entire Admin.razor content)
- **Sections Extracted:**
  - `AdminSection.razor` (183 lines) - Complete admin interface
- **Target:** `Shared/Admin/` (1 component, 183 lines total)
- **Status:** ✅ Completed (2025-12-16)
- **Note:** Admin.razor is small enough that it doesn't require further splitting into multiple components
- **Impact:** Admin.razor ready for component integration

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

