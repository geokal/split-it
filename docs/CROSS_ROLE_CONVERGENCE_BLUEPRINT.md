# Cross-Role Convergence Blueprint (Read-only)

- Scope: identify cross-role duplication and define shared wrapper contracts without merging roles or changing behavior.
- Constraints:
  - No authorization logic inside shared wrappers
  - No role branching inside shared wrappers
  - All role differences must be parameterized and/or passed as `RenderFragment` slots

## Step 1: Build a role component inventory

### Student (`Components/Layout/StudentSections/StudentSection.razor`)
- Top-level layout structure
  - Role gate via `UserRole == "Student"` plus `HideAllInitialCards()`
  - 3-stage flow: not-initialized → loading UI; not-registered → registration prompt; else → Bootstrap tabs
  - Tabs embed: `StudentAnnouncementsSection`, `StudentEventsSection`, `StudentThesisDisplaySection`, `StudentJobsDisplaySection`, `StudentInternshipsSection`, `StudentCompanySearchSection`
- Repeating UI blocks
  - “professional-loading” spinner block
  - Registration prompt card + warning paragraph
  - Tab nav + tab-pane scaffolding
  - Collapsible section headers with plus/minus and `@onclick:stopPropagation`
  - Manual pagination blocks + page size selectors (per section)
  - Modal patterns (Bootstrap / conditional `show d-block`)
- State variables (dominant patterns in subcomponents)
  - Visibility flags: `is…Visible` toggles per section/subsection
  - Lists: announcements/events/results collections
  - Pagination: `currentPage…`, `pageSize`, `totalPages…`
  - Expand/collapse: `expanded…Id` (single expanded item)
  - Search: many `search…` strings + `…Suggestions` lists + selected chip sets
  - Modal state: `show…Modal` + selected entity
- Injected services (dominant patterns in subcomponents)
  - Frequent: `AppDbContext`, `IJSRuntime`, `AuthenticationStateProvider`, `NavigationManager`, `HttpClient`, `InternshipEmailService`
- Event handlers (dominant patterns)
  - `Toggle…Visibility`, `ToggleDescription(...)`
  - `Handle…Input(...)`, `Select…Suggestion(...)`, add/remove selected chips
  - `Search…()`, `Clear…()`
  - `GoToFirst/Previous/Next/GoToLast`, `OnPageSizeChange…`
  - `Show…Modal` / `Close…Modal`
- Conditional flags
  - Role and registration/init flags from cascading parameters
  - Published-status filters inside lists (e.g., “Δημοσιευμένη”)
  - Modal visibility + selected entity null checks

### Company (`Components/Layout/CompanySections/CompanySection.razor`)
- Top-level layout structure
  - Role gate via `UserRole == "Company"`
  - 3-stage flow: not-initialized → loading UI; not-registered → registration prompt; else → Bootstrap tabs
  - Tabs embed: `CompanyAnnouncementsManagementSection`, `CompanyAnnouncementsSection`, `CompanyEventsSection`, `CompanyJobsSection`, `CompanyInternshipsSection`, `CompanyThesesSection`, `CompanyStudentSearchSection`, `CompanyProfessorSearchSection`, `CompanyResearchGroupSearchSection`
- Repeating UI blocks
  - Same loading + registration prompt patterns as Student
  - Tab scaffolding and repeated collapsible section headers inside tabs
  - Manual pagination + expandable descriptions + bulk-action UI (notably in management sections)
  - Modal confirmations (delete/bulk/email confirmation)
- State variables (dominant patterns in subcomponents)
  - Visibility flags, pagination fields, `expanded…Id`, selected IDs for bulk actions, modal booleans, “loading modal” booleans + progress
- Injected services (dominant patterns in subcomponents)
  - Frequent: `ICompanyDashboardService`, `IJSRuntime`, `AuthenticationStateProvider`, `NavigationManager`, `HttpClient`, `InternshipEmailService`
- Event handlers (dominant patterns)
  - Same UI toggles/pagination as Student, plus CRUD flows (create/edit/delete/bulk/email-confirm)
- Conditional flags
  - Role gate + registration/init flags
  - Per-item status-based UI (published/active states), bulk selection state

### Professor (`Components/Layout/ProfessorSections/ProfessorSection.razor`)
- Top-level layout structure
  - Role gate is embedded as `else if (UserRole == "Professor")` (same 3-stage flow as other roles)
  - Tabs embed: `ProfessorAnnouncementsManagementSection`, `ProfessorEventsSection`, `ProfessorThesesSection`, `ProfessorInternshipsSection`, `ProfessorStudentSearchSection`, `ProfessorCompanySearchSection`, `ProfessorResearchGroupSearchSection`
