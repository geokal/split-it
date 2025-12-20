using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using QuizManager.Data;
using QuizManager.Models;

namespace QuizManager.Services.ProfessorDashboard
{
    public class ProfessorDashboardService : IProfessorDashboardService
    {
        private const string DashboardCachePrefix = "professor-dashboard:";
        private const string LookupCachePrefix = "professor-lookups:";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<ProfessorDashboardService> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly SemaphoreSlim _dashboardLock = new(1, 1);
        private readonly SemaphoreSlim _lookupLock = new(1, 1);

        public ProfessorDashboardService(
            AuthenticationStateProvider authenticationStateProvider,
            IDbContextFactory<AppDbContext> dbContextFactory,
            ILogger<ProfessorDashboardService> logger,
            IMemoryCache memoryCache)
        {
            _authenticationStateProvider = authenticationStateProvider;
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public async Task<ProfessorDashboardData> LoadDashboardDataAsync(CancellationToken cancellationToken = default)
        {
            var email = await ResolveProfessorEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return ProfessorDashboardData.Empty;
            }

            if (_memoryCache.TryGetValue(DashboardCachePrefix + email, out ProfessorDashboardData cached))
            {
                return cached;
            }

            await _dashboardLock.WaitAsync(cancellationToken);
            try
            {
                if (_memoryCache.TryGetValue(DashboardCachePrefix + email, out cached))
                {
                    return cached;
                }

                var data = await BuildDashboardDataAsync(email, cancellationToken);
                _memoryCache.Set(DashboardCachePrefix + email, data, CacheDuration);
                return data;
            }
            finally
            {
                _dashboardLock.Release();
            }
        }

        public async Task RefreshDashboardCacheAsync(CancellationToken cancellationToken = default)
        {
            var email = await ResolveProfessorEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return;
            }

            _memoryCache.Remove(DashboardCachePrefix + email);
            _memoryCache.Remove(LookupCachePrefix + email);
        }

        public async Task<ProfessorDashboardLookups> GetLookupsAsync(CancellationToken cancellationToken = default)
        {
            var email = await ResolveProfessorEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return ProfessorDashboardLookups.Empty;
            }

            if (_memoryCache.TryGetValue(LookupCachePrefix + email, out ProfessorDashboardLookups cached))
            {
                return cached;
            }

