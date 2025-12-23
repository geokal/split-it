using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuizManager.Data;
using QuizManager.Models;

namespace QuizManager.Services.ResearchGroupDashboard
{
    public class ResearchGroupDashboardService : IResearchGroupDashboardService
    {
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<ResearchGroupDashboardService> _logger;

        public ResearchGroupDashboardService(
            AuthenticationStateProvider authenticationStateProvider,
            IDbContextFactory<AppDbContext> dbContextFactory,
            ILogger<ResearchGroupDashboardService> logger)
        {
            _authenticationStateProvider = authenticationStateProvider;
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public Task<ResearchGroupDashboardData> LoadDashboardDataAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task RefreshDashboardCacheAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ResearchGroupDashboardLookups> GetLookupsAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<IReadOnlyList<CompanyEvent>> GetPublishedCompanyEventsAsync(CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.CompanyEvents
                .Include(e => e.Company)
                .AsNoTracking()
                .Where(e => e.CompanyEventStatus == "Δημοσιευμένη")
                .OrderByDescending(e => e.CompanyEventActiveDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ProfessorEvent>> GetPublishedProfessorEventsAsync(CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.ProfessorEvents
                .Include(e => e.Professor)
                .AsNoTracking()
                .Where(e => e.ProfessorEventStatus == "Δημοσιευμένη")
                .OrderByDescending(e => e.ProfessorEventActiveDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<HashSet<long>> GetCompanyEventInterestsForProfessorAsync(string professorEmail, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(professorEmail))
            {
                return new HashSet<long>();
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var interestRngs = await context.InterestInCompanyEventsAsProfessor
                .AsNoTracking()
                .Where(i => i.ProfessorEmailShowInterestForCompanyEvent == professorEmail)
                .Select(i => i.RNGForCompanyEventInterestAsProfessor)
                .ToListAsync(cancellationToken);

            return interestRngs.ToHashSet();
        }

        public async Task<IReadOnlyList<AnnouncementAsCompany>> GetPublishedCompanyAnnouncementsAsync(CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.AnnouncementsAsCompany
                .AsNoTracking()
                .Where(a => a.CompanyAnnouncementStatus == "Δημοσιευμένη")
                .OrderByDescending(a => a.CompanyAnnouncementUploadDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<AnnouncementAsProfessor>> GetPublishedProfessorAnnouncementsAsync(CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.AnnouncementsAsProfessor
                .AsNoTracking()
                .Where(a => a.ProfessorAnnouncementStatus == "Δημοσιευμένη")
                .OrderByDescending(a => a.ProfessorAnnouncementUploadDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<AnnouncementAsResearchGroup>> GetPublishedResearchGroupAnnouncementsAsync(CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.AnnouncementAsResearchGroup
                .AsNoTracking()
                .Where(a => a.ResearchGroupAnnouncementStatus == "Δημοσιευμένη")
                .OrderByDescending(a => a.ResearchGroupAnnouncementUploadDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<AnnouncementAsResearchGroup>> GetUploadedResearchGroupAnnouncementsAsync(string researchGroupEmail, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(researchGroupEmail))
            {
                return new List<AnnouncementAsResearchGroup>();
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.AnnouncementAsResearchGroup
                .AsNoTracking()
                .Where(a => a.ResearchGroupAnnouncementResearchGroupEmail == researchGroupEmail)
                .OrderByDescending(a => a.ResearchGroupAnnouncementUploadDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<AnnouncementAsResearchGroup?> CreateAnnouncementAsync(AnnouncementAsResearchGroup announcement, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                context.AnnouncementAsResearchGroup.Add(announcement);
                await context.SaveChangesAsync(cancellationToken);
                return announcement;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating announcement");
                return null;
            }
        }

        public async Task<bool> UpdateAnnouncementAsync(int announcementId, AnnouncementAsResearchGroup updates, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var existing = await context.AnnouncementAsResearchGroup.FindAsync(new object[] { announcementId }, cancellationToken: cancellationToken);
                if (existing == null)
                {
                    return false;
                }

                existing.ResearchGroupAnnouncementTitle = updates.ResearchGroupAnnouncementTitle;
                existing.ResearchGroupAnnouncementDescription = updates.ResearchGroupAnnouncementDescription;
                existing.ResearchGroupAnnouncementTimeToBeActive = updates.ResearchGroupAnnouncementTimeToBeActive;
                if (updates.ResearchGroupAnnouncementAttachmentFile != null)
                {
                    existing.ResearchGroupAnnouncementAttachmentFile = updates.ResearchGroupAnnouncementAttachmentFile;
                }

                await context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating announcement {AnnouncementId}", announcementId);
                return false;
            }
        }

        public async Task<bool> DeleteAnnouncementAsync(int announcementId, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var announcement = await context.AnnouncementAsResearchGroup.FindAsync(new object[] { announcementId }, cancellationToken: cancellationToken);
                if (announcement == null)
                {
                    return false;
                }

                context.AnnouncementAsResearchGroup.Remove(announcement);
                await context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting announcement {AnnouncementId}", announcementId);
                return false;
            }
        }

        public async Task<MutationResult> DeleteAnnouncementAsync(long announcementId, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var announcement = await context.AnnouncementAsResearchGroup.FindAsync(new object[] { announcementId }, cancellationToken: cancellationToken);
                if (announcement == null)
                {
                    return MutationResult.Failed("Announcement not found");
                }

                context.AnnouncementAsResearchGroup.Remove(announcement);
                await context.SaveChangesAsync(cancellationToken);
                return MutationResult.Succeeded(announcementId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting announcement {AnnouncementId}", announcementId);
                return MutationResult.Failed(ex.Message);
            }
        }

        public async Task<bool> BulkCopyAnnouncementsAsync(IEnumerable<int> announcementIds, string researchGroupEmail, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var idsList = announcementIds.ToList();
                var announcements = await context.AnnouncementAsResearchGroup
                    .Where(a => idsList.Contains(a.Id))
                    .ToListAsync(cancellationToken);

                foreach (var original in announcements)
                {
                    var copy = new AnnouncementAsResearchGroup
                    {
                        ResearchGroupAnnouncementTitle = original.ResearchGroupAnnouncementTitle + " (Αντίγραφο)",
                        ResearchGroupAnnouncementDescription = original.ResearchGroupAnnouncementDescription,
                        ResearchGroupAnnouncementStatus = "Μη Δημοσιευμένη",
                        ResearchGroupAnnouncementUploadDate = DateTime.Now,
                        ResearchGroupAnnouncementResearchGroupEmail = researchGroupEmail,
                        ResearchGroupAnnouncementTimeToBeActive = original.ResearchGroupAnnouncementTimeToBeActive,
                        ResearchGroupAnnouncementAttachmentFile = original.ResearchGroupAnnouncementAttachmentFile,
                        ResearchGroupAnnouncementRNG = new Random().NextInt64(),
                        ResearchGroupAnnouncementRNG_HashedAsUniqueID = HashingHelper.HashLong(new Random().NextInt64())
                    };
                    context.AnnouncementAsResearchGroup.Add(copy);
                }

                await context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk copying announcements");
                return false;
            }
        }

        public async Task<int> BulkUpdateStatusAsync(IEnumerable<int> announcementIds, string newStatus, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var idsList = announcementIds.ToList();
                var announcements = await context.AnnouncementAsResearchGroup
                    .Where(a => idsList.Contains(a.Id))
                    .ToListAsync(cancellationToken);

                foreach (var announcement in announcements)
                {
                    announcement.ResearchGroupAnnouncementStatus = newStatus;
                }

                await context.SaveChangesAsync(cancellationToken);
                return announcements.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk updating announcement status");
                return 0;
            }
        }

        public Task<MutationResult> CreateOrUpdateAnnouncementAsync(ResearchGroupAnnouncementRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<AttachmentDownloadResult?> GetAnnouncementAttachmentAsync(long announcementId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<Area>> SearchAreasAsync(string query, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<Skill>> SearchSkillsAsync(string query, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<Company>> SearchCompaniesAsync(string query, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<Professor>> SearchProfessorsAsync(string query, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<Company>> FilterCompaniesAsync(ResearchGroupCompanySearchRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<Professor>> FilterProfessorsAsync(ResearchGroupProfessorSearchRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        private static string? ResolveUserEmail(ClaimsPrincipal user)
        {
            return user.FindFirst("name")?.Value
                ?? user.FindFirst(ClaimTypes.Email)?.Value
                ?? user.Claims.FirstOrDefault(c => string.Equals(c.Type, "email", StringComparison.OrdinalIgnoreCase))?.Value
                ?? user.Identity?.Name;
        }
    }
}
