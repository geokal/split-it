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

        Task<MutationResult> CreateOrUpdateAnnouncementAsync(ResearchGroupAnnouncementRequest request, CancellationToken cancellationToken = default);
        Task<MutationResult> DeleteAnnouncementAsync(long announcementId, CancellationToken cancellationToken = default);
        Task<AttachmentDownloadResult?> GetAnnouncementAttachmentAsync(long announcementId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Area>> SearchAreasAsync(string query, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Skill>> SearchSkillsAsync(string query, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Company>> SearchCompaniesAsync(string query, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Professor>> SearchProfessorsAsync(string query, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Company>> FilterCompaniesAsync(ResearchGroupCompanySearchRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Professor>> FilterProfessorsAsync(ResearchGroupProfessorSearchRequest request, CancellationToken cancellationToken = default);
    }
}