- Repeating UI blocks
  - Same loading + registration prompt patterns
  - Tab scaffolding
  - High duplication of “Recent announcements” + news panels + expandable list + pagination inside announcements/events management
- State variables (dominant patterns in subcomponents)
  - Heavy: many visibility flags, multiple paginated lists (announcements/events), expanded IDs per list, selection sets, modals, loading/progress flags
- Injected services (dominant patterns in subcomponents)
  - Frequent: `IProfessorDashboardService`, `IFrontPageService`, `IJSRuntime`, `AuthenticationStateProvider`, `NavigationManager`, `HttpClient`
- Event handlers (dominant patterns)
  - Toggle visibility, pagination, expand/collapse, CRUD/bulk operations, modal show/hide
- Conditional flags
  - Role/init/registration gates
  - Status-based filtering and visibility (published vs non-published, etc.)

### Research Group (`Components/Layout/ResearchGroupSections/ResearchGroupSection.razor`)
- Top-level layout structure
  - Role gate via `UserRole == "Research Group"`
  - 3-stage flow: not-initialized → loading UI; not-registered → registration prompt; else → Bootstrap tabs
  - Tabs embed: `ResearchGroupAnnouncementsSection`, `ResearchGroupEventsSection`, `ResearchGroupCompanySearchSection`, `ResearchGroupProfessorSearchSection`, `ResearchGroupStatisticsSection`
- Repeating UI blocks
  - Same loading + registration prompt patterns
  - Tab scaffolding
  - Collapsible panels, manual pagination, expandable descriptions, modals
- State variables (dominant patterns in subcomponents)
  - Similar to Company/Professor for announcements feeds: multiple lists + multiple pagers + expanded IDs
  - Search sections: typeahead + chip lists + paginated results + details modal
- Injected services (dominant patterns in subcomponents)
  - Mixed: `IResearchGroupDashboardService` (search/stats), `IDbContextFactory<AppDbContext>` (announcements/events), plus `IJSRuntime`, `AuthenticationStateProvider`, `HttpClient`
- Event handlers (dominant patterns)
  - Toggle visibility, pagination, expand/collapse, search + suggestion selection, modal show/hide
- Conditional flags
  - Role/init/registration gates
  - Status-based filters (published), visibility toggles

### Admin (`Components/Layout/AdminSections/AdminSection.razor`, `Components/Layout/AdminSections/AdminSection.razor.cs`)
- Top-level layout structure
  - Gate via `ShouldShowAdminTable()` (auth-derived role + route check)
  - Bootstrap tabs: Students / Companies / Professors
  - Within tabs: collapsible header blocks, large tables, analytics toggles
- Repeating UI blocks
  - Collapsible header + plus/minus
  - Table layouts
- State variables
  - `UserRole`, visibility toggles, table datasets, analytics dictionaries, filter selections
- Injected services
  - `IDbContextFactory<AppDbContext>`, `IJSRuntime`, `AuthenticationStateProvider`, `NavigationManager`
- Event handlers
  - Role load, toggle visibility, analytics load, filtering
- Conditional flags
  - Role check and route-based suppression (`/profile`)

## Step 2: Identify convergent patterns across roles

### Convergence Group 1: Initialization + Registration Gate (role section entry)
- Involved roles
  - Student, Company, Professor, Research Group
- Shared responsibility
  - Gate role content behind “initialized” and “registered” readiness checks
- Invariant behavior
  - If not initialized: render the same loading UI
  - Else if not registered: render the same “registration required” prompt
  - Else: render the role’s tabbed content
- Variable behavior
  - Registration title text + registration URL
  - Student-only extra gate (`HideAllInitialCards`) and surrounding layout container
  - Professor section’s surrounding `else if` placement (structural, not behavioral)
- Classification: SAFE
- Justification (heuristics)
  - A: identical 3-stage flow; no role-specific branching needed inside wrapper if booleans are passed in
  - B: wrapper can be stateless; all state comes from parameters
  - D: wrapper does not inspect `UserRole`; caller enforces role and passes flags
  - E: differences are labels/URLs only
- Extraction recommendation
  - Extract now

### Convergence Group 2: Collapsible Section Header/Panel (plus/minus toggle + stopPropagation)
- Involved roles
  - All roles (Student/Company/Professor/Research Group/Admin), across many “sections within tabs”
- Shared responsibility
  - Provide a consistent collapsible panel chrome (header click toggles a section; plus/minus indicator; optional iconography)
- Invariant behavior
  - Header toggles a boolean visibility flag
  - Button uses `@onclick:stopPropagation` to avoid double toggles
  - Content renders only when visible
- Variable behavior
  - Labels, icons, CSS classes, optional “toggle on header vs toggle on button only”
  - Content body is role/feature-specific
