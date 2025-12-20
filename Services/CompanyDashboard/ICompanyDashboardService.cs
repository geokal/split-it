using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QuizManager.Models;

namespace QuizManager.Services.CompanyDashboard
{
    public interface ICompanyDashboardService
    {
        Task<CompanyDashboardData> LoadDashboardDataAsync(CancellationToken cancellationToken = default);
        Task RefreshDashboardCacheAsync(CancellationToken cancellationToken = default);
        Task<CompanyDashboardLookups> GetLookupsAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Professor>> SearchProfessorsAsync(ProfessorSearchFilter filter, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ResearchGroup>> SearchResearchGroupsAsync(ResearchGroupSearchFilter filter, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<string>> SearchAreasAsync(string term, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<string>> SearchSkillsAsync(string term, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<string>> SearchCompanyNamesAsync(string term, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Student>> SearchStudentsAsync(StudentSearchFilter filter, CancellationToken cancellationToken = default);

        Task<MutationResult> CreateOrUpdateJobAsync(CompanyJob job, CancellationToken cancellationToken = default);
        Task<MutationResult> CreateOrUpdateInternshipAsync(CompanyInternship internship, CancellationToken cancellationToken = default);
        Task<MutationResult> CreateOrUpdateThesisAsync(CompanyThesis thesis, CancellationToken cancellationToken = default);
        Task<MutationResult> CreateOrUpdateAnnouncementAsync(AnnouncementAsCompany announcement, CancellationToken cancellationToken = default);
        Task<MutationResult> CreateOrUpdateCompanyEventAsync(CompanyEvent companyEvent, CancellationToken cancellationToken = default);

        Task<bool> DeleteJobAsync(int jobId, CancellationToken cancellationToken = default);
        Task<bool> DeleteInternshipAsync(int internshipId, CancellationToken cancellationToken = default);
        Task<bool> DeleteThesisAsync(int thesisId, CancellationToken cancellationToken = default);
        Task<bool> DeleteAnnouncementAsync(int announcementId, CancellationToken cancellationToken = default);
        Task<bool> DeleteCompanyEventAsync(int eventId, CancellationToken cancellationToken = default);
        Task<bool> DeleteThesisApplicationAsync(long thesisApplicationRng, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CompanyJobApplied>> GetJobApplicationsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<InternshipApplied>> GetInternshipApplicationsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CompanyThesisApplied>> GetThesisApplicationsAsync(CancellationToken cancellationToken = default);
        Task<MutationResult> DecideOnApplicationAsync(ApplicationDecisionRequest request, CancellationToken cancellationToken = default);

        Task<AttachmentDownloadResult?> GetJobAttachmentAsync(int jobId, CancellationToken cancellationToken = default);
        Task<AttachmentDownloadResult?> GetInternshipAttachmentAsync(int internshipId, CancellationToken cancellationToken = default);
        Task<AttachmentDownloadResult?> GetThesisAttachmentAsync(int thesisId, CancellationToken cancellationToken = default);

        Task<Company?> GetCompanyProfileAsync(CancellationToken cancellationToken = default);
        Task<Company?> GetCompanyDetailsForModalAsync(string companyEmail, CancellationToken cancellationToken = default);
        Task<Company?> GetCompanyByUniqueIdAsync(string companyUniqueId, CancellationToken cancellationToken = default);
        Task<Company?> GetCompanyByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<Student?> GetStudentByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<Student?> GetStudentByUniqueIdAsync(string studentUniqueId, CancellationToken cancellationToken = default);
        Task<Student?> GetStudentByIdAsync(int studentId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Student>> GetAllStudentsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CompanyThesis>> SearchCompanyThesesAsync(CompanyThesisSearchFilter filter, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProfessorThesis>> SearchProfessorThesesAsync(ProfessorThesisSearchFilter filter, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Professor>> GetProfessorsByEmailsAsync(IEnumerable<string> emails, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CompanyThesis>> GetCompanyThesisProfessorInterestsAsync(long thesisRng, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CompanyEventInterestDetails>> GetCompanyEventInterestsAsync(long eventRng, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<InterestInCompanyEventAsProfessor>> GetProfessorCompanyEventInterestsAsync(long eventRng, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<InterestInProfessorEvent>> GetProfessorEventStudentInterestsAsync(long professorEventRng, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<InterestInProfessorEventAsCompany>> GetProfessorEventCompanyInterestsAsync(long professorEventRng, CancellationToken cancellationToken = default);

        Task<ApplicantDetailsResult> LoadApplicantDetailsAsync(ApplicantDetailsRequest request, CancellationToken cancellationToken = default);
        Task<MutationResult> ShowInterestInCompanyEventAsync(long eventRng, string studentEmail, bool needsTransport, string? transportLocation, CancellationToken cancellationToken = default);
        Task<MutationResult> ShowInterestInCompanyEventAsProfessorAsync(long eventRng, string professorEmail, CancellationToken cancellationToken = default);
        Task<MutationResult> ShowInterestInProfessorEventAsCompanyAsync(long professorEventRng, int attendeeCount, CancellationToken cancellationToken = default);
        Task<MutationResult> ShowInterestInCompanyThesisAsProfessorAsync(long thesisRng, string professorEmail, CancellationToken cancellationToken = default);
        Task<MutationResult> ShowInterestInProfessorThesisAsync(long professorThesisRng, CancellationToken cancellationToken = default);
        Task<(int companyEvents, int professorEvents)> CountPublishedEventsOnDateAsync(DateTime date, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CompanyEvent>> GetCompanyEventsForMonthAsync(int year, int month, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProfessorEvent>> GetProfessorEventsForMonthAsync(int year, int month, CancellationToken cancellationToken = default);

        Task<Professor?> GetProfessorByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<ResearchGroupDetails?> GetResearchGroupDetailsAsync(string researchGroupEmail, CancellationToken cancellationToken = default);
        Task<CompanyJob?> GetJobByRngAsync(long rngForPositionUploaded, CancellationToken cancellationToken = default);
        Task<AttachmentDownloadResult?> GetProfessorThesisAttachmentAsync(int professorThesisId, CancellationToken cancellationToken = default);

        Task<MutationResult> UpdateJobStatusAsync(int jobId, string newStatus, CancellationToken cancellationToken = default);
        Task<MutationResult> UpdateInternshipStatusAsync(int internshipId, string newStatus, CancellationToken cancellationToken = default);
        Task<MutationResult> UpdateThesisStatusAsync(int thesisId, string newStatus, CancellationToken cancellationToken = default);
        Task<MutationResult> UpdateAnnouncementStatusAsync(int announcementId, string newStatus, CancellationToken cancellationToken = default);
        Task<MutationResult> UpdateCompanyEventStatusAsync(int companyEventId, string newStatus, CancellationToken cancellationToken = default);
    }
}

