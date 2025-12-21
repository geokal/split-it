## 2024-XX-XX Company components migrated to services
- CompanyJobsSection, CompanyInternshipsSection, CompanyThesesSection, CompanyEventsSection, Announcements and search components now depend on ICompanyDashboardService (no direct DbContext usage).
- Service calls cover CRUD/status updates, lookups (areas/regions/skills/professors), attachments, applications/interest flows, and decisions.
- Lookups pulled from CompanyDashboardService; Region/Town view models reused in jobs. Added HasCompanyShownInterestInProfessorEvent helper to track interests client-side.
- Build passes (warnings only: nullable noise, offline NuGet vulnerability check). Remaining roles (Professor/ResearchGroup/Student) still inject DbContext and need future migration.