- Classification: SAFE
- Justification (heuristics)
  - A: same UI flow and event sequence (toggle boolean → rerender)
  - B: wrapper can be stateless with `IsExpanded` + change callback
  - D: no auth or role checks required
  - E: variability is exactly labels/icons/CSS/content slots
  - F: future visual changes should apply to all roles equally
- Extraction recommendation
  - Extract now

### Convergence Group 3: Expandable + Paginated Feed Lists (announcements/events-style lists)
- Involved roles
  - Student (`StudentAnnouncementsSection`)
  - Company (`CompanyAnnouncementsManagementSection`, partially `CompanyAnnouncementsSection`)
  - Professor (`ProfessorAnnouncementsManagementSection`)
  - Research Group (`ResearchGroupAnnouncementsSection`)
- Shared responsibility
  - Display a list of items with expandable “details/description”, sorted and paginated, with a common empty-state pattern
- Invariant behavior
  - Filter to a “published” subset, sort descending by date, `Skip/Take` by page, click-to-expand a single item, show pagination controls
- Variable behavior
  - Item types and fields (Company/Professor/ResearchGroup announcements; Professor also mixes in event lists)
  - Per-role actions: selection, bulk operations, edit/delete flows, loading modals
  - Different pagination state variables per list (multiple independent pagers in same component)
- Classification: CONDITIONAL
- Justification (heuristics)
  - A: the read-only list behavior is invariant, but management variants introduce different side effects on click/actions
  - B: wrapper can safely own UI-only state (expanded ID/current page) only if kept strictly UI; action state is role-specific
  - C: services differ by role; wrapper must not inject or decide which service to call
  - E: variability exceeds labels/columns when CRUD/bulk actions are included
  - F: likely to diverge by role over time for management tooling
- Extraction recommendation
  - Extract later (after carving out a purely read-only “feed list” slice and pushing actions into slots/callbacks)

#### Similar-looking sections that should remain separate (not convergent at “whole section” level)
- Cross-role Search sections (Company/Professor/Research Group/Student search UIs)
  - They share typeahead/chip/pagination UI primitives, but differ in service calls, filtering semantics, result shapes, and modal detail behavior
  - Treat as convergence only at the UI-primitive level (Group 2), not as a single shared “SearchSection” wrapper

## Step 3: Define shared wrapper contracts (no code)

### Wrapper for Group 1: “RoleContentGate”
- Responsibility (one sentence)
  - Render loading/registration gating UI and then render child content once ready
- Required parameters
  - `IsInitialized` (bool)
  - `IsRegistered` (bool)
  - `RegistrationUrl` (string)
  - `RegistrationTitle` (string)
  - `RegisteredContent` (RenderFragment)
- Optional parameters
  - `IsHidden` (bool) or a precomputed “should render” flag (to keep `HideAllInitialCards` outside)
  - Custom loading title/subtitle strings
  - Custom unregistered warning text
  - `LoadingContent` and `UnregisteredContent` slots (override defaults)
- RenderFragment slots
  - `RegisteredContent` (main slot)
  - Optional `LoadingContent`
  - Optional `UnregisteredContent`
- Events exposed to parent
  - None required (navigation can remain a normal link), optional “OnRegisterClick” if you want analytics
- State ownership
  - Wrapper: none
  - Role component: role checks + flag computation + whether to hide content

### Wrapper for Group 2: “CollapsiblePanel”
- Responsibility (one sentence)
  - Provide a consistent collapsible header and render body content when expanded
- Required parameters
  - `Title` (string)
  - `IsExpanded` (bool)
  - `IsExpandedChanged` (callback/event)
  - `ChildContent` (RenderFragment)
- Optional parameters
  - `IconClass` / `SecondaryIconClass` (string)
  - Header/body CSS class overrides
  - `ToggleOnHeaderClick` (bool)
  - `ContentId` for aria-controls (string)
- RenderFragment slots
  - Optional `HeaderContent` (for complex headers)
  - `ChildContent` (body)
- Events exposed to parent
  - `OnToggle` (optional, if you want separate from `IsExpandedChanged`)
- State ownership
  - Wrapper: none (recommended); or wrapper-owned internal bool only if you accept reduced external control
  - Role component: owns the boolean state and any “load-on-open” behavior

### Wrapper for Group 3: “ExpandablePaginatedFeed”
- Responsibility (one sentence)
  - Render a paginated list of items with a single expanded item and standardized pagination UI
- Required parameters
  - `Items` (already filtered/sorted list to keep wrapper service-agnostic)
  - `PageSize` and `PageSizeOptions`
  - `ItemKey` selector (stable ID)
  - `ItemSummary` (RenderFragment<TItem>)
  - `ItemDetails` (RenderFragment<TItem>)