            await _lookupLock.WaitAsync(cancellationToken);
            try
            {
                if (_memoryCache.TryGetValue(LookupCachePrefix + email, out cached))
                {
                    return cached;
                }

                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var lookups = await BuildLookupsAsync(context, cancellationToken);
                _memoryCache.Set(LookupCachePrefix + email, lookups, CacheDuration);
                return lookups;
            }
            finally
            {
                _lookupLock.Release();
            }
        }

        // Search operations
        public async Task<IReadOnlyList<Student>> SearchStudentsAsync(StudentSearchFilter filter, CancellationToken cancellationToken = default)
        {
            filter ??= new StudentSearchFilter();
            var maxResults = Math.Clamp(filter.MaxResults, 1, 100);

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var query = context.Students.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                var name = filter.Name.Trim().ToLowerInvariant();
                query = query.Where(s => s.Name != null && s.Name.ToLower().Contains(name));
            }

            if (!string.IsNullOrWhiteSpace(filter.Surname))
            {
                var surname = filter.Surname.Trim().ToLowerInvariant();
                query = query.Where(s => s.Surname != null && s.Surname.ToLower().Contains(surname));
            }

            if (!string.IsNullOrWhiteSpace(filter.RegistrationNumber) && long.TryParse(filter.RegistrationNumber, out var regNumber))
            {
                query = query.Where(s => s.RegNumber == regNumber);
            }

            if (!string.IsNullOrWhiteSpace(filter.Department))
            {
                var department = filter.Department.Trim().ToLowerInvariant();
                query = query.Where(s => s.Department != null && s.Department.ToLower().Contains(department));
            }

            if (!string.IsNullOrWhiteSpace(filter.School))
            {
                var school = filter.School.Trim().ToLowerInvariant();
                query = query.Where(s => s.School != null && s.School.ToLower().Contains(school));
            }

            if (!string.IsNullOrWhiteSpace(filter.AreasOfExpertise))
            {
                var areas = filter.AreasOfExpertise.Trim().ToLowerInvariant();
                query = query.Where(s => s.AreasOfExpertise != null && s.AreasOfExpertise.ToLower().Contains(areas));
            }

            if (!string.IsNullOrWhiteSpace(filter.Keywords))
            {
                var keywords = filter.Keywords.Trim().ToLowerInvariant();
                query = query.Where(s => s.Keywords != null && s.Keywords.ToLower().Contains(keywords));
            }

            return await query
                .OrderBy(s => s.Surname)
                .ThenBy(s => s.Name)
                .Take(maxResults)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Company>> SearchCompaniesAsync(CompanySearchFilter filter, CancellationToken cancellationToken = default)
        {
            filter ??= new CompanySearchFilter();
            var maxResults = Math.Clamp(filter.MaxResults, 1, 100);

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var query = context.Companies.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter.CompanyName))
            {
                var name = filter.CompanyName.Trim().ToLowerInvariant();
                query = query.Where(c => (c.CompanyName != null && c.CompanyName.ToLower().Contains(name)) ||
                                         (c.CompanyNameENG != null && c.CompanyNameENG.ToLower().Contains(name)));
            }

            if (!string.IsNullOrWhiteSpace(filter.CompanyEmail))
            {
                var email = filter.CompanyEmail.Trim().ToLowerInvariant();
                query = query.Where(c => c.CompanyEmail != null && c.CompanyEmail.ToLower().Contains(email));
            }

            if (!string.IsNullOrWhiteSpace(filter.CompanyType))
            {
                var type = filter.CompanyType.Trim();
                query = query.Where(c => c.CompanyType == type);
            }

            if (!string.IsNullOrWhiteSpace(filter.CompanyActivity))
            {
                var activity = filter.CompanyActivity.Trim().ToLowerInvariant();
                query = query.Where(c => c.CompanyActivity != null && c.CompanyActivity.ToLower().Contains(activity));
            }

            if (!string.IsNullOrWhiteSpace(filter.CompanyTown))
            {
                var town = filter.CompanyTown.Trim();
                query = query.Where(c => c.CompanyTown == town);
            }

            if (!string.IsNullOrWhiteSpace(filter.CompanyAreas))
            {
                var areas = filter.CompanyAreas.Trim().ToLowerInvariant();
                query = query.Where(c => c.CompanyAreas != null && c.CompanyAreas.ToLower().Contains(areas));
            }

            if (!string.IsNullOrWhiteSpace(filter.CompanyDesiredSkills))
            {
                var skills = filter.CompanyDesiredSkills.Trim().ToLowerInvariant();
                query = query.Where(c => c.CompanyDesiredSkills != null && c.CompanyDesiredSkills.ToLower().Contains(skills));
            }

            return await query
                .OrderBy(c => c.CompanyName)
                .Take(maxResults)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ResearchGroup>> SearchResearchGroupsAsync(ResearchGroupSearchFilter filter, CancellationToken cancellationToken = default)
        {
            filter ??= new ResearchGroupSearchFilter();
            var maxResults = Math.Clamp(filter.MaxResults, 1, 100);

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var query = context.ResearchGroups.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                var name = filter.Name.Trim().ToLowerInvariant();
                query = query.Where(r => r.ResearchGroupName != null && r.ResearchGroupName.ToLower().Contains(name));
            }

            if (!string.IsNullOrWhiteSpace(filter.Areas))
            {
                var areas = filter.Areas.Trim().ToLowerInvariant();
                query = query.Where(r => r.ResearchGroupAreas != null && r.ResearchGroupAreas.ToLower().Contains(areas));
            }

            if (!string.IsNullOrWhiteSpace(filter.Skills))
            {
                var skills = filter.Skills.Trim().ToLowerInvariant();
                query = query.Where(r => r.ResearchGroupSkills != null && r.ResearchGroupSkills.ToLower().Contains(skills));
            }

            return await query
                .OrderBy(r => r.ResearchGroupName)
                .Take(maxResults)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<string>> SearchAreasAsync(string term, CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var areas = await context.Areas.AsNoTracking().ToListAsync(cancellationToken);

            var suggestions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var trimmedTerm = term?.Trim();
            var normalized = trimmedTerm?.ToLowerInvariant();

            foreach (var area in areas)
            {
                var areaMatches = string.IsNullOrWhiteSpace(normalized) ||
                                  (area.AreaName?.ToLower().Contains(normalized) ?? false) ||
                                  (area.AreaSubFields?.ToLower().Contains(normalized) ?? false);

                if (!areaMatches)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(area.AreaName))
                {
                    suggestions.Add(area.AreaName);
                }

                if (!string.IsNullOrWhiteSpace(area.AreaSubFields))
                {
                    var subfields = area.AreaSubFields
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    foreach (var subfield in subfields)
                    {
                        if (string.IsNullOrWhiteSpace(normalized) ||
                            subfield.Contains(trimmedTerm!, StringComparison.OrdinalIgnoreCase))
                        {
                            suggestions.Add($"{area.AreaName} - {subfield}");
                        }
                    }
                }
            }

            return suggestions.OrderBy(s => s).ToList();
        }

        public async Task<IReadOnlyList<string>> SearchSkillsAsync(string term, CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var trimmedTerm = term?.Trim();
            var normalized = trimmedTerm?.ToLowerInvariant();

            var skills = await context.Skills
                .AsNoTracking()
                .Where(s => string.IsNullOrWhiteSpace(normalized) ||
                           (s.SkillName != null && s.SkillName.ToLower().Contains(normalized)))
                .Select(s => s.SkillName)
                .Distinct()
                .OrderBy(s => s)
                .Take(50)
                .ToListAsync(cancellationToken);

            return skills;
        }

        public async Task<IReadOnlyList<CompanyThesis>> SearchCompanyThesesAsync(CompanyThesisSearchFilter filter, CancellationToken cancellationToken = default)
        {
            filter ??= new CompanyThesisSearchFilter();
            var maxResults = Math.Clamp(filter.MaxResults, 1, 200);

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var query = context.CompanyTheses
                .Include(t => t.Company)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter.CompanyName))
            {
                var companyName = filter.CompanyName.Trim().ToLowerInvariant();
                query = query.Where(t => t.Company != null &&
                    (t.Company.CompanyName != null && t.Company.CompanyName.ToLower().Contains(companyName)) ||
                    (t.Company.CompanyNameENG != null && t.Company.CompanyNameENG.ToLower().Contains(companyName)));
            }

            if (!string.IsNullOrWhiteSpace(filter.Title))
            {
                var title = filter.Title.Trim().ToLowerInvariant();
                query = query.Where(t => t.CompanyThesisTitle != null && t.CompanyThesisTitle.ToLower().Contains(title));
            }

            if (!string.IsNullOrWhiteSpace(filter.Supervisor))
            {
                var supervisor = filter.Supervisor.Trim().ToLowerInvariant();
                query = query.Where(t => t.CompanyThesisCompanySupervisorFullName != null &&
                    t.CompanyThesisCompanySupervisorFullName.ToLower().Contains(supervisor));
            }

            if (!string.IsNullOrWhiteSpace(filter.Department))
            {
                var department = filter.Department.Trim().ToLowerInvariant();
                query = query.Where(t => t.CompanyThesisDepartment != null && t.CompanyThesisDepartment.ToLower().Contains(department));
            }

            if (filter.EarliestStartDate.HasValue)
            {
                query = query.Where(t => t.CompanyThesisStartingDate >= filter.EarliestStartDate.Value);
            }

            if (filter.RequiredSkills.Any())
            {
                foreach (var skill in filter.RequiredSkills)
                {
                    var skillLower = skill.ToLowerInvariant();
                    query = query.Where(t => t.CompanyThesisSkillsNeeded != null &&
                        t.CompanyThesisSkillsNeeded.ToLower().Contains(skillLower));
                }
            }

            return await query
                .OrderByDescending(t => t.CompanyThesisUploadDateTime)
                .Take(maxResults)
                .ToListAsync(cancellationToken);
        }

        // CRUD operations
        public async Task<MutationResult> CreateOrUpdateThesisAsync(ProfessorThesis thesis, CancellationToken cancellationToken = default)
        {
            if (thesis == null)
            {
                return MutationResult.Failed("Thesis payload is required.");
            }

            var email = await ResolveProfessorEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return MutationResult.Failed("Unable to resolve professor context.");
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var normalizedEmail = email.ToLowerInvariant();

            if (thesis.Id == 0)
            {
                // Create new
                thesis.ProfessorEmailUsedToUploadThesis = normalizedEmail;
                thesis.ThesisUploadDateTime = DateTime.UtcNow;
                if (thesis.RNGForThesisUploaded == 0)
                {
                    thesis.RNGForThesisUploaded = new Random().NextInt64();
                    thesis.RNGForThesisUploaded_HashedAsUniqueID = Data.HashingHelper.HashLong(thesis.RNGForThesisUploaded);
                }
                context.ProfessorTheses.Add(thesis);
            }
            else
            {
                // Update existing
                var existing = await context.ProfessorTheses
                    .FirstOrDefaultAsync(t => t.Id == thesis.Id && 
                        t.ProfessorEmailUsedToUploadThesis != null && 
                        t.ProfessorEmailUsedToUploadThesis.ToLower() == normalizedEmail, cancellationToken);

                if (existing == null)
                {
                    return MutationResult.Failed("Thesis not found or not owned by this professor.");
                }

                thesis.ProfessorEmailUsedToUploadThesis = normalizedEmail;
                context.Entry(existing).CurrentValues.SetValues(thesis);
            }

            await context.SaveChangesAsync(cancellationToken);
            await RefreshDashboardCacheAsync(cancellationToken);
            return MutationResult.Succeeded(thesis.Id);
        }

        public async Task<MutationResult> CreateOrUpdateInternshipAsync(ProfessorInternship internship, CancellationToken cancellationToken = default)
        {
            if (internship == null)
            {
                return MutationResult.Failed("Internship payload is required.");
            }

            var email = await ResolveProfessorEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return MutationResult.Failed("Unable to resolve professor context.");
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var normalizedEmail = email.ToLowerInvariant();

            if (internship.Id == 0)
            {
                // Create new
                internship.ProfessorEmailUsedToUploadInternship = normalizedEmail;
                internship.ProfessorInternshipUploadDate = DateTime.UtcNow;
                if (internship.RNGForInternshipUploadedAsProfessor == 0)
                {
                    internship.RNGForInternshipUploadedAsProfessor = new Random().NextInt64();
                    internship.RNGForInternshipUploadedAsProfessor_HashedAsUniqueID = Data.HashingHelper.HashLong(internship.RNGForInternshipUploadedAsProfessor);
                }
                context.ProfessorInternships.Add(internship);
            }
            else
            {
                // Update existing
                var existing = await context.ProfessorInternships
                    .FirstOrDefaultAsync(i => i.Id == internship.Id && 
                        i.ProfessorEmailUsedToUploadInternship != null && 
                        i.ProfessorEmailUsedToUploadInternship.ToLower() == normalizedEmail, cancellationToken);

                if (existing == null)
                {
                    return MutationResult.Failed("Internship not found or not owned by this professor.");
                }

                internship.ProfessorEmailUsedToUploadInternship = normalizedEmail;
                context.Entry(existing).CurrentValues.SetValues(internship);
            }

            await context.SaveChangesAsync(cancellationToken);
            await RefreshDashboardCacheAsync(cancellationToken);
            return MutationResult.Succeeded(internship.Id);
        }

        public async Task<MutationResult> CreateOrUpdateEventAsync(ProfessorEvent professorEvent, CancellationToken cancellationToken = default)
        {
            if (professorEvent == null)
            {
                return MutationResult.Failed("Event payload is required.");
            }

            var email = await ResolveProfessorEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return MutationResult.Failed("Unable to resolve professor context.");
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var normalizedEmail = email.ToLowerInvariant();

            if (professorEvent.Id == 0)
            {
                // Create new
                professorEvent.ProfessorEmailUsedToUploadEvent = normalizedEmail;
                professorEvent.ProfessorEventUploadedDate = DateTime.UtcNow;
                if (professorEvent.RNGForEventUploadedAsProfessor == 0)
                {
                    professorEvent.RNGForEventUploadedAsProfessor = new Random().NextInt64();
                    professorEvent.RNGForEventUploadedAsProfessor_HashedAsUniqueID = Data.HashingHelper.HashLong(professorEvent.RNGForEventUploadedAsProfessor);
                }
                context.ProfessorEvents.Add(professorEvent);
            }
            else
            {
                // Update existing
                var existing = await context.ProfessorEvents
                    .FirstOrDefaultAsync(e => e.Id == professorEvent.Id && 
                        e.ProfessorEmailUsedToUploadEvent != null && 
                        e.ProfessorEmailUsedToUploadEvent.ToLower() == normalizedEmail, cancellationToken);

                if (existing == null)
                {
                    return MutationResult.Failed("Event not found or not owned by this professor.");
                }

                professorEvent.ProfessorEmailUsedToUploadEvent = normalizedEmail;
                context.Entry(existing).CurrentValues.SetValues(professorEvent);
            }

            await context.SaveChangesAsync(cancellationToken);
            await RefreshDashboardCacheAsync(cancellationToken);
            return MutationResult.Succeeded(professorEvent.Id);
        }

        public async Task<MutationResult> CreateOrUpdateAnnouncementAsync(AnnouncementAsProfessor announcement, CancellationToken cancellationToken = default)
        {
            if (announcement == null)
            {
                return MutationResult.Failed("Announcement payload is required.");
            }

            var email = await ResolveProfessorEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return MutationResult.Failed("Unable to resolve professor context.");
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var normalizedEmail = email.ToLowerInvariant();

                if (announcement.Id == 0)
                {
                    // Create new
                    announcement.ProfessorAnnouncementProfessorEmail = normalizedEmail;
                    announcement.ProfessorAnnouncementUploadDate = DateTime.UtcNow;
                    if (announcement.ProfessorAnnouncementRNG == null || announcement.ProfessorAnnouncementRNG == 0)
                    {
                        announcement.ProfessorAnnouncementRNG = new Random().NextInt64();
                        announcement.ProfessorAnnouncementRNG_HashedAsUniqueID = HashingHelper.HashLong(announcement.ProfessorAnnouncementRNG.Value);
                    }
                    context.AnnouncementsAsProfessor.Add(announcement);
                }
                else
                {
                    // Update existing
                    var existing = await context.AnnouncementsAsProfessor
                        .FirstOrDefaultAsync(a => a.Id == announcement.Id &&
                            a.ProfessorAnnouncementProfessorEmail != null &&
                            a.ProfessorAnnouncementProfessorEmail.ToLower() == normalizedEmail, cancellationToken);

                    if (existing == null)
                    {
                        return MutationResult.Failed("Announcement not found or not owned by this professor.");
                    }

                    announcement.ProfessorAnnouncementProfessorEmail = normalizedEmail;
                    context.Entry(existing).CurrentValues.SetValues(announcement);
                }

                await context.SaveChangesAsync(cancellationToken);
                await RefreshDashboardCacheAsync(cancellationToken);
                return MutationResult.Succeeded(announcement.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating professor announcement");
                return MutationResult.Failed($"Error: {ex.Message}");
            }
        }

        // Delete operations
        public async Task<bool> DeleteThesisAsync(int thesisId, CancellationToken cancellationToken = default)
        {
            var email = await ResolveProfessorEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var normalizedEmail = email.ToLowerInvariant();

                var thesis = await context.ProfessorTheses
                    .FirstOrDefaultAsync(t => t.Id == thesisId && 
                        t.ProfessorEmailUsedToUploadThesis != null && 
                        t.ProfessorEmailUsedToUploadThesis.ToLower() == normalizedEmail, cancellationToken);

                if (thesis == null)
                {
                    return false;
                }

                context.ProfessorTheses.Remove(thesis);
                await context.SaveChangesAsync(cancellationToken);
                await RefreshDashboardCacheAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting professor thesis");
                return false;
            }
        }

        public async Task<bool> DeleteInternshipAsync(int internshipId, CancellationToken cancellationToken = default)
        {
            var email = await ResolveProfessorEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var normalizedEmail = email.ToLowerInvariant();

                var internship = await context.ProfessorInternships
                    .FirstOrDefaultAsync(i => i.Id == internshipId && 
                        i.ProfessorEmailUsedToUploadInternship != null && 
                        i.ProfessorEmailUsedToUploadInternship.ToLower() == normalizedEmail, cancellationToken);

                if (internship == null)
                {
                    return false;
                }

                context.ProfessorInternships.Remove(internship);
                await context.SaveChangesAsync(cancellationToken);
                await RefreshDashboardCacheAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting professor internship");
                return false;
            }
        }

        public async Task<bool> DeleteEventAsync(int eventId, CancellationToken cancellationToken = default)
        {
            var email = await ResolveProfessorEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var normalizedEmail = email.ToLowerInvariant();

                var professorEvent = await context.ProfessorEvents
                    .FirstOrDefaultAsync(e => e.Id == eventId && 
                        e.ProfessorEmailUsedToUploadEvent != null && 
                        e.ProfessorEmailUsedToUploadEvent.ToLower() == normalizedEmail, cancellationToken);

                if (professorEvent == null)
                {
                    return false;
                }

                context.ProfessorEvents.Remove(professorEvent);
                await context.SaveChangesAsync(cancellationToken);
                await RefreshDashboardCacheAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting professor event");
                return false;
            }
        }

        public async Task<bool> DeleteAnnouncementAsync(int announcementId, CancellationToken cancellationToken = default)
        {
            var email = await ResolveProfessorEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var normalizedEmail = email.ToLowerInvariant();

                var announcement = await context.AnnouncementsAsProfessor
                    .FirstOrDefaultAsync(a => a.Id == announcementId && 
                        a.ProfessorAnnouncementProfessorEmail != null && 
                        a.ProfessorAnnouncementProfessorEmail.ToLower() == normalizedEmail, cancellationToken);

                if (announcement == null)
                {
                    return false;
                }

                context.AnnouncementsAsProfessor.Remove(announcement);
                await context.SaveChangesAsync(cancellationToken);
                await RefreshDashboardCacheAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting professor announcement");
                return false;
            }
        }

        // Status updates
        public async Task<MutationResult> UpdateThesisStatusAsync(int thesisId, string newStatus, CancellationToken cancellationToken = default)
        {
            var email = await ResolveProfessorEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return MutationResult.Failed("Unable to resolve professor context.");
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var normalizedEmail = email.ToLowerInvariant();

                var thesis = await context.ProfessorTheses
                    .FirstOrDefaultAsync(t => t.Id == thesisId &&
                        t.ProfessorEmailUsedToUploadThesis != null &&
                        t.ProfessorEmailUsedToUploadThesis.ToLower() == normalizedEmail, cancellationToken);

                if (thesis == null)
                {
                    return MutationResult.Failed("Thesis not found or not owned by this professor.");
                }

                thesis.ThesisStatus = newStatus;
                thesis.ThesisUpdateDateTime = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);
                await RefreshDashboardCacheAsync(cancellationToken);
                return MutationResult.Succeeded(thesisId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating thesis status");
                return MutationResult.Failed($"Error: {ex.Message}");
            }
        }

        public async Task<MutationResult> UpdateInternshipStatusAsync(int internshipId, string newStatus, CancellationToken cancellationToken = default)
        {
            var email = await ResolveProfessorEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return MutationResult.Failed("Unable to resolve professor context.");
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var normalizedEmail = email.ToLowerInvariant();

                var internship = await context.ProfessorInternships
                    .FirstOrDefaultAsync(i => i.Id == internshipId &&
                        i.ProfessorEmailUsedToUploadInternship != null &&
                        i.ProfessorEmailUsedToUploadInternship.ToLower() == normalizedEmail, cancellationToken);

                if (internship == null)
                {
                    return MutationResult.Failed("Internship not found or not owned by this professor.");
                }

                internship.ProfessorUploadedInternshipStatus = newStatus;
                internship.ProfessorInternshipLastUpdate = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);
                await RefreshDashboardCacheAsync(cancellationToken);
                return MutationResult.Succeeded(internshipId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating internship status");
                return MutationResult.Failed($"Error: {ex.Message}");
            }
        }

        public async Task<MutationResult> UpdateEventStatusAsync(int eventId, string newStatus, CancellationToken cancellationToken = default)
        {
            var email = await ResolveProfessorEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return MutationResult.Failed("Unable to resolve professor context.");
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var normalizedEmail = email.ToLowerInvariant();

                var professorEvent = await context.ProfessorEvents
                    .FirstOrDefaultAsync(e => e.Id == eventId &&
                        e.ProfessorEmailUsedToUploadEvent != null &&
                        e.ProfessorEmailUsedToUploadEvent.ToLower() == normalizedEmail, cancellationToken);

                if (professorEvent == null)
                {
                    return MutationResult.Failed("Event not found or not owned by this professor.");
                }

                professorEvent.ProfessorEventStatus = newStatus;
                await context.SaveChangesAsync(cancellationToken);
                await RefreshDashboardCacheAsync(cancellationToken);
                return MutationResult.Succeeded(eventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event status");
                return MutationResult.Failed($"Error: {ex.Message}");
            }
        }

        public async Task<MutationResult> UpdateAnnouncementStatusAsync(int announcementId, string newStatus, CancellationToken cancellationToken = default)
        {
            var email = await ResolveProfessorEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return MutationResult.Failed("Unable to resolve professor context.");
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var normalizedEmail = email.ToLowerInvariant();

                var announcement = await context.AnnouncementsAsProfessor
                    .FirstOrDefaultAsync(a => a.Id == announcementId &&
                        a.ProfessorAnnouncementProfessorEmail != null &&
                        a.ProfessorAnnouncementProfessorEmail.ToLower() == normalizedEmail, cancellationToken);

                if (announcement == null)
                {
                    return MutationResult.Failed("Announcement not found or not owned by this professor.");
                }

                announcement.ProfessorAnnouncementStatus = newStatus;
                await context.SaveChangesAsync(cancellationToken);
                await RefreshDashboardCacheAsync(cancellationToken);
                return MutationResult.Succeeded(announcementId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating announcement status");
                return MutationResult.Failed($"Error: {ex.Message}");
            }
        }

        // Application operations
        public async Task<IReadOnlyList<ProfessorThesisApplied>> GetThesisApplicationsAsync(CancellationToken cancellationToken = default)
        {
            var email = await ResolveProfessorEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return Array.Empty<ProfessorThesisApplied>();
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var normalizedEmail = email.ToLowerInvariant();

            return await context.ProfessorThesesApplied
                .Include(a => a.StudentDetails)
                .Include(a => a.ProfessorDetails)
                .AsNoTracking()
                .Where(a => a.ProfessorEmailWhereStudentAppliedForProfessorThesis != null &&
                           a.ProfessorEmailWhereStudentAppliedForProfessorThesis.ToLower() == normalizedEmail)
                .OrderByDescending(a => a.DateTimeStudentAppliedForProfessorThesis)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ProfessorInternshipApplied>> GetInternshipApplicationsAsync(CancellationToken cancellationToken = default)
        {
            var email = await ResolveProfessorEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return Array.Empty<ProfessorInternshipApplied>();
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var normalizedEmail = email.ToLowerInvariant();

            return await context.ProfessorInternshipsApplied
                .Include(a => a.StudentDetails)
                .Include(a => a.ProfessorDetails)
                .AsNoTracking()
                .Where(a => a.ProfessorEmailWhereStudentAppliedForInternship != null &&
                           a.ProfessorEmailWhereStudentAppliedForInternship.ToLower() == normalizedEmail)
                .OrderByDescending(a => a.DateTimeStudentAppliedForProfessorInternship)
                .ToListAsync(cancellationToken);
        }

        public async Task<MutationResult> DecideOnThesisApplicationAsync(long applicationRng, string studentUniqueId, ApplicationDecision decision, CancellationToken cancellationToken = default)
        {
            var email = await ResolveProfessorEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return MutationResult.Failed("Unable to resolve professor context.");
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var normalized = email.ToLowerInvariant();

            var thesisApplication = await context.ProfessorThesesApplied
                .FirstOrDefaultAsync(a => a.RNGForProfessorThesisApplied == applicationRng &&
                                          a.StudentUniqueIDAppliedForProfessorThesis == studentUniqueId &&
                                          a.ProfessorEmailWhereStudentAppliedForProfessorThesis != null &&
                                          a.ProfessorEmailWhereStudentAppliedForProfessorThesis.ToLower() == normalized,
                                          cancellationToken);

            if (thesisApplication == null)
            {
                return MutationResult.Failed("Thesis application not found.");
            }

            switch (decision)
            {
                case ApplicationDecision.Accept:
                    thesisApplication.ProfessorThesisStatusAppliedAtProfessorSide = "Επιτυχής";
                    thesisApplication.ProfessorThesisStatusAppliedAtStudentSide = "Επιτυχής";
                    break;
                case ApplicationDecision.Reject:
                    thesisApplication.ProfessorThesisStatusAppliedAtProfessorSide = "Απορρίφθηκε";
                    thesisApplication.ProfessorThesisStatusAppliedAtStudentSide = "Απορρίφθηκε";
                    break;
                case ApplicationDecision.Cancel:
                    thesisApplication.ProfessorThesisStatusAppliedAtProfessorSide = "Ακυρώθηκε";
                    thesisApplication.ProfessorThesisStatusAppliedAtStudentSide = "Ακυρώθηκε";
                    break;
            }

            await context.SaveChangesAsync(cancellationToken);
            await RefreshDashboardCacheAsync(cancellationToken);
            return MutationResult.Succeeded();
        }

        public async Task<MutationResult> DecideOnInternshipApplicationAsync(long applicationRng, string studentUniqueId, ApplicationDecision decision, CancellationToken cancellationToken = default)
        {
            var email = await ResolveProfessorEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return MutationResult.Failed("Unable to resolve professor context.");
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var normalized = email.ToLowerInvariant();

            var internshipApplication = await context.ProfessorInternshipsApplied
                .FirstOrDefaultAsync(a => a.RNGForProfessorInternshipApplied == applicationRng &&
                                          a.StudentUniqueIDAppliedForProfessorInternship == studentUniqueId &&
                                          a.ProfessorEmailWhereStudentAppliedForInternship != null &&
                                          a.ProfessorEmailWhereStudentAppliedForInternship.ToLower() == normalized,
                                          cancellationToken);

            if (internshipApplication == null)
            {
                return MutationResult.Failed("Internship application not found.");
            }

            switch (decision)
            {
                case ApplicationDecision.Accept:
                    internshipApplication.InternshipStatusAppliedAtTheProfessorSide = "Επιτυχής";
                    internshipApplication.InternshipStatusAppliedAtTheStudentSide = "Επιτυχής";
                    break;
                case ApplicationDecision.Reject:
                    internshipApplication.InternshipStatusAppliedAtTheProfessorSide = "Απορρίφθηκε";
                    internshipApplication.InternshipStatusAppliedAtTheStudentSide = "Απορρίφθηκε";
                    break;
                case ApplicationDecision.Cancel:
                    internshipApplication.InternshipStatusAppliedAtTheProfessorSide = "Ακυρώθηκε";
                    internshipApplication.InternshipStatusAppliedAtTheStudentSide = "Ακυρώθηκε";
                    break;
            }

            await context.SaveChangesAsync(cancellationToken);
            await RefreshDashboardCacheAsync(cancellationToken);
            return MutationResult.Succeeded();
        }

        // Event interest operations
        public async Task<IReadOnlyList<InterestInProfessorEvent>> GetProfessorEventStudentInterestsAsync(long professorEventRng, CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.InterestInProfessorEvents
                .AsNoTracking()
                .Include(i => i.StudentDetails)
                .Include(i => i.ProfessorDetails)
                .Where(i => i.RNGForProfessorEventInterest == professorEventRng)
                .OrderByDescending(i => i.DateTimeStudentShowInterest)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<InterestInProfessorEventAsCompany>> GetProfessorEventCompanyInterestsAsync(long professorEventRng, CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.InterestInProfessorEventsAsCompany
                .AsNoTracking()
                .Include(i => i.CompanyDetails)
                .Include(i => i.ProfessorDetails)
                .Where(i => i.RNGForProfessorEventInterestAsCompany == professorEventRng)
                .OrderByDescending(i => i.DateTimeCompanyShowInterestForProfessorEvent)
                .ToListAsync(cancellationToken);
        }

        public async Task<MutationResult> ShowInterestInCompanyEventAsProfessorAsync(long eventRng, string professorEmail, CancellationToken cancellationToken = default)
        {
            // This method is the same as CompanyDashboardService.ShowInterestInCompanyEventAsProfessorAsync
            // Delegating to avoid code duplication, or we can copy the implementation
            // For now, I'll implement it directly here to maintain service independence
            
            if (eventRng <= 0)
            {
                return MutationResult.Failed("Δεν βρέθηκε η εκδήλωση.");
            }

            if (string.IsNullOrWhiteSpace(professorEmail))
            {
                return MutationResult.Failed("Δεν βρέθηκαν στοιχεία καθηγητή.");
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var normalizedProfessorEmail = professorEmail.Trim().ToLowerInvariant();

                var professor = await context.Professors
                    .FirstOrDefaultAsync(p => p.ProfEmail != null && p.ProfEmail.ToLower() == normalizedProfessorEmail, cancellationToken);

                if (professor == null)
                {
                    return MutationResult.Failed("Δεν βρέθηκαν στοιχεία καθηγητή.");
                }

                var companyEvent = await context.CompanyEvents
                    .Include(e => e.Company)
                    .FirstOrDefaultAsync(e => e.RNGForEventUploadedAsCompany == eventRng, cancellationToken);

                if (companyEvent == null)
                {
                    return MutationResult.Failed("Δεν βρέθηκε η εκδήλωση.");
                }

                if (!string.Equals(companyEvent.CompanyEventStatus, "Δημοσιευμένη", StringComparison.OrdinalIgnoreCase))
                {
                    return MutationResult.Failed("Η Εκδήλωση έχει Αποδημοσιευτεί. Παρακαλώ δοκιμάστε αργότερα.");
                }

                var normalizedCompanyEmail = companyEvent.CompanyEmailUsedToUploadEvent?.ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(normalizedCompanyEmail))
                {
                    return MutationResult.Failed("Δεν βρέθηκαν στοιχεία εταιρείας.");
                }

                var existingInterest = await context.InterestInCompanyEventAsProfessor
                    .FirstOrDefaultAsync(i =>
                        i.ProfessorEmailShowInterestForCompanyEvent != null &&
                        i.ProfessorEmailShowInterestForCompanyEvent.ToLower() == normalizedProfessorEmail &&
                        i.RNGForCompanyEventInterestAsProfessor == eventRng, cancellationToken);

                if (existingInterest != null)
                {
                    return MutationResult.Failed("Έχετε ήδη δείξει ενδιαφέρον για αυτήν την εκδήλωση.");
                }

                var company = companyEvent.Company ?? await context.Companies
                    .FirstOrDefaultAsync(c => c.CompanyEmail != null && c.CompanyEmail.ToLower() == normalizedCompanyEmail, cancellationToken);

                if (company == null)
                {
                    return MutationResult.Failed("Δεν βρέθηκαν στοιχεία εταιρείας.");
                }

                await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

                var interest = new InterestInCompanyEventAsProfessor
                {
                    RNGForCompanyEventInterestAsProfessor = companyEvent.RNGForEventUploadedAsCompany,
                    RNGForCompanyEventInterestAsProfessor_HashedAsUniqueID = companyEvent.RNGForEventUploadedAsCompany_HashedAsUniqueID ?? HashingHelper.HashLong(companyEvent.RNGForEventUploadedAsCompany),
                    DateTimeProfessorShowInterestForCompanyEvent = DateTime.Now,
                    CompanyEventStatus_ShowInterestAsProfessor_AtCompanySide = "Προς Επεξεργασία",
                    CompanyEventStatus_ShowInterestAsProfessor_AtProfessorSide = "Έχετε Δείξει Ενδιαφέρον",
                    ProfessorEmailShowInterestForCompanyEvent = professor.ProfEmail,
                    ProfessorUniqueIDShowInterestForCompanyEvent = professor.Professor_UniqueID,
                    CompanyEmailWhereProfessorShowedInterest = companyEvent.CompanyEmailUsedToUploadEvent,
                    CompanyUniqueIDWhereProfessorShowedInterest = company.Company_UniqueID,
                    ProfessorDetails = new InterestInCompanyEventAsProfessor_ProfessorDetails
                    {
                        ProfessorUniqueIDShowInterestForCompanyEvent = professor.Professor_UniqueID,
                        ProfessorEmailShowInterestForCompanyEvent = professor.ProfEmail,
                        DateTimeProfessorShowInterestForCompanyEvent = DateTime.Now,
                        RNGForCompanyEventShowInterestAsProfessor_HashedAsUniqueID = companyEvent.RNGForEventUploadedAsCompany_HashedAsUniqueID ?? HashingHelper.HashLong(companyEvent.RNGForEventUploadedAsCompany)
                    },
                    CompanyDetails = new InterestInCompanyEventAsProfessor_CompanyDetails
                    {
                        CompanyUniqueIDWhereProfessorShowInterestForCompanyEvent = company.Company_UniqueID,
                        CompanyEmailWhereProfessorShowInterestForCompanyEvent = companyEvent.CompanyEmailUsedToUploadEvent
                    }
                };

                context.InterestInCompanyEventAsProfessor.Add(interest);
                context.PlatformActions.Add(new PlatformActions
                {
                    UserRole_PerformedAction = "PROFESSOR",
                    ForWhat_PerformedAction = "COMPANY_EVENT",
                    HashedPositionRNG_PerformedAction = HashingHelper.HashLong(companyEvent.RNGForEventUploadedAsCompany),
                    TypeOfAction_PerformedAction = "SHOW_INTEREST",
                    DateTime_PerformedAction = DateTime.Now
                });

                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                await RefreshDashboardCacheAsync(cancellationToken);
                return MutationResult.Succeeded();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting professor interest for company event {EventRng}", eventRng);
                return MutationResult.Failed("Σφάλμα κατά την υποβολή του ενδιαφέροντος.");
            }
        }

        public async Task<MutationResult> ShowInterestInCompanyThesisAsProfessorAsync(long thesisRng, string professorEmail, CancellationToken cancellationToken = default)
        {
            if (thesisRng <= 0)
            {
                return MutationResult.Failed("Δεν βρέθηκε η πτυχιακή εργασία.");
            }

            if (string.IsNullOrWhiteSpace(professorEmail))
            {
                return MutationResult.Failed("Δεν βρέθηκαν στοιχεία καθηγητή.");
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var normalizedProfessorEmail = professorEmail.Trim().ToLowerInvariant();

                var professor = await context.Professors
                    .FirstOrDefaultAsync(p => p.ProfEmail != null && p.ProfEmail.ToLower() == normalizedProfessorEmail, cancellationToken);

                if (professor == null)
                {
                    return MutationResult.Failed("Δεν βρέθηκαν στοιχεία καθηγητή.");
                }

                var thesis = await context.CompanyTheses
                    .FirstOrDefaultAsync(t => t.RNGForThesisUploadedAsCompany == thesisRng, cancellationToken);

                if (thesis == null)
                {
                    return MutationResult.Failed("Δεν βρέθηκε η πτυχιακή εργασία.");
                }

                if (thesis.IsProfessorInteresetedInCompanyThesis &&
                    !string.IsNullOrWhiteSpace(thesis.ProfessorEmailInterestedInCompanyThesis) &&
                    !string.Equals(thesis.ProfessorEmailInterestedInCompanyThesis, professor.ProfEmail, StringComparison.OrdinalIgnoreCase))
                {
                    return MutationResult.Failed("Η πτυχιακή εργασία δεν είναι πλέον διαθέσιμη. Έχει ήδη εκδηλωθεί ενδιαφέρον από άλλο καθηγητή.");
                }

                var alreadyInterested = thesis.IsProfessorInteresetedInCompanyThesis &&
                                        string.Equals(thesis.ProfessorEmailInterestedInCompanyThesis, professor.ProfEmail, StringComparison.OrdinalIgnoreCase);

                if (alreadyInterested)
                {
                    return MutationResult.Failed("Έχετε ήδη δείξει ενδιαφέρον για αυτήν την πτυχιακή εργασία.");
                }

                thesis.IsProfessorInteresetedInCompanyThesis = true;
                thesis.ProfessorEmailInterestedInCompanyThesis = professor.ProfEmail;
                thesis.IsProfessorInterestedInCompanyThesisStatus = "Έχετε Αποδεχτεί";
                thesis.ProfessorInterested = professor;

                context.CompanyTheses.Update(thesis);
                context.PlatformActions.Add(new PlatformActions
                {
                    UserRole_PerformedAction = "PROFESSOR",
                    ForWhat_PerformedAction = "COMPANY_THESIS",
                    HashedPositionRNG_PerformedAction = HashingHelper.HashLong(thesis.RNGForThesisUploadedAsCompany),
                    TypeOfAction_PerformedAction = "SHOW_INTEREST",
                    DateTime_PerformedAction = DateTime.Now
                });

                await context.SaveChangesAsync(cancellationToken);
                await RefreshDashboardCacheAsync(cancellationToken);
                return MutationResult.Succeeded();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting professor interest for thesis {ThesisRng}", thesisRng);
                return MutationResult.Failed("Σφάλμα κατά την υποβολή του ενδιαφέροντος.");
            }
        }

        // Attachment downloads
        public async Task<AttachmentDownloadResult?> GetThesisAttachmentAsync(int thesisId, CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var thesis = await context.ProfessorTheses
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == thesisId, cancellationToken);

            if (thesis?.ThesisAttachment == null || thesis.ThesisAttachment.Length == 0)
            {
                return null;
            }

            return new AttachmentDownloadResult
            {
                Data = thesis.ThesisAttachment,
                FileName = $"{thesis.ThesisTitle ?? "ProfessorThesis"}_Attachment.pdf",
                ContentType = "application/pdf"
            };
        }

        public async Task<AttachmentDownloadResult?> GetInternshipAttachmentAsync(int internshipId, CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var internship = await context.ProfessorInternships
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == internshipId, cancellationToken);

            if (internship?.ProfessorInternshipAttachment == null || internship.ProfessorInternshipAttachment.Length == 0)
            {
                return null;
            }

            return new AttachmentDownloadResult
            {
                Data = internship.ProfessorInternshipAttachment,
                FileName = $"{internship.ProfessorInternshipTitle ?? "ProfessorInternship"}_Attachment.pdf",
                ContentType = "application/pdf"
            };
        }

        public async Task<AttachmentDownloadResult?> GetEventAttachmentAsync(int eventId, CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var professorEvent = await context.ProfessorEvents
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

            if (professorEvent?.ProfessorEventAttachmentFile == null || professorEvent.ProfessorEventAttachmentFile.Length == 0)
            {
                return null;
            }

            return new AttachmentDownloadResult
            {
                Data = professorEvent.ProfessorEventAttachmentFile,
                FileName = $"{professorEvent.ProfessorEventTitle ?? "ProfessorEvent"}_Attachment.pdf",
                ContentType = "application/pdf"
            };
        }

        // Entity lookups
        public async Task<Professor?> GetProfessorProfileAsync(CancellationToken cancellationToken = default)
        {
            var dashboard = await LoadDashboardDataAsync(cancellationToken);
            return dashboard?.ProfessorProfile;
        }

        public async Task<Student?> GetStudentByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var normalized = email.Trim().ToLowerInvariant();
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Email != null && s.Email.ToLower() == normalized, cancellationToken);
        }

        public async Task<Student?> GetStudentByUniqueIdAsync(string studentUniqueId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(studentUniqueId))
            {
                return null;
            }

            var normalized = studentUniqueId.Trim().ToLowerInvariant();
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Student_UniqueID != null && s.Student_UniqueID.ToLower() == normalized, cancellationToken);
        }

        public async Task<Company?> GetCompanyByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var normalized = email.Trim().ToLowerInvariant();
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CompanyEmail != null && c.CompanyEmail.ToLower() == normalized, cancellationToken);
        }

        public async Task<Company?> GetCompanyByUniqueIdAsync(string companyUniqueId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(companyUniqueId))
            {
                return null;
            }

            var normalized = companyUniqueId.Trim().ToLowerInvariant();
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Company_UniqueID != null && c.Company_UniqueID.ToLower() == normalized, cancellationToken);
        }

        public async Task<ResearchGroupDetails?> GetResearchGroupDetailsAsync(string researchGroupEmail, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(researchGroupEmail))
            {
                return null;
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var normalized = researchGroupEmail.Trim().ToLowerInvariant();

            var researchGroup = await context.ResearchGroups
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ResearchGroupEmail != null &&
                    r.ResearchGroupEmail.ToLower() == normalized, cancellationToken);

            if (researchGroup == null)
            {
                return null;
            }

            // Load related data
            var researchActions = await context.ResearchGroup_ResearchActions
                .AsNoTracking()
                .Where(ra => ra.ResearchGroup_UniqueID == researchGroup.ResearchGroup_UniqueID &&
                    ra.ResearchGroup_ProjectStatus == "Ενεργό")
                .ToListAsync(cancellationToken);

            // Parse faculty members, non-faculty members, and spin-off companies from ResearchGroupDetails field
            var facultyMembers = new List<FacultyMemberInfo>();
            var nonFacultyMembers = new List<NonFacultyMemberInfo>();
            var spinOffCompanies = new List<SpinOffCompanyInfo>();

            // This would need to parse the ResearchGroupDetails field if it contains structured data
            // For now, return basic structure

            return new ResearchGroupDetails
            {
                ResearchGroupEmail = researchGroup.ResearchGroupEmail ?? string.Empty,
                FacultyMembers = facultyMembers,
                NonFacultyMembers = nonFacultyMembers,
                SpinOffCompanies = spinOffCompanies,
                ActiveResearchActionsCount = researchActions.Count,
                PatentsCount = 0 // Patents table not available
            };
        }

        // Calendar operations
        public async Task<(int companyEvents, int professorEvents)> CountPublishedEventsOnDateAsync(DateTime date, CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var dateOnly = date.Date;

            var companyEventsCount = await context.CompanyEvents
                .AsNoTracking()
                .CountAsync(e => e.CompanyEventActiveDate.Date == dateOnly &&
                    e.CompanyEventStatus == "Δημοσιευμένη", cancellationToken);

            var professorEventsCount = await context.ProfessorEvents
                .AsNoTracking()
                .CountAsync(e => e.ProfessorEventActiveDate.Date == dateOnly &&
                    e.ProfessorEventStatus == "Δημοσιευμένη", cancellationToken);

            return (companyEventsCount, professorEventsCount);
        }

        public async Task<IReadOnlyList<CompanyEvent>> GetCompanyEventsForMonthAsync(int year, int month, CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            return await context.CompanyEvents
                .Include(e => e.Company)
                .AsNoTracking()
                .Where(e => e.CompanyEventActiveDate >= startDate &&
                    e.CompanyEventActiveDate < endDate &&
                    e.CompanyEventStatus == "Δημοσιευμένη")
                .OrderBy(e => e.CompanyEventActiveDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ProfessorEvent>> GetProfessorEventsForMonthAsync(int year, int month, CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            return await context.ProfessorEvents
                .AsNoTracking()
                .Where(e => e.ProfessorEventActiveDate >= startDate &&
                    e.ProfessorEventActiveDate < endDate &&
                    e.ProfessorEventStatus == "Δημοσιευμένη")
                .OrderBy(e => e.ProfessorEventActiveDate)
                .ToListAsync(cancellationToken);
        }

        // Private helper methods
        private async Task<ProfessorDashboardData> BuildDashboardDataAsync(string email, CancellationToken cancellationToken)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var normalizedEmail = email.ToLowerInvariant();

            var professor = await context.Professors
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProfEmail != null && p.ProfEmail.ToLower() == normalizedEmail, cancellationToken);

            if (professor == null)
            {
                return new ProfessorDashboardData
                {
                    IsAuthenticated = true,
                    IsRegisteredProfessor = false,
                    Email = email,
                    LastRefreshedUtc = DateTimeOffset.UtcNow,
                    Lookups = await BuildLookupsAsync(context, cancellationToken)
                };
            }

            var theses = await context.ProfessorTheses
                .AsNoTracking()
                .Where(t => t.ProfessorEmailUsedToUploadThesis != null &&
                    t.ProfessorEmailUsedToUploadThesis.ToLower() == normalizedEmail)
                .OrderByDescending(t => t.ThesisUploadDateTime)
                .ToListAsync(cancellationToken);

            var internships = await context.ProfessorInternships
                .AsNoTracking()
                .Where(i => i.ProfessorEmailUsedToUploadInternship != null &&
                    i.ProfessorEmailUsedToUploadInternship.ToLower() == normalizedEmail)
                .OrderByDescending(i => i.ProfessorInternshipUploadDate)
                .ToListAsync(cancellationToken);

            var events = await context.ProfessorEvents
                .AsNoTracking()
                .Where(e => e.ProfessorEmailUsedToUploadEvent != null &&
                    e.ProfessorEmailUsedToUploadEvent.ToLower() == normalizedEmail)
                .OrderByDescending(e => e.ProfessorEventUploadedDate)
                .ToListAsync(cancellationToken);

            var announcements = await context.AnnouncementsAsProfessor
                .AsNoTracking()
                .Where(a => a.ProfessorAnnouncementProfessorEmail != null &&
                    a.ProfessorAnnouncementProfessorEmail.ToLower() == normalizedEmail)
                .OrderByDescending(a => a.ProfessorAnnouncementUploadDate)
                .ToListAsync(cancellationToken);

            var thesisApplications = await context.ProfessorThesesApplied
                .AsNoTracking()
                .Where(a => a.ProfessorEmailWhereStudentAppliedForProfessorThesis != null &&
                    a.ProfessorEmailWhereStudentAppliedForProfessorThesis.ToLower() == normalizedEmail)
                .OrderByDescending(a => a.DateTimeStudentAppliedForProfessorThesis)
                .ToListAsync(cancellationToken);

            var internshipApplications = await context.ProfessorInternshipsApplied
                .AsNoTracking()
                .Where(a => a.ProfessorEmailWhereStudentAppliedForInternship != null &&
                    a.ProfessorEmailWhereStudentAppliedForInternship.ToLower() == normalizedEmail)
                .OrderByDescending(a => a.DateTimeStudentAppliedForProfessorInternship)
                .ToListAsync(cancellationToken);

            // Load company events and theses for professor to view
            var companyEvents = await context.CompanyEvents
                .AsNoTracking()
                .Where(e => e.CompanyEventStatus == "Δημοσιευμένη")
                .OrderByDescending(e => e.CompanyEventActiveDate)
                .Take(50)
                .ToListAsync(cancellationToken);

            var companyTheses = await context.CompanyTheses
                .AsNoTracking()
                .Where(t => t.CompanyThesisStatus == "Δημοσιευμένη")
                .OrderByDescending(t => t.CompanyThesisUploadDateTime)
                .Take(50)
                .ToListAsync(cancellationToken);

            // Load event interests
            var studentEventInterests = await context.InterestInProfessorEvents
                .AsNoTracking()
                .Where(i => i.ProfessorEmailWhereStudentShowedInterest != null &&
                    i.ProfessorEmailWhereStudentShowedInterest.ToLower() == normalizedEmail)
                .ToListAsync(cancellationToken);

            var companyEventInterests = await context.InterestInProfessorEventsAsCompany
                .AsNoTracking()
                .Where(i => i.ProfessorDetails != null &&
                    i.ProfessorDetails.ProfessorEmailWhereCompanyShowInterestForProfessorEvent != null &&
                    i.ProfessorDetails.ProfessorEmailWhereCompanyShowInterestForProfessorEvent.ToLower() == normalizedEmail)
                .ToListAsync(cancellationToken);

            return new ProfessorDashboardData
            {
                IsAuthenticated = true,
                IsRegisteredProfessor = true,
                Email = email,
                ProfessorProfile = professor,
                Theses = theses,
                Internships = internships,
                Events = events,
                Announcements = announcements,
                ThesisApplications = thesisApplications,
                InternshipApplications = internshipApplications,
                StudentEventInterests = studentEventInterests,
                CompanyEventInterests = companyEventInterests,
                CompanyEvents = companyEvents,
                CompanyTheses = companyTheses,
                LastRefreshedUtc = DateTimeOffset.UtcNow,
                Lookups = await BuildLookupsAsync(context, cancellationToken)
            };
        }

        private async Task<ProfessorDashboardLookups> BuildLookupsAsync(AppDbContext context, CancellationToken cancellationToken)
        {
            var areas = await context.Areas.AsNoTracking().ToListAsync(cancellationToken);
            var skills = await context.Skills.AsNoTracking().ToListAsync(cancellationToken);
            var companies = await context.Companies.AsNoTracking().Take(100).ToListAsync(cancellationToken);
            var researchGroups = await context.ResearchGroups.AsNoTracking().Take(100).ToListAsync(cancellationToken);

            return new ProfessorDashboardLookups
            {
                Areas = areas,
                Skills = skills,
                Companies = companies,
                ResearchGroups = researchGroups
            };
        }

        private async Task<string?> ResolveProfessorEmailAsync(CancellationToken cancellationToken)
        {
            try
            {
                var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (user?.Identity?.IsAuthenticated != true)
                {
                    return null;
                }

                var email = user.FindFirst("name")?.Value ??
                            user.FindFirst(ClaimTypes.Email)?.Value ??
                            user.Identity?.Name;

                return email?.Trim().ToLowerInvariant();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to resolve the current professor email.");
                return null;
            }
        }
    }
}

