using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QuizManager.Models;

namespace QuizManager.Services.ProfessorDashboard
{
    public interface IProfessorDashboardService
    {
        Task<ProfessorDashboardData> LoadDashboardDataAsync(CancellationToken cancellationToken = default);
        Task RefreshDashboardCacheAsync(CancellationToken cancellationToken = default);
        Task<ProfessorDashboardLookups> GetLookupsAsync(CancellationToken cancellationToken = default);

        // Search operations
        Task<IReadOnlyList<Student>> SearchStudentsAsync(StudentSearchFilter filter, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Company>> SearchCompaniesAsync(CompanySearchFilter filter, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ResearchGroup>> SearchResearchGroupsAsync(ResearchGroupSearchFilter filter, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<string>> SearchAreasAsync(string term, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<string>> SearchSkillsAsync(string term, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CompanyThesis>> SearchCompanyThesesAsync(CompanyThesisSearchFilter filter, CancellationToken cancellationToken = default);

        // CRUD operations for Professor entities
        Task<MutationResult> CreateOrUpdateThesisAsync(ProfessorThesis thesis, CancellationToken cancellationToken = default);
        Task<MutationResult> CreateOrUpdateInternshipAsync(ProfessorInternship internship, CancellationToken cancellationToken = default);
        Task<MutationResult> CreateOrUpdateEventAsync(ProfessorEvent professorEvent, CancellationToken cancellationToken = default);
        Task<MutationResult> CreateOrUpdateAnnouncementAsync(AnnouncementAsProfessor announcement, CancellationToken cancellationToken = default);

        Task<bool> DeleteThesisAsync(int thesisId, CancellationToken cancellationToken = default);
        Task<bool> DeleteInternshipAsync(int internshipId, CancellationToken cancellationToken = default);
        Task<bool> DeleteEventAsync(int eventId, CancellationToken cancellationToken = default);
        Task<bool> DeleteAnnouncementAsync(int announcementId, CancellationToken cancellationToken = default);

        // Status updates
        Task<MutationResult> UpdateThesisStatusAsync(int thesisId, string newStatus, CancellationToken cancellationToken = default);
        Task<MutationResult> UpdateInternshipStatusAsync(int internshipId, string newStatus, CancellationToken cancellationToken = default);
        Task<MutationResult> UpdateEventStatusAsync(int eventId, string newStatus, CancellationToken cancellationToken = default);
        Task<MutationResult> UpdateAnnouncementStatusAsync(int announcementId, string newStatus, CancellationToken cancellationToken = default);

        // Application operations
        Task<IReadOnlyList<ProfessorThesisApplied>> GetThesisApplicationsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProfessorInternshipApplied>> GetInternshipApplicationsAsync(CancellationToken cancellationToken = default);
        Task<MutationResult> DecideOnThesisApplicationAsync(long applicationRng, string studentUniqueId, ApplicationDecision decision, CancellationToken cancellationToken = default);
        Task<MutationResult> DecideOnInternshipApplicationAsync(long applicationRng, string studentUniqueId, ApplicationDecision decision, CancellationToken cancellationToken = default);

        // Event interest operations
        Task<IReadOnlyList<InterestInProfessorEvent>> GetProfessorEventStudentInterestsAsync(long professorEventRng, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<InterestInProfessorEventAsCompany>> GetProfessorEventCompanyInterestsAsync(long professorEventRng, CancellationToken cancellationToken = default);
        Task<MutationResult> ShowInterestInCompanyEventAsProfessorAsync(long eventRng, string professorEmail, CancellationToken cancellationToken = default);
        Task<MutationResult> ShowInterestInCompanyThesisAsProfessorAsync(long thesisRng, string professorEmail, CancellationToken cancellationToken = default);

        // Attachment downloads
        Task<AttachmentDownloadResult?> GetThesisAttachmentAsync(int thesisId, CancellationToken cancellationToken = default);
        Task<AttachmentDownloadResult?> GetInternshipAttachmentAsync(int internshipId, CancellationToken cancellationToken = default);
        Task<AttachmentDownloadResult?> GetEventAttachmentAsync(int eventId, CancellationToken cancellationToken = default);

        // Entity lookups
        Task<Professor?> GetProfessorProfileAsync(CancellationToken cancellationToken = default);
        Task<Student?> GetStudentByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<Student?> GetStudentByUniqueIdAsync(string studentUniqueId, CancellationToken cancellationToken = default);
        Task<Company?> GetCompanyByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<Company?> GetCompanyByUniqueIdAsync(string companyUniqueId, CancellationToken cancellationToken = default);
        Task<ResearchGroupDetails?> GetResearchGroupDetailsAsync(string researchGroupEmail, CancellationToken cancellationToken = default);

        // Calendar operations
        Task<(int companyEvents, int professorEvents)> CountPublishedEventsOnDateAsync(DateTime date, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CompanyEvent>> GetCompanyEventsForMonthAsync(int year, int month, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProfessorEvent>> GetProfessorEventsForMonthAsync(int year, int month, CancellationToken cancellationToken = default);
    }
}

