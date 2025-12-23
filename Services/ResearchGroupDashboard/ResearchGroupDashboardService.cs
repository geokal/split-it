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

        public Task<MutationResult> CreateOrUpdateAnnouncementAsync(ResearchGroupAnnouncementRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<MutationResult> DeleteAnnouncementAsync(long announcementId, CancellationToken cancellationToken = default)
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
