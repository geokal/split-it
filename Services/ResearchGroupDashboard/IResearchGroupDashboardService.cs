using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QuizManager.Models;

namespace QuizManager.Services.ResearchGroupDashboard
{
    public interface IResearchGroupDashboardService
    {
        Task<ResearchGroupDashboardData> LoadDashboardDataAsync(CancellationToken cancellationToken = default);
        Task RefreshDashboardCacheAsync(CancellationToken cancellationToken = default);
        Task<ResearchGroupDashboardLookups> GetLookupsAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CompanyEvent>> GetPublishedCompanyEventsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProfessorEvent>> GetPublishedProfessorEventsAsync(CancellationToken cancellationToken = default);
        Task<HashSet<long>> GetCompanyEventInterestsForProfessorAsync(string professorEmail, CancellationToken cancellationToken = default);

        Task<MutationResult> CreateOrUpdateAnnouncementAsync(ResearchGroupAnnouncementRequest request, CancellationToken cancellationToken = default);
        Task<MutationResult> DeleteAnnouncementAsync(long announcementId, CancellationToken cancellationToken = default);
        Task<AttachmentDownloadResult?> GetAnnouncementAttachmentAsync(long announcementId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Area>> SearchAreasAsync(string query, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Skill>> SearchSkillsAsync(string query, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Company>> SearchCompaniesAsync(string query, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Professor>> SearchProfessorsAsync(string query, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Company>> FilterCompaniesAsync(ResearchGroupCompanySearchRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Professor>> FilterProfessorsAsync(ResearchGroupProfessorSearchRequest request, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AnnouncementAsCompany>> GetPublishedCompanyAnnouncementsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AnnouncementAsProfessor>> GetPublishedProfessorAnnouncementsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AnnouncementAsResearchGroup>> GetPublishedResearchGroupAnnouncementsAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AnnouncementAsResearchGroup>> GetUploadedResearchGroupAnnouncementsAsync(string researchGroupEmail, CancellationToken cancellationToken = default);

        Task<AnnouncementAsResearchGroup?> CreateAnnouncementAsync(AnnouncementAsResearchGroup announcement, CancellationToken cancellationToken = default);
        Task<bool> UpdateAnnouncementAsync(int announcementId, AnnouncementAsResearchGroup updates, CancellationToken cancellationToken = default);
        Task<bool> DeleteAnnouncementAsync(int announcementId, CancellationToken cancellationToken = default);

        Task<bool> BulkCopyAnnouncementsAsync(IEnumerable<int> announcementIds, string researchGroupEmail, CancellationToken cancellationToken = default);
        Task<int> BulkUpdateStatusAsync(IEnumerable<int> announcementIds, string newStatus, CancellationToken cancellationToken = default);
    }
}