- Optional parameters
  - Empty-state message/title
  - `InitiallyExpandedKey` (optional)
  - “Show page X of Y” flag
  - “Allow multi-expand” flag (only if needed; default single-expand)
- RenderFragment slots
  - `ItemSummary`, `ItemDetails`
  - Optional `ItemActions` (render-only; actions stay in parent via callbacks)
- Events exposed to parent
  - Optional `OnExpandedItemChanged`
  - Optional `OnPageChanged` / `OnPageSizeChanged` (if parent wants to persist state)
- State owned by wrapper vs role
  - Wrapper: UI-only state (current page, expanded key) for the read-only feed slice
  - Role: any domain state (selected IDs, bulk action state, delete/edit modals, validation, service calls)

## Step 4: Define extraction boundaries

### RoleContentGate
- Must remain inside role components
  - `UserRole == …` checks and any “admin/profile-route” rules
  - Computation of `IsInitialized` / `IsRegistered` and `HideAllInitialCards`
- Must move to wrapper
  - Loading UI markup and registration prompt markup
- Must never be shared
  - Any logic that derives role from claims/auth state
- Risks if extracted incorrectly
  - Accidentally reintroducing role/claim inspection inside the wrapper (violates hard constraint)

### CollapsiblePanel
- Must remain inside role components
  - Any “open triggers load” logic (e.g., on first expand, fetch data)
  - Permission decisions for whether the panel should exist at all
- Must move to wrapper
  - Header rendering, plus/minus indicator, consistent click/stopPropagation behavior
- Must never be shared
  - Authorization/business rules encoded as visibility logic inside wrapper
- Risks if extracted incorrectly
  - Wrapper starts owning role-specific state or embeds “if role then …” branches

### ExpandablePaginatedFeed
- Must remain inside role components
  - Data fetching, filtering rules, and ordering semantics (or pass preprocessed list)
  - All CRUD/bulk-action logic, modal workflows, and side effects
- Must move to wrapper
  - Expand/collapse UI pattern and pagination UI pattern for read-only feeds
- Must never be shared
  - Service injection, authorization checks, or status transitions (publish/unpublish, delete) inside wrapper
- Risks if extracted incorrectly
  - A click in one role triggering different service calls than another (behavioral divergence)
  - Wrapper accidentally becoming the “source of truth” for role-specific mutable state (selection sets, draft forms)

## Step 5: Propose a migration order

- 1) Lowest risk: Group 1 “RoleContentGate”
  - Pure UI duplication; no service calls; no auth logic required inside wrapper
- 2) Medium risk: Group 2 “CollapsiblePanel”
  - Still UI-only, but widely used; requires careful replacement to avoid breaking click behavior
- 3) Highest risk: Group 3 “ExpandablePaginatedFeed”
  - Touches many big components; needs strict boundary enforcement to keep actions and auth out of the wrapper

## Step 6: Validate the design

- Lines removable per role component (based on current duplicated gate blocks; exact counts from file line ranges)
  - Student (`Components/Layout/StudentSections/StudentSection.razor`): ~40 lines (loading + registration block around lines 24–63)
  - Company (`Components/Layout/CompanySections/CompanySection.razor`): ~35 lines (lines 18–52)
  - Professor (`Components/Layout/ProfessorSections/ProfessorSection.razor`): ~39 lines (lines 17–55 within the Professor branch)
  - Research Group (`Components/Layout/ResearchGroupSections/ResearchGroupSection.razor`): ~40 lines (lines 16–55)
- Shared wrappers that reduce the most duplication
  - “ExpandablePaginatedFeed” (Group 3) has the largest payoff because the same expand/collapse + pager pattern appears multiple times per announcements management component (company/professor/researchgroup + student read-only)
- Shared wrappers that most improve long-term maintainability
  - “CollapsiblePanel” (Group 2): standardizes interaction behavior across the app and reduces future UI drift
  - “RoleContentGate” (Group 1): keeps all role entry gating consistent and easy to audit

## Final validation step (SAFE/CONDITIONAL/UNSAFE summary)

- SAFE wrappers extractable immediately
  - 2 (Group 1 “RoleContentGate”, Group 2 “CollapsiblePanel”)
- CONDITIONAL wrappers needing pre-work
  - 1 (Group 3 “ExpandablePaginatedFeed”: only after isolating the read-only feed slice and pushing actions/modals into slots/callbacks)
- UNSAFE areas that must stay duplicated (at whole-section level)
  - End-to-end “SearchSection” wrappers across roles (service calls + semantics differ); converge only on UI primitives (typeahead/chips/pager), not on full behavior

