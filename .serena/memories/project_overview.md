## 2024-XX-XX Project snapshot
- Company sections fully on ICompanyDashboardService (no direct DbContext).
- QuizViewer pages (1–4) use IDbContextFactory (no injected AppDbContext).
- Student sections (Events, JobsDisplay, ThesisDisplay, CompanySearch, Internships) now use IDbContextFactory<AppDbContext>.
- ResearchGroup Announcements/Events already use IDbContextFactory; full service extraction still pending for ResearchGroup + remaining Student flows.
- Build passes (warnings only: nullable/unused field noise).
- Git staging/commits now unblocked after fixing .git permissions.
