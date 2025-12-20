using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuizManager.Data;
using QuizManager.Models;

namespace QuizManager.Services.FrontPage
{
    public class FrontPageService : IFrontPageService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<FrontPageService> _logger;

        public FrontPageService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            ILogger<FrontPageService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<IReadOnlyList<CompanyEvent>> GetPublishedCompanyEventsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

                return await context.CompanyEvents
                    .Where(e => e.CompanyEventStatus == "Δημοσιευμένη")
                    .AsNoTracking()
                    .OrderByDescending(e => e.CompanyEventActiveDate)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading published company events");
                return Array.Empty<CompanyEvent>();
            }
        }

        public async Task<IReadOnlyList<ProfessorEvent>> GetPublishedProfessorEventsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

                return await context.ProfessorEvents
                    .Where(e => e.ProfessorEventStatus == "Δημοσιευμένη")
                    .AsNoTracking()
                    .OrderByDescending(e => e.ProfessorEventActiveDate)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading published professor events");
                return Array.Empty<ProfessorEvent>();
            }
        }

        public async Task<FrontPageData> LoadFrontPageDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

                var companyEventsTask = context.CompanyEvents
                    .Where(e => e.CompanyEventStatus == "Δημοσιευμένη")
                    .AsNoTracking()
                    .OrderByDescending(e => e.CompanyEventActiveDate)
                    .ToListAsync(cancellationToken);

                var professorEventsTask = context.ProfessorEvents
                    .Where(e => e.ProfessorEventStatus == "Δημοσιευμένη")
                    .AsNoTracking()
                    .OrderByDescending(e => e.ProfessorEventActiveDate)
                    .ToListAsync(cancellationToken);

                // Load public announcements if needed (you may need to add status checks)
                var companyAnnouncementsTask = context.AnnouncementsAsCompany
                    .AsNoTracking()
                    .OrderByDescending(a => a.CompanyAnnouncementTimeToBeActive)
                    .ToListAsync(cancellationToken);

                var professorAnnouncementsTask = context.AnnouncementsAsProfessor
                    .AsNoTracking()
                    .OrderByDescending(a => a.ProfessorAnnouncementTimeToBeActive)
                    .ToListAsync(cancellationToken);

                var researchGroupAnnouncementsTask = context.AnnouncementAsResearchGroup
                    .AsNoTracking()
                    .OrderByDescending(a => a.ResearchGroupAnnouncementTimeToBeActive)
                    .ToListAsync(cancellationToken);

                await Task.WhenAll(
                    companyEventsTask,
                    professorEventsTask,
                    companyAnnouncementsTask,
                    professorAnnouncementsTask,
                    researchGroupAnnouncementsTask);

                return new FrontPageData
                {
                    CompanyEvents = await companyEventsTask,
                    ProfessorEvents = await professorEventsTask,
                    CompanyAnnouncements = await companyAnnouncementsTask,
                    ProfessorAnnouncements = await professorAnnouncementsTask,
                    ResearchGroupAnnouncements = await researchGroupAnnouncementsTask
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading front page data");
                return FrontPageData.Empty;
            }
        }
    }
}

