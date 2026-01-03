using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using QuizManager.Data;
using QuizManager.Models;

namespace QuizManager.Services.CompanyDashboard
{
    public class CompanyDashboardService : ICompanyDashboardService
    {
        private const string DashboardCachePrefix = "company-dashboard:";
        private const string LookupCachePrefix = "company-lookups:";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<CompanyDashboardService> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly SemaphoreSlim _dashboardLock = new(1, 1);
        private readonly SemaphoreSlim _lookupLock = new(1, 1);

        private static readonly Dictionary<string, IReadOnlyList<string>> DefaultRegionToTownsMap =
            new()
            {
                ["Ανατολική Μακεδονία και Θράκη"] = new List<string> { "Κομοτηνή", "Αλεξανδρούπολη", "Καβάλα", "Ξάνθη", "Δράμα", "Ορεστιάδα", "Διδυμότειχο", "Ίασμος", "Νέα Βύσσα", "Φέρες" },
                ["Κεντρική Μακεδονία"] = new List<string> { "Θεσσαλονίκη", "Κατερίνη", "Σέρρες", "Κιλκίς", "Πολύγυρος", "Ναούσα", "Έδεσσα", "Γιαννιτσά", "Καβάλα", "Άμφισσα" },
                ["Δυτική Μακεδονία"] = new List<string> { "Κοζάνη", "Φλώρινα", "Καστοριά", "Γρεβενά" },
                ["Ήπειρος"] = new List<string> { "Ιωάννινα", "Άρτα", "Πρέβεζα", "Ηγουμενίτσα" },
                ["Θεσσαλία"] = new List<string> { "Λάρισα", "Βόλος", "Τρίκαλα", "Καρδίτσα" },
                ["Ιόνια Νησιά"] = new List<string> { "Κέρκυρα", "Λευκάδα", "Κεφαλονιά", "Ζάκυνθος", "Ιθάκη", "Παξοί", "Κυθήρα" },
                ["Δυτική Ελλάδα"] = new List<string> { "Πάτρα", "Μεσολόγγι", "Αμφιλοχία", "Πύργος", "Αίγιο", "Ναύπακτος" },
                ["Κεντρική Ελλάδα"] = new List<string> { "Λαμία", "Χαλκίδα", "Λιβαδειά", "Θήβα", "Αλιάρτος", "Αμφίκλεια" },
                ["Αττική"] = new List<string> { "Αθήνα", "Πειραιάς", "Κηφισιά", "Παλλήνη", "Αγία Παρασκευή", "Χαλάνδρι", "Καλλιθέα", "Γλυφάδα", "Περιστέρι", "Αιγάλεω" },
                ["Πελοπόννησος"] = new List<string> { "Πάτρα", "Τρίπολη", "Καλαμάτα", "Κόρινθος", "Άργος", "Ναύπλιο", "Σπάρτη", "Κυπαρισσία", "Πύργος", "Μεσσήνη" },
                ["Βόρειο Αιγαίο"] = new List<string> { "Μυτιλήνη", "Χίος", "Λήμνος", "Σάμος", "Ίκαρος", "Λέσβος", "Θάσος", "Σκύρος", "Ψαρά" },
                ["Νότιο Αιγαίο"] = new List<string> { "Ρόδος", "Κως", "Κάρπαθος", "Σαντορίνη", "Μύκονος", "Νάξος", "Πάρος", "Σύρος", "Άνδρος" },
                ["Κρήτη"] = new List<string> { "Ηράκλειο", "Χανιά", "Ρέθυμνο", "Άγιος Νικόλαος", "Ιεράπετρα", "Σητεία", "Κίσαμος", "Παλαιόχωρα", "Αρχάνες", "Ανώγεια" }
            };

        public CompanyDashboardService(
            AuthenticationStateProvider authenticationStateProvider,
            IDbContextFactory<AppDbContext> dbContextFactory,
            ILogger<CompanyDashboardService> logger,
            IMemoryCache memoryCache)
        {
            _authenticationStateProvider = authenticationStateProvider;
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public async Task<CompanyDashboardData> LoadDashboardDataAsync(CancellationToken cancellationToken = default)
        {
            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return CompanyDashboardData.Empty;
            }

            if (_memoryCache.TryGetValue(DashboardCachePrefix + email, out CompanyDashboardData cached))
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
            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return;
            }

            _memoryCache.Remove(DashboardCachePrefix + email);
            _memoryCache.Remove(LookupCachePrefix + email);
        }

        public async Task<CompanyDashboardLookups> GetLookupsAsync(CancellationToken cancellationToken = default)
        {
            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return CompanyDashboardLookups.Empty;
            }

            if (_memoryCache.TryGetValue(LookupCachePrefix + email, out CompanyDashboardLookups cached))
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

        // Search methods
        public async Task<IReadOnlyList<Professor>> SearchProfessorsAsync(ProfessorSearchFilter filter, CancellationToken cancellationToken = default)
        {
            filter ??= new ProfessorSearchFilter();
            var maxResults = Math.Clamp(filter.MaxResults, 1, 100);

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var query = context.Professors.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                var name = filter.Name.Trim().ToLowerInvariant();
                query = query.Where(p => p.ProfName != null && p.ProfName.ToLower().Contains(name));
            }

            if (!string.IsNullOrWhiteSpace(filter.Surname))
            {
                var surname = filter.Surname.Trim().ToLowerInvariant();
                query = query.Where(p => p.ProfSurname != null && p.ProfSurname.ToLower().Contains(surname));
            }

            if (!string.IsNullOrWhiteSpace(filter.Department))
            {
                var department = filter.Department.Trim().ToLowerInvariant();
                query = query.Where(p => p.ProfDepartment != null && p.ProfDepartment.ToLower().Contains(department));
            }

            if (!string.IsNullOrWhiteSpace(filter.School))
            {
                var school = filter.School.Trim().ToLowerInvariant();
                query = query.Where(p => p.ProfSchool != null && p.ProfSchool.ToLower().Contains(school));
            }

            if (!string.IsNullOrWhiteSpace(filter.AreasOfInterest))
            {
                var areas = filter.AreasOfInterest.Trim().ToLowerInvariant();
                query = query.Where(p => p.ProfGeneralFieldOfWork != null && p.ProfGeneralFieldOfWork.ToLower().Contains(areas));
            }

            return await query
                .OrderBy(p => p.ProfSurname)
                .ThenBy(p => p.ProfName)
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

            return suggestions
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .Take(50)
                .ToList();
        }

        public async Task<IReadOnlyList<string>> SearchSkillsAsync(string term, CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var query = context.Skills
                .AsNoTracking()
                .Select(s => s.SkillName);

            if (!string.IsNullOrWhiteSpace(term))
            {
                var normalized = term.Trim().ToLowerInvariant();
                query = query.Where(s => s.ToLower().Contains(normalized));
            }

            return await query
                .Distinct()
                .OrderBy(s => s)
                .Take(50)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<string>> SearchCompanyNamesAsync(string term, CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var query = context.Companies
                .AsNoTracking()
                .Select(c => c.CompanyName);

            if (!string.IsNullOrWhiteSpace(term))
            {
                var normalized = term.Trim().ToLowerInvariant();
                query = query.Where(name => name.ToLower().Contains(normalized));
            }

            return await query
                .Where(name => name != null)
                .Distinct()
                .OrderBy(name => name)
                .Take(50)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Student>> SearchStudentsAsync(StudentSearchFilter filter, CancellationToken cancellationToken = default)
        {
            filter ??= new StudentSearchFilter();
            var maxResults = Math.Clamp(filter.MaxResults, 1, 100);

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var query = context.Students.AsNoTracking();

            var hasName = !string.IsNullOrWhiteSpace(filter.Name);
            var hasSurname = !string.IsNullOrWhiteSpace(filter.Surname);

            if (hasName && hasSurname &&
                string.Equals(filter.Name!.Trim(), filter.Surname!.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                var term = filter.Name.Trim().ToLowerInvariant();
                query = query.Where(s =>
                    s.Name.ToLower().Contains(term) ||
                    s.Surname.ToLower().Contains(term));
            }
            else
            {
                if (hasName)
                {
                    var name = filter.Name!.Trim().ToLowerInvariant();
                    query = query.Where(s => s.Name.ToLower().Contains(name));
                }

                if (hasSurname)
                {
                    var surname = filter.Surname!.Trim().ToLowerInvariant();
                    query = query.Where(s => s.Surname.ToLower().Contains(surname));
                }
            }

            if (!string.IsNullOrWhiteSpace(filter.RegistrationNumber) && long.TryParse(filter.RegistrationNumber, out var regNumber))
            {
                query = query.Where(s => s.RegNumber == regNumber);
            }

            if (!string.IsNullOrWhiteSpace(filter.Department))
            {
                var department = filter.Department.Trim().ToLowerInvariant();
                query = query.Where(s => s.Department.ToLower().Contains(department));
            }

            if (!string.IsNullOrWhiteSpace(filter.School))
            {
                var school = filter.School.Trim().ToLowerInvariant();
                query = query.Where(s => s.School.ToLower().Contains(school));
            }

            if (!string.IsNullOrWhiteSpace(filter.AreasOfExpertise))
            {
                var areas = filter.AreasOfExpertise.Trim().ToLowerInvariant();
                query = query.Where(s => s.AreasOfExpertise.ToLower().Contains(areas));
            }

            if (!string.IsNullOrWhiteSpace(filter.Keywords))
            {
                var keywords = filter.Keywords.Trim().ToLowerInvariant();
                query = query.Where(s => s.Keywords.ToLower().Contains(keywords));
            }

            if (!string.IsNullOrWhiteSpace(filter.DegreeLevel))
            {
                var degree = filter.DegreeLevel.Trim().ToLowerInvariant();
                query = query.Where(s => s.LevelOfDegree != null && s.LevelOfDegree.ToLower().Contains(degree));
            }

            return await query
                .OrderBy(s => s.Surname)
                .ThenBy(s => s.Name)
                .Take(maxResults)
                .ToListAsync(cancellationToken);
        }

        // Create/Update methods
        public async Task<MutationResult> CreateOrUpdateJobAsync(CompanyJob job, CancellationToken cancellationToken = default)
        {
            if (job == null)
            {
                return MutationResult.Failed("Job payload is required.");
            }

            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return MutationResult.Failed("Unable to resolve company context.");
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var normalizedEmail = email.ToLowerInvariant();

            if (job.Id == 0)
            {
                // Create new
                job.EmailUsedToUploadJobs = normalizedEmail;
                job.UploadDateTime = DateTime.UtcNow;
                if (job.RNGForPositionUploaded == 0)
                {
                    job.RNGForPositionUploaded = new Random().NextInt64();
                    job.RNGForPositionUploaded_HashedAsUniqueID = Data.HashingHelper.HashLong(job.RNGForPositionUploaded);
                }
                context.CompanyJobs.Add(job);
            }
            else
            {
                // Update existing
                var existing = await context.CompanyJobs
                    .FirstOrDefaultAsync(j => j.Id == job.Id && 
                        j.EmailUsedToUploadJobs != null && 
                        j.EmailUsedToUploadJobs.ToLower() == normalizedEmail, cancellationToken);

                if (existing == null)
                {
                    return MutationResult.Failed("Job not found or not owned by this company.");
                }

                job.EmailUsedToUploadJobs = normalizedEmail;
                context.Entry(existing).CurrentValues.SetValues(job);
            }

            await context.SaveChangesAsync(cancellationToken);
            await RefreshDashboardCacheAsync(cancellationToken);
            return MutationResult.Succeeded(job.Id);
        }

        public async Task<MutationResult> CreateOrUpdateInternshipAsync(CompanyInternship internship, CancellationToken cancellationToken = default)
        {
            if (internship == null)
            {
                return MutationResult.Failed("Internship payload is required.");
            }

            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return MutationResult.Failed("Unable to resolve company context.");
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var normalizedEmail = email.ToLowerInvariant();

            if (internship.Id == 0)
            {
                // Create new
                internship.CompanyEmailUsedToUploadInternship = normalizedEmail;
                internship.CompanyInternshipUploadDate = DateTime.UtcNow;
                if (internship.RNGForInternshipUploadedAsCompany == 0)
                {
                    internship.RNGForInternshipUploadedAsCompany = new Random().NextInt64();
                    internship.RNGForInternshipUploadedAsCompany_HashedAsUniqueID = Data.HashingHelper.HashLong(internship.RNGForInternshipUploadedAsCompany);
                }
                context.CompanyInternships.Add(internship);
            }
            else
            {
                // Update existing
                var existing = await context.CompanyInternships
                    .FirstOrDefaultAsync(i => i.Id == internship.Id && 
                        i.CompanyEmailUsedToUploadInternship != null && 
                        i.CompanyEmailUsedToUploadInternship.ToLower() == normalizedEmail, cancellationToken);

                if (existing == null)
                {
                    return MutationResult.Failed("Internship not found or not owned by this company.");
                }

                internship.CompanyEmailUsedToUploadInternship = normalizedEmail;
                context.Entry(existing).CurrentValues.SetValues(internship);
            }

            await context.SaveChangesAsync(cancellationToken);
            await RefreshDashboardCacheAsync(cancellationToken);
            return MutationResult.Succeeded(internship.Id);
        }

        public async Task<MutationResult> CreateOrUpdateThesisAsync(CompanyThesis thesis, CancellationToken cancellationToken = default)
        {
            if (thesis == null)
            {
                return MutationResult.Failed("Thesis payload is required.");
            }

            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return MutationResult.Failed("Unable to resolve company context.");
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var normalizedEmail = email.ToLowerInvariant();

            if (thesis.Id == 0)
            {
                // Create new
                thesis.CompanyEmailUsedToUploadThesis = normalizedEmail;
                thesis.CompanyThesisUploadDateTime = DateTime.UtcNow;
                if (thesis.RNGForThesisUploadedAsCompany == 0)
                {
                    thesis.RNGForThesisUploadedAsCompany = new Random().NextInt64();
                    thesis.RNGForThesisUploadedAsCompany_HashedAsUniqueID = Data.HashingHelper.HashLong(thesis.RNGForThesisUploadedAsCompany);
                }
                context.CompanyTheses.Add(thesis);
            }
            else
            {
                // Update existing
                var existing = await context.CompanyTheses
                    .FirstOrDefaultAsync(t => t.Id == thesis.Id && 
                        t.CompanyEmailUsedToUploadThesis != null && 
                        t.CompanyEmailUsedToUploadThesis.ToLower() == normalizedEmail, cancellationToken);

                if (existing == null)
                {
                    return MutationResult.Failed("Thesis not found or not owned by this company.");
                }

                thesis.CompanyEmailUsedToUploadThesis = normalizedEmail;
                context.Entry(existing).CurrentValues.SetValues(thesis);
            }

            await context.SaveChangesAsync(cancellationToken);
            await RefreshDashboardCacheAsync(cancellationToken);
            return MutationResult.Succeeded(thesis.Id);
        }

        public async Task<MutationResult> CreateOrUpdateAnnouncementAsync(AnnouncementAsCompany announcement, CancellationToken cancellationToken = default)
        {
            if (announcement == null)
            {
                return MutationResult.Failed("Announcement payload is required.");
            }

            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return MutationResult.Failed("Unable to resolve company context.");
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var normalizedEmail = email.ToLowerInvariant();

            if (announcement.Id == 0)
            {
                // Create new
                announcement.CompanyAnnouncementCompanyEmail = normalizedEmail;
                announcement.CompanyAnnouncementUploadDate = DateTime.UtcNow;
                if (announcement.CompanyAnnouncementRNG == null || announcement.CompanyAnnouncementRNG == 0)
                {
                    announcement.CompanyAnnouncementRNG = new Random().NextInt64();
                    announcement.CompanyAnnouncementRNG_HashedAsUniqueID = Data.HashingHelper.HashLong(announcement.CompanyAnnouncementRNG.Value);
                }
                context.AnnouncementsAsCompany.Add(announcement);
            }
            else
            {
                // Update existing
                var existing = await context.AnnouncementsAsCompany
                    .FirstOrDefaultAsync(a => a.Id == announcement.Id && 
                        a.CompanyAnnouncementCompanyEmail != null && 
                        a.CompanyAnnouncementCompanyEmail.ToLower() == normalizedEmail, cancellationToken);

                if (existing == null)
                {
                    return MutationResult.Failed("Announcement not found or not owned by this company.");
                }

                announcement.CompanyAnnouncementCompanyEmail = normalizedEmail;
                context.Entry(existing).CurrentValues.SetValues(announcement);
            }

            await context.SaveChangesAsync(cancellationToken);
            await RefreshDashboardCacheAsync(cancellationToken);
            return MutationResult.Succeeded(announcement.Id);
        }

        public async Task<MutationResult> CreateOrUpdateCompanyEventAsync(CompanyEvent companyEvent, CancellationToken cancellationToken = default)
        {
            if (companyEvent == null)
            {
                return MutationResult.Failed("Event payload is required.");
            }

            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return MutationResult.Failed("Unable to resolve company context.");
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var normalizedEmail = email.ToLowerInvariant();

            if (companyEvent.Id == 0)
            {
                // Create new
                companyEvent.CompanyEmailUsedToUploadEvent = normalizedEmail;
                companyEvent.CompanyEventUploadedDate = DateTime.UtcNow;
                if (companyEvent.RNGForEventUploadedAsCompany == 0)
                {
                    companyEvent.RNGForEventUploadedAsCompany = new Random().NextInt64();
                }
                context.CompanyEvents.Add(companyEvent);
            }
            else
            {
                // Update existing
                var existing = await context.CompanyEvents
                    .FirstOrDefaultAsync(e => e.Id == companyEvent.Id && 
                        e.CompanyEmailUsedToUploadEvent != null && 
                        e.CompanyEmailUsedToUploadEvent.ToLower() == normalizedEmail, cancellationToken);

                if (existing == null)
                {
                    return MutationResult.Failed("Event not found or not owned by this company.");
                }

                companyEvent.CompanyEmailUsedToUploadEvent = normalizedEmail;
                context.Entry(existing).CurrentValues.SetValues(companyEvent);
            }

            await context.SaveChangesAsync(cancellationToken);
            await RefreshDashboardCacheAsync(cancellationToken);
            return MutationResult.Succeeded(companyEvent.Id);
        }

        // Delete methods
        public async Task<bool> DeleteJobAsync(int jobId, CancellationToken cancellationToken = default)
        {
            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var normalizedEmail = email.ToLowerInvariant();

                var job = await context.CompanyJobs
                    .FirstOrDefaultAsync(j => j.Id == jobId && 
                        j.EmailUsedToUploadJobs != null && 
                        j.EmailUsedToUploadJobs.ToLower() == normalizedEmail, cancellationToken);

                if (job == null)
                {
                    return false;
                }

                context.CompanyJobs.Remove(job);
                await context.SaveChangesAsync(cancellationToken);
                await RefreshDashboardCacheAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company job");
                return false;
            }
        }

        public async Task<bool> DeleteInternshipAsync(int internshipId, CancellationToken cancellationToken = default)
        {
            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var normalizedEmail = email.ToLowerInvariant();

                var internship = await context.CompanyInternships
                    .FirstOrDefaultAsync(i => i.Id == internshipId && 
                        i.CompanyEmailUsedToUploadInternship != null && 
                        i.CompanyEmailUsedToUploadInternship.ToLower() == normalizedEmail, cancellationToken);

                if (internship == null)
                {
                    return false;
                }

                context.CompanyInternships.Remove(internship);
                await context.SaveChangesAsync(cancellationToken);
                await RefreshDashboardCacheAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company internship");
                return false;
            }
        }

        public async Task<bool> DeleteThesisAsync(int thesisId, CancellationToken cancellationToken = default)
        {
            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var normalizedEmail = email.ToLowerInvariant();

                var thesis = await context.CompanyTheses
                    .FirstOrDefaultAsync(t => t.Id == thesisId && 
                        t.CompanyEmailUsedToUploadThesis != null && 
                        t.CompanyEmailUsedToUploadThesis.ToLower() == normalizedEmail, cancellationToken);

                if (thesis == null)
                {
                    return false;
                }

                context.CompanyTheses.Remove(thesis);
                await context.SaveChangesAsync(cancellationToken);
                await RefreshDashboardCacheAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company thesis");
                return false;
            }
        }

        public async Task<bool> DeleteAnnouncementAsync(int announcementId, CancellationToken cancellationToken = default)
        {
            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var normalizedEmail = email.ToLowerInvariant();

                var announcement = await context.AnnouncementsAsCompany
                    .FirstOrDefaultAsync(a => a.Id == announcementId && 
                        a.CompanyAnnouncementCompanyEmail != null && 
                        a.CompanyAnnouncementCompanyEmail.ToLower() == normalizedEmail, cancellationToken);

                if (announcement == null)
                {
                    return false;
                }

                context.AnnouncementsAsCompany.Remove(announcement);
                await context.SaveChangesAsync(cancellationToken);
                await RefreshDashboardCacheAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company announcement");
                return false;
            }
        }

        public async Task<bool> DeleteCompanyEventAsync(int eventId, CancellationToken cancellationToken = default)
        {
            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var normalizedEmail = email.ToLowerInvariant();

                var companyEvent = await context.CompanyEvents
                    .FirstOrDefaultAsync(e => e.Id == eventId && 
                        e.CompanyEmailUsedToUploadEvent != null && 
                        e.CompanyEmailUsedToUploadEvent.ToLower() == normalizedEmail, cancellationToken);

                if (companyEvent == null)
                {
                    return false;
                }

                context.CompanyEvents.Remove(companyEvent);
                await context.SaveChangesAsync(cancellationToken);
                await RefreshDashboardCacheAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company event");
                return false;
            }
        }

        public async Task<bool> DeleteThesisApplicationAsync(long thesisApplicationRng, CancellationToken cancellationToken = default)
        {
            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var normalized = email.ToLowerInvariant();

            var application = await context.CompanyThesesApplied
                .FirstOrDefaultAsync(a =>
                        a.RNGForCompanyThesisApplied == thesisApplicationRng &&
                        a.CompanyEmailWhereStudentAppliedForThesis != null &&
                        a.CompanyEmailWhereStudentAppliedForThesis.ToLower() == normalized,
                    cancellationToken);

            if (application == null)
            {
                return false;
            }

            var studentDetails = await context.CompanyThesisApplied_StudentDetails
                .FirstOrDefaultAsync(s =>
                        s.StudentUniqueIDAppliedForThesis == application.StudentUniqueIDAppliedForThesis &&
                        s.StudentEmailAppliedForThesis == application.StudentEmailAppliedForThesis,
                    cancellationToken);

            var companyDetails = await context.CompanyThesisApplied_CompanyDetails
                .FirstOrDefaultAsync(c =>
                        c.CompanyUniqueIDWhereStudentAppliedForThesis == application.CompanyUniqueIDWhereStudentAppliedForThesis &&
                        c.CompanyEmailWhereStudentAppliedForThesis == application.CompanyEmailWhereStudentAppliedForThesis,
                    cancellationToken);

            if (studentDetails != null)
            {
                context.CompanyThesisApplied_StudentDetails.Remove(studentDetails);
            }

            if (companyDetails != null)
            {
                context.CompanyThesisApplied_CompanyDetails.Remove(companyDetails);
            }

            context.CompanyThesesApplied.Remove(application);
            await context.SaveChangesAsync(cancellationToken);
            await RefreshDashboardCacheAsync(cancellationToken);
            return true;
        }

        // Application methods
        public async Task<IReadOnlyList<CompanyJobApplied>> GetJobApplicationsAsync(CancellationToken cancellationToken = default)
        {
            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return Array.Empty<CompanyJobApplied>();
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var normalizedEmail = email.ToLowerInvariant();

            return await context.CompanyJobsApplied
                .Include(a => a.StudentDetails)
                .Include(a => a.CompanyDetails)
                .AsNoTracking()
                .Where(a => a.CompanysEmailWhereStudentAppliedForCompanyJob != null &&
                           a.CompanysEmailWhereStudentAppliedForCompanyJob.ToLower() == normalizedEmail)
                .OrderByDescending(a => a.DateTimeStudentAppliedForCompanyJob)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<InternshipApplied>> GetInternshipApplicationsAsync(CancellationToken cancellationToken = default)
        {
            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return Array.Empty<InternshipApplied>();
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var normalizedEmail = email.ToLowerInvariant();

            return await context.InternshipsApplied
                .Include(a => a.StudentDetails)
                .Include(a => a.CompanyDetails)
                .AsNoTracking()
                .Where(a => a.CompanyEmailWhereStudentAppliedForInternship != null &&
                           a.CompanyEmailWhereStudentAppliedForInternship.ToLower() == normalizedEmail)
                .OrderByDescending(a => a.DateTimeStudentAppliedForInternship)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<CompanyThesisApplied>> GetThesisApplicationsAsync(CancellationToken cancellationToken = default)
        {
            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return Array.Empty<CompanyThesisApplied>();
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var normalizedEmail = email.ToLowerInvariant();

            return await context.CompanyThesesApplied
                .Include(a => a.StudentDetails)
                .Include(a => a.CompanyDetails)
                .AsNoTracking()
                .Where(a => a.CompanyEmailWhereStudentAppliedForThesis != null &&
                           a.CompanyEmailWhereStudentAppliedForThesis.ToLower() == normalizedEmail)
                .OrderByDescending(a => a.DateTimeStudentAppliedForThesis)
                .ToListAsync(cancellationToken);
        }

        public async Task<MutationResult> DecideOnApplicationAsync(ApplicationDecisionRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return MutationResult.Failed("Decision request is required.");
            }

            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return MutationResult.Failed("Unable to resolve company context.");
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var normalized = email.ToLowerInvariant();

            switch (request.ApplicationType)
            {
                case ApplicationType.Job:
                    var jobApplication = await context.CompanyJobsApplied
                        .FirstOrDefaultAsync(a => a.RNGForCompanyJobApplied == request.ApplicationRng &&
                                                  a.StudentUniqueIDAppliedForCompanyJob == request.StudentUniqueId &&
                                                  a.CompanysEmailWhereStudentAppliedForCompanyJob != null &&
                                                  a.CompanysEmailWhereStudentAppliedForCompanyJob.ToLower() == normalized,
                                                  cancellationToken);

                    if (jobApplication == null)
                    {
                        return MutationResult.Failed("Job application not found.");
                    }

                    ApplyDecision(jobApplication, request.Decision);
                    break;

                case ApplicationType.Internship:
                    var internshipApplication = await context.InternshipsApplied
                        .FirstOrDefaultAsync(a => a.RNGForInternshipApplied == request.ApplicationRng &&
                                                  a.StudentUniqueIDAppliedForInternship == request.StudentUniqueId &&
                                                  a.CompanyEmailWhereStudentAppliedForInternship != null &&
                                                  a.CompanyEmailWhereStudentAppliedForInternship.ToLower() == normalized,
                                                  cancellationToken);

                    if (internshipApplication == null)
                    {
                        return MutationResult.Failed("Internship application not found.");
                    }

                    ApplyDecision(internshipApplication, request.Decision);
                    break;

                case ApplicationType.Thesis:
                    var thesisApplication = await context.CompanyThesesApplied
                        .FirstOrDefaultAsync(a => a.RNGForCompanyThesisApplied == request.ApplicationRng &&
                                                  a.StudentUniqueIDAppliedForThesis == request.StudentUniqueId &&
                                                  a.CompanyEmailWhereStudentAppliedForThesis != null &&
                                                  a.CompanyEmailWhereStudentAppliedForThesis.ToLower() == normalized,
                                                  cancellationToken);

                    if (thesisApplication == null)
                    {
                        return MutationResult.Failed("Thesis application not found.");
                    }

                    ApplyDecision(thesisApplication, request.Decision);
                    break;

                default:
                    return MutationResult.Failed("Unsupported application type.");
            }

            await context.SaveChangesAsync(cancellationToken);
            await RefreshDashboardCacheAsync(cancellationToken);
            return MutationResult.Succeeded();
        }

        // Attachment methods
        public async Task<AttachmentDownloadResult?> GetJobAttachmentAsync(int jobId, CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var job = await context.CompanyJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

            if (job?.PositionAttachment == null || job.PositionAttachment.Length == 0)
            {
                return null;
            }

            return new AttachmentDownloadResult
            {
                Data = job.PositionAttachment,
                FileName = $"{job.PositionTitle ?? "CompanyJob"}_Attachment.pdf",
                ContentType = "application/pdf"
            };
        }

        public async Task<AttachmentDownloadResult?> GetInternshipAttachmentAsync(int internshipId, CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var internship = await context.CompanyInternships.AsNoTracking().FirstOrDefaultAsync(i => i.Id == internshipId, cancellationToken);

            if (internship?.CompanyInternshipAttachment == null || internship.CompanyInternshipAttachment.Length == 0)
            {
                return null;
            }

            return new AttachmentDownloadResult
            {
                Data = internship.CompanyInternshipAttachment,
                FileName = $"{internship.CompanyInternshipTitle ?? "CompanyInternship"}_Attachment.pdf",
                ContentType = "application/pdf"
            };
        }

        public async Task<AttachmentDownloadResult?> GetThesisAttachmentAsync(int thesisId, CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var thesis = await context.CompanyTheses.AsNoTracking().FirstOrDefaultAsync(t => t.Id == thesisId, cancellationToken);

            if (thesis?.CompanyThesisAttachmentUpload == null || thesis.CompanyThesisAttachmentUpload.Length == 0)
            {
                return null;
            }

            return new AttachmentDownloadResult
            {
                Data = thesis.CompanyThesisAttachmentUpload,
                FileName = $"{thesis.CompanyThesisTitle ?? "CompanyThesis"}_Attachment.pdf",
                ContentType = "application/pdf"
            };
        }

        // Get entity methods
        public async Task<Company?> GetCompanyProfileAsync(CancellationToken cancellationToken = default)
        {
            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CompanyEmail.ToLower() == email.ToLower(), cancellationToken);
        }

        public async Task<Company?> GetCompanyDetailsForModalAsync(string companyEmail, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(companyEmail))
            {
                return null;
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CompanyEmail.ToLower() == companyEmail.ToLower(), cancellationToken);
        }

        public async Task<Company?> GetCompanyByUniqueIdAsync(string companyUniqueId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(companyUniqueId))
            {
                return null;
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Company_UniqueID == companyUniqueId, cancellationToken);
        }

        public async Task<Company?> GetCompanyByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CompanyEmail.ToLower() == email.ToLower(), cancellationToken);
        }

        public async Task<Student?> GetStudentByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Email.ToLower() == email.ToLower(), cancellationToken);
        }

        public async Task<Student?> GetStudentByUniqueIdAsync(string studentUniqueId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(studentUniqueId))
            {
                return null;
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Student_UniqueID == studentUniqueId, cancellationToken);
        }

        public async Task<Student?> GetStudentByIdAsync(int studentId, CancellationToken cancellationToken = default)
        {
            if (studentId <= 0)
            {
                return null;
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken);
        }

        public async Task<IReadOnlyList<Student>> GetAllStudentsAsync(CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Students
                .AsNoTracking()
                .OrderBy(s => s.Surname)
                .ThenBy(s => s.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<CompanyThesis>> SearchCompanyThesesAsync(CompanyThesisSearchFilter filter, CancellationToken cancellationToken = default)
        {
            filter ??= new CompanyThesisSearchFilter();
            var maxResults = Math.Clamp(filter.MaxResults, 1, 500);

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var query = context.CompanyTheses
                .Include(t => t.Company)
                .AsNoTracking()
                .Where(t =>
                    t.CompanyThesisStatus == "Δημοσιευμένη" &&
                    (t.IsProfessorInterestedInCompanyThesisStatus == "Δεν έχει γίνει Αποδοχή" ||
                     t.IsProfessorInterestedInCompanyThesisStatus == "Έχετε Αποδεχτεί"));

            if (!string.IsNullOrWhiteSpace(filter.CompanyName))
            {
                var name = filter.CompanyName.Trim().ToLowerInvariant();
                query = query.Where(t => t.Company != null &&
                                         t.Company.CompanyName != null &&
                                         t.Company.CompanyName.ToLower().Contains(name));
            }

            if (!string.IsNullOrWhiteSpace(filter.Title))
            {
                var title = filter.Title.Trim().ToLowerInvariant();
                query = query.Where(t => t.CompanyThesisTitle != null &&
                                         t.CompanyThesisTitle.ToLower().Contains(title));
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
                query = query.Where(t => t.CompanyThesisDepartment != null &&
                                         t.CompanyThesisDepartment.ToLower().Contains(department));
            }

            if (filter.EarliestStartDate.HasValue)
            {
                var startDate = filter.EarliestStartDate.Value.Date;
                query = query.Where(t => t.CompanyThesisStartingDate.Date >= startDate);
            }

            var theses = await query
                .OrderByDescending(t => t.CompanyThesisUploadDateTime)
                .Take(maxResults)
                .ToListAsync(cancellationToken);

            var requiredSkills = filter.RequiredSkills?
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList() ?? new List<string>();

            if (requiredSkills.Count > 0)
            {
                theses = theses
                    .Where(t => !string.IsNullOrEmpty(t.CompanyThesisSkillsNeeded) &&
                                requiredSkills.All(skill =>
                                    t.CompanyThesisSkillsNeeded.IndexOf(skill, StringComparison.OrdinalIgnoreCase) >= 0))
                    .ToList();
            }

            return theses;
        }

        public async Task<IReadOnlyList<ProfessorThesis>> SearchProfessorThesesAsync(ProfessorThesisSearchFilter filter, CancellationToken cancellationToken = default)
        {
            filter ??= new ProfessorThesisSearchFilter();
            var maxResults = Math.Clamp(filter.MaxResults, 1, 500);

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var query = context.ProfessorTheses
                .Include(t => t.Professor)
                .AsNoTracking()
                .Where(t => t.ThesisStatus == "Δημοσιευμένη");

            if (!string.IsNullOrWhiteSpace(filter.ProfessorName))
            {
                var name = filter.ProfessorName.Trim().ToLowerInvariant();
                query = query.Where(t => t.Professor != null &&
                                         ((t.Professor.ProfName != null && t.Professor.ProfName.ToLower().Contains(name)) ||
                                          (t.Professor.ProfSurname != null && t.Professor.ProfSurname.ToLower().Contains(name))));
            }

            if (!string.IsNullOrWhiteSpace(filter.ProfessorSurname))
            {
                var surname = filter.ProfessorSurname.Trim().ToLowerInvariant();
                query = query.Where(t => t.Professor != null &&
                                         t.Professor.ProfSurname != null &&
                                         t.Professor.ProfSurname.ToLower().Contains(surname));
            }

            if (!string.IsNullOrWhiteSpace(filter.ThesisTitle))
            {
                var title = filter.ThesisTitle.Trim().ToLowerInvariant();
                query = query.Where(t => t.ThesisTitle != null &&
                                         t.ThesisTitle.ToLower().Contains(title));
            }

            if (filter.EarliestStartDate.HasValue)
            {
                var startDate = filter.EarliestStartDate.Value.Date;
                query = query.Where(t => t.ThesisActivePeriod.Date >= startDate);
            }

            return await query
                .OrderByDescending(t => t.ThesisUploadDateTime)
                .Take(maxResults)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Professor>> GetProfessorsByEmailsAsync(IEnumerable<string> emails, CancellationToken cancellationToken = default)
        {
            var emailList = emails?
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

            if (emailList == null || emailList.Count == 0)
            {
                return Array.Empty<Professor>();
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Professors
                .AsNoTracking()
                .Where(p => p.ProfEmail != null && emailList.Contains(p.ProfEmail.ToLower()))
                .OrderBy(p => p.ProfSurname)
                .ThenBy(p => p.ProfName)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<CompanyThesis>> GetCompanyThesisProfessorInterestsAsync(long thesisRng, CancellationToken cancellationToken = default)
        {
            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return Array.Empty<CompanyThesis>();
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var normalized = email.ToLowerInvariant();

            // This method returns the company thesis if professors have shown interest in it
            // The status field indicates interest: "Έχετε Αποδεχτεί" means a professor has accepted
            return await context.CompanyTheses
                .Include(t => t.Company)
                .AsNoTracking()
                .Where(t =>
                    t.CompanyEmailUsedToUploadThesis != null &&
                    t.CompanyEmailUsedToUploadThesis.ToLower() == normalized &&
                    (t.IsProfessorInterestedInCompanyThesisStatus == "Έχετε Αποδεχτεί" ||
                     t.IsProfessorInterestedInCompanyThesisStatus == "Δεν έχει γίνει Αποδοχή") &&
                    t.RNGForThesisUploadedAsCompany == thesisRng)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<CompanyEventInterestDetails>> GetCompanyEventInterestsAsync(long eventRng, CancellationToken cancellationToken = default)
        {
            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return Array.Empty<CompanyEventInterestDetails>();
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var normalized = email.ToLowerInvariant();

            var interests = await context.InterestInCompanyEvents
                .Include(i => i.StudentDetails)
                .AsNoTracking()
                .Where(i =>
                    i.RNGForCompanyEventInterest == eventRng &&
                    i.CompanyEmailWhereStudentShowedInterest != null &&
                    i.CompanyEmailWhereStudentShowedInterest.ToLower() == normalized)
                .ToListAsync(cancellationToken);

            var studentEmails = interests
                .Select(i => i.StudentEmailShowInterestForEvent?.ToLower())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Distinct()
                .ToList();

            var students = await context.Students
                .AsNoTracking()
                .Where(s => s.Email != null && studentEmails.Contains(s.Email.ToLower()))
                .ToListAsync(cancellationToken);

            var studentMap = students.ToDictionary(s => s.Email.ToLower(), s => s);

            return interests
                .Select(i => new CompanyEventInterestDetails
                {
                    Application = i,
                    Student = i.StudentEmailShowInterestForEvent != null &&
                              studentMap.TryGetValue(i.StudentEmailShowInterestForEvent.ToLower(), out var student)
                        ? student
                        : null
                })
                .ToList();
        }

        public async Task<IReadOnlyList<InterestInCompanyEventAsProfessor>> GetProfessorCompanyEventInterestsAsync(long eventRng, CancellationToken cancellationToken = default)
        {
            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return Array.Empty<InterestInCompanyEventAsProfessor>();
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var normalized = email.ToLowerInvariant();

            return await context.InterestInCompanyEventAsProfessor
                .Include(i => i.ProfessorDetails)
                .AsNoTracking()
                .Where(i =>
                    i.RNGForCompanyEventInterestAsProfessor == eventRng &&
                    i.CompanyDetails != null &&
                    i.CompanyDetails.CompanyEmailWhereProfessorShowInterestForCompanyEvent != null &&
                    i.CompanyDetails.CompanyEmailWhereProfessorShowInterestForCompanyEvent.ToLower() == normalized)
                .OrderByDescending(i => i.DateTimeProfessorShowInterestForCompanyEvent)
                .ToListAsync(cancellationToken);
        }

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

        // Complex operations
        public async Task<ApplicantDetailsResult> LoadApplicantDetailsAsync(ApplicantDetailsRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return new ApplicantDetailsResult { ApplicationType = ApplicationType.Job };
            }

            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return new ApplicantDetailsResult { ApplicationType = request.ApplicationType };
            }

            var normalized = email.ToLowerInvariant();
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var studentEmails = new HashSet<string>();

            switch (request.ApplicationType)
            {
                case ApplicationType.Job:
                    var jobApplications = await context.CompanyJobsApplied
                        .AsNoTracking()
                        .Where(a => a.RNGForCompanyJobApplied == request.ApplicationRng &&
                                    a.CompanysEmailWhereStudentAppliedForCompanyJob != null &&
                                    a.CompanysEmailWhereStudentAppliedForCompanyJob.ToLower() == normalized)
                        .ToListAsync(cancellationToken);

                    foreach (var application in jobApplications)
                    {
                        if (!string.IsNullOrWhiteSpace(application.StudentEmailAppliedForCompanyJob))
                        {
                            studentEmails.Add(application.StudentEmailAppliedForCompanyJob.ToLower());
                        }
                    }

                    var jobStudents = await LoadStudentsByEmailsAsync(context, studentEmails, cancellationToken);
                    return new ApplicantDetailsResult
                    {
                        ApplicationType = ApplicationType.Job,
                        JobApplications = jobApplications,
                        Students = jobStudents
                    };

                case ApplicationType.Internship:
                    var internshipApplications = await context.InternshipsApplied
                        .AsNoTracking()
                        .Where(a => a.RNGForInternshipApplied == request.ApplicationRng &&
                                    a.CompanyEmailWhereStudentAppliedForInternship != null &&
                                    a.CompanyEmailWhereStudentAppliedForInternship.ToLower() == normalized)
                        .ToListAsync(cancellationToken);

                    foreach (var application in internshipApplications)
                    {
                        if (!string.IsNullOrWhiteSpace(application.StudentEmailAppliedForInternship))
                        {
                            studentEmails.Add(application.StudentEmailAppliedForInternship.ToLower());
                        }
                    }

                    var internshipStudents = await LoadStudentsByEmailsAsync(context, studentEmails, cancellationToken);
                    return new ApplicantDetailsResult
                    {
                        ApplicationType = ApplicationType.Internship,
                        InternshipApplications = internshipApplications,
                        Students = internshipStudents
                    };

                case ApplicationType.Thesis:
                    var thesisApplications = await context.CompanyThesesApplied
                        .AsNoTracking()
                        .Where(a => a.RNGForCompanyThesisApplied == request.ApplicationRng &&
                                    a.CompanyEmailWhereStudentAppliedForThesis != null &&
                                    a.CompanyEmailWhereStudentAppliedForThesis.ToLower() == normalized)
                        .ToListAsync(cancellationToken);

                    foreach (var application in thesisApplications)
                    {
                        if (!string.IsNullOrWhiteSpace(application.StudentEmailAppliedForThesis))
                        {
                            studentEmails.Add(application.StudentEmailAppliedForThesis.ToLower());
                        }
                    }

                    var thesisStudents = await LoadStudentsByEmailsAsync(context, studentEmails, cancellationToken);
                    return new ApplicantDetailsResult
                    {
                        ApplicationType = ApplicationType.Thesis,
                        ThesisApplications = thesisApplications,
                        Students = thesisStudents
                    };

                default:
                    return new ApplicantDetailsResult { ApplicationType = request.ApplicationType };
            }
        }

        public async Task<MutationResult> ShowInterestInCompanyEventAsync(long eventRng, string studentEmail, bool needsTransport, string? transportLocation, CancellationToken cancellationToken = default)
        {
            if (eventRng <= 0)
            {
                return MutationResult.Failed("Δεν βρέθηκε η εκδήλωση.");
            }

            if (string.IsNullOrWhiteSpace(studentEmail))
            {
                return MutationResult.Failed("Δεν βρέθηκαν στοιχεία φοιτητή.");
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var normalizedStudentEmail = studentEmail.Trim().ToLowerInvariant();

                var student = await context.Students
                    .FirstOrDefaultAsync(s => s.Email != null && s.Email.ToLower() == normalizedStudentEmail, cancellationToken);

                if (student == null)
                {
                    return MutationResult.Failed("Δεν βρέθηκαν στοιχεία φοιτητή.");
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

                if (needsTransport && string.IsNullOrWhiteSpace(transportLocation))
                {
                    return MutationResult.Failed("Παρακαλώ επιλέξτε μια τοποθεσία μετακίνησης πριν δείξετε ενδιαφέρον.");
                }

                var normalizedCompanyEmail = companyEvent.CompanyEmailUsedToUploadEvent?.ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(normalizedCompanyEmail))
                {
                    return MutationResult.Failed("Δεν βρέθηκαν στοιχεία εταιρείας.");
                }

                var existingInterest = await context.InterestInCompanyEvents
                    .FirstOrDefaultAsync(i =>
                        i.StudentEmailShowInterestForEvent != null &&
                        i.StudentEmailShowInterestForEvent.ToLower() == normalizedStudentEmail &&
                        i.RNGForCompanyEventInterest == eventRng, cancellationToken);

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

                var interest = new InterestInCompanyEvent
                {
                    RNGForCompanyEventInterest = companyEvent.RNGForEventUploadedAsCompany,
                    RNGForCompanyEventInterest_HashedAsUniqueID = companyEvent.RNGForEventUploadedAsCompany_HashedAsUniqueID,
                    DateTimeStudentShowInterest = DateTime.Now,
                    CompanyEventStatusAtStudentSide = "Έχετε Δείξει Ενδιαφέρον",
                    CompanyEventStatusAtCompanySide = "Προς Επεξεργασία",
                    CompanyEmailWhereStudentShowedInterest = companyEvent.CompanyEmailUsedToUploadEvent,
                    CompanyUniqueIDWhereStudentShowedInterest = company.Company_UniqueID,
                    StudentEmailShowInterestForEvent = student.Email,
                    StudentUniqueIDShowInterestForEvent = student.Student_UniqueID,
                    StudentTransportNeedWhenShowInterestForCompanyEvent = needsTransport ? "Ναι" : "Όχι",
                    StudentTransportChosenLocationWhenShowInterestForCompanyEvent = needsTransport ? transportLocation : null,
                    StudentDetails = new InterestInCompanyEvent_StudentDetails
                    {
                        StudentUniqueIDShowInterestForCompanyEvent = student.Student_UniqueID,
                        StudentEmailShowInterestForCompanyEvent = student.Email,
                        DateTimeStudentShowInterestForCompanyEvent = DateTime.Now,
                        RNGForCompanyEventShowInterestAsStudent_HashedAsUniqueID = companyEvent.RNGForEventUploadedAsCompany_HashedAsUniqueID ?? HashingHelper.HashLong(companyEvent.RNGForEventUploadedAsCompany)
                    },
                    CompanyDetails = new InterestInCompanyEvent_CompanyDetails
                    {
                        CompanyUniqueIDWhereStudentShowInterestForCompanyEvent = company.Company_UniqueID,
                        CompanyEmailWhereStudentShowInterestForCompanyEvent = companyEvent.CompanyEmailUsedToUploadEvent
                    }
                };

                context.InterestInCompanyEvents.Add(interest);
                context.PlatformActions.Add(new PlatformActions
                {
                    UserRole_PerformedAction = "STUDENT",
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
                _logger.LogError(ex, "Error submitting student interest for company event {EventRng}", eventRng);
                return MutationResult.Failed("Σφάλμα κατά την υποβολή του ενδιαφέροντος.");
            }
        }

        public async Task<MutationResult> ShowInterestInCompanyEventAsProfessorAsync(long eventRng, string professorEmail, CancellationToken cancellationToken = default)
        {
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

        public async Task<MutationResult> ShowInterestInProfessorEventAsCompanyAsync(long professorEventRng, int attendeeCount, CancellationToken cancellationToken = default)
        {
            if (professorEventRng <= 0)
            {
                return MutationResult.Failed("Δεν βρέθηκε η εκδήλωση.");
            }

            if (attendeeCount <= 0)
            {
                return MutationResult.Failed("Παρακαλώ επιλέξτε αριθμό ατόμων πριν δείξετε ενδιαφέρον.");
            }

            var companyEmail = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(companyEmail))
            {
                return MutationResult.Failed("Δεν βρέθηκαν στοιχεία εταιρείας.");
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var normalizedCompanyEmail = companyEmail.ToLowerInvariant();

                var company = await context.Companies
                    .FirstOrDefaultAsync(c => c.CompanyEmail != null && c.CompanyEmail.ToLower() == normalizedCompanyEmail, cancellationToken);

                if (company == null)
                {
                    return MutationResult.Failed("Δεν βρέθηκαν στοιχεία εταιρείας.");
                }

                var professorEvent = await context.ProfessorEvents
                    .Include(e => e.Professor)
                    .FirstOrDefaultAsync(e => e.RNGForEventUploadedAsProfessor == professorEventRng, cancellationToken);

                if (professorEvent == null)
                {
                    return MutationResult.Failed("Δεν βρέθηκε η εκδήλωση.");
                }

                if (!string.Equals(professorEvent.ProfessorEventStatus, "Δημοσιευμένη", StringComparison.OrdinalIgnoreCase))
                {
                    return MutationResult.Failed("Η Εκδήλωση έχει Αποδημοσιευτεί. Παρακαλώ δοκιμάστε αργότερα.");
                }

                var professorEmail = professorEvent.ProfessorEmailUsedToUploadEvent?.ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(professorEmail))
                {
                    return MutationResult.Failed("Δεν βρέθηκαν στοιχεία καθηγητή.");
                }

                var professor = professorEvent.Professor ?? await context.Professors
                    .FirstOrDefaultAsync(p => p.ProfEmail != null && p.ProfEmail.ToLower() == professorEmail, cancellationToken);

                if (professor == null)
                {
                    return MutationResult.Failed("Δεν βρέθηκαν στοιχεία καθηγητή.");
                }

                var existingInterest = await context.InterestInProfessorEventsAsCompany
                    .FirstOrDefaultAsync(i =>
                        i.CompanyEmailShowInterestForProfessorEvent != null &&
                        i.CompanyEmailShowInterestForProfessorEvent.ToLower() == normalizedCompanyEmail &&
                        i.RNGForProfessorEventInterestAsCompany == professorEventRng, cancellationToken);

                if (existingInterest != null)
                {
                    return MutationResult.Failed("Έχετε ήδη δείξει ενδιαφέρον για αυτήν την εκδήλωση.");
                }

                await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

                var interest = new InterestInProfessorEventAsCompany
                {
                    RNGForProfessorEventInterestAsCompany = professorEvent.RNGForEventUploadedAsProfessor,
                    RNGForProfessorEventInterestAsCompany_HashedAsUniqueID = professorEvent.RNGForEventUploadedAsProfessor_HashedAsUniqueID ?? HashingHelper.HashLong(professorEvent.RNGForEventUploadedAsProfessor),
                    DateTimeCompanyShowInterestForProfessorEvent = DateTime.Now,
                    ProfessorEventStatus_ShowInterestAsCompany_AtCompanySide = "Έχετε Δείξει Ενδιαφέρον",
                    ProfessorEventStatus_ShowInterestAsCompany_AtProfessorSide = "Προς Επεξεργασία",
                    ProfessorEmailWhereCompanyShowedInterest = professor.ProfEmail,
                    ProfessorUniqueIDWhereCompanyShowedInterest = professor.Professor_UniqueID,
                    CompanyEmailShowInterestForProfessorEvent = company.CompanyEmail,
                    CompanyUniqueIDShowInterestForProfessorEvent = company.Company_UniqueID,
                    CompanyNumberOfPeopleToShowUpWhenShowInterestForProfessorEvent = attendeeCount.ToString(),
                    CompanyDetails = new InterestInProfessorEventAsCompany_CompanyDetails
                    {
                        CompanyUniqueIDShowInterestForProfessorEvent = company.Company_UniqueID,
                        CompanyEmailShowInterestForProfessorEvent = company.CompanyEmail,
                        DateTimeCompanyShowInterestForProfessorEvent = DateTime.Now,
                        RNGForProfessorEventShowInterestAsCompany_HashedAsUniqueID = professorEvent.RNGForEventUploadedAsProfessor_HashedAsUniqueID ?? HashingHelper.HashLong(professorEvent.RNGForEventUploadedAsProfessor)
                    },
                    ProfessorDetails = new InterestInProfessorEventAsCompany_ProfessorDetails
                    {
                        ProfessorUniqueIDWhereCompanyShowInterestForProfessorEvent = professor.Professor_UniqueID,
                        ProfessorEmailWhereCompanyShowInterestForProfessorEvent = professor.ProfEmail
                    }
                };

                context.InterestInProfessorEventsAsCompany.Add(interest);
                context.PlatformActions.Add(new PlatformActions
                {
                    UserRole_PerformedAction = "COMPANY",
                    ForWhat_PerformedAction = "PROFESSOR_EVENT",
                    HashedPositionRNG_PerformedAction = HashingHelper.HashLong(professorEvent.RNGForEventUploadedAsProfessor),
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
                _logger.LogError(ex, "Error submitting company interest for professor event {EventRng}", professorEventRng);
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

        public async Task<MutationResult> ShowInterestInProfessorThesisAsync(long professorThesisRng, CancellationToken cancellationToken = default)
        {
            if (professorThesisRng <= 0)
            {
                return MutationResult.Failed("Δεν βρέθηκε η πτυχιακή εργασία.");
            }

            var companyEmail = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(companyEmail))
            {
                return MutationResult.Failed("Δεν βρέθηκαν στοιχεία εταιρείας.");
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var normalizedEmail = companyEmail.ToLowerInvariant();

                var company = await context.Companies
                    .FirstOrDefaultAsync(c => c.CompanyEmail != null && c.CompanyEmail.ToLower() == normalizedEmail, cancellationToken);

                if (company == null)
                {
                    return MutationResult.Failed("Δεν βρέθηκαν στοιχεία εταιρείας.");
                }

                var thesis = await context.ProfessorTheses
                    .FirstOrDefaultAsync(t => t.RNGForThesisUploaded == professorThesisRng, cancellationToken);

                if (thesis == null)
                {
                    return MutationResult.Failed("Δεν βρέθηκε η πτυχιακή εργασία.");
                }

                if (thesis.IsCompanyInteresetedInProfessorThesis &&
                    !string.IsNullOrWhiteSpace(thesis.CompanyEmailInterestedInProfessorThesis) &&
                    !string.Equals(thesis.CompanyEmailInterestedInProfessorThesis, company.CompanyEmail, StringComparison.OrdinalIgnoreCase))
                {
                    return MutationResult.Failed("Η πτυχιακή εργασία δεν είναι πλέον διαθέσιμη. Έχει ήδη εκδηλωθεί ενδιαφέρον από άλλη εταιρεία.");
                }

                var alreadyInterested = thesis.IsCompanyInteresetedInProfessorThesis &&
                                        string.Equals(thesis.CompanyEmailInterestedInProfessorThesis, company.CompanyEmail, StringComparison.OrdinalIgnoreCase);

                if (alreadyInterested)
                {
                    return MutationResult.Failed("Έχετε ήδη δείξει ενδιαφέρον για αυτήν την πτυχιακή εργασία.");
                }

                thesis.IsCompanyInteresetedInProfessorThesis = true;
                thesis.CompanyEmailInterestedInProfessorThesis = company.CompanyEmail;
                thesis.IsCompanyInterestedInProfessorThesisStatus = "Έχετε Αποδεχτεί";
                thesis.CompanyInterested = company;

                context.ProfessorTheses.Update(thesis);
                context.PlatformActions.Add(new PlatformActions
                {
                    UserRole_PerformedAction = "COMPANY",
                    ForWhat_PerformedAction = "PROFESSOR_THESIS",
                    HashedPositionRNG_PerformedAction = HashingHelper.HashLong(thesis.RNGForThesisUploaded),
                    TypeOfAction_PerformedAction = "SHOW_INTEREST",
                    DateTime_PerformedAction = DateTime.Now
                });

                await context.SaveChangesAsync(cancellationToken);
                await RefreshDashboardCacheAsync(cancellationToken);
                return MutationResult.Succeeded(thesis.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting company interest for professor thesis {ThesisRng}", professorThesisRng);
                return MutationResult.Failed("Σφάλμα κατά την υποβολή του ενδιαφέροντος.");
            }
        }

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

        public async Task<Professor?> GetProfessorByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Professors
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProfEmail != null && p.ProfEmail.ToLower() == email.ToLower(), cancellationToken);
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

        public async Task<CompanyJob?> GetJobByRngAsync(long rngForPositionUploaded, CancellationToken cancellationToken = default)
        {
            if (rngForPositionUploaded <= 0)
            {
                return null;
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.CompanyJobs
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.RNGForPositionUploaded == rngForPositionUploaded, cancellationToken);
        }

        public async Task<AttachmentDownloadResult?> GetProfessorThesisAttachmentAsync(int professorThesisId, CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var thesis = await context.ProfessorTheses
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == professorThesisId, cancellationToken);

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

        // Status update methods
        public async Task<MutationResult> UpdateJobStatusAsync(int jobId, string newStatus, CancellationToken cancellationToken = default)
        {
            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return MutationResult.Failed("Unable to resolve company context.");
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var normalizedEmail = email.ToLowerInvariant();

                var job = await context.CompanyJobs
                    .FirstOrDefaultAsync(j => j.Id == jobId && 
                        j.EmailUsedToUploadJobs != null && 
                        j.EmailUsedToUploadJobs.ToLower() == normalizedEmail, cancellationToken);

                if (job == null)
                {
                    return MutationResult.Failed("Job not found or not owned by this company.");
                }

                job.PositionStatus = newStatus;
                await context.SaveChangesAsync(cancellationToken);
                await RefreshDashboardCacheAsync(cancellationToken);
                return MutationResult.Succeeded(jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job status");
                return MutationResult.Failed($"Error: {ex.Message}");
            }
        }

        public async Task<MutationResult> UpdateInternshipStatusAsync(int internshipId, string newStatus, CancellationToken cancellationToken = default)
        {
            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return MutationResult.Failed("Unable to resolve company context.");
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var normalizedEmail = email.ToLowerInvariant();

                var internship = await context.CompanyInternships
                    .FirstOrDefaultAsync(i => i.Id == internshipId && 
                        i.CompanyEmailUsedToUploadInternship != null && 
                        i.CompanyEmailUsedToUploadInternship.ToLower() == normalizedEmail, cancellationToken);

                if (internship == null)
                {
                    return MutationResult.Failed("Internship not found or not owned by this company.");
                }

                internship.CompanyUploadedInternshipStatus = newStatus;
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

        public async Task<MutationResult> UpdateThesisStatusAsync(int thesisId, string newStatus, CancellationToken cancellationToken = default)
        {
            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return MutationResult.Failed("Unable to resolve company context.");
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var normalizedEmail = email.ToLowerInvariant();

                var thesis = await context.CompanyTheses
                    .FirstOrDefaultAsync(t => t.Id == thesisId && 
                        t.CompanyEmailUsedToUploadThesis != null && 
                        t.CompanyEmailUsedToUploadThesis.ToLower() == normalizedEmail, cancellationToken);

                if (thesis == null)
                {
                    return MutationResult.Failed("Thesis not found or not owned by this company.");
                }

                thesis.CompanyThesisStatus = newStatus;
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

        public async Task<MutationResult> UpdateAnnouncementStatusAsync(int announcementId, string newStatus, CancellationToken cancellationToken = default)
        {
            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return MutationResult.Failed("Unable to resolve company context.");
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var normalizedEmail = email.ToLowerInvariant();

                var announcement = await context.AnnouncementsAsCompany
                    .FirstOrDefaultAsync(a => a.Id == announcementId && 
                        a.CompanyAnnouncementCompanyEmail != null && 
                        a.CompanyAnnouncementCompanyEmail.ToLower() == normalizedEmail, cancellationToken);

                if (announcement == null)
                {
                    return MutationResult.Failed("Announcement not found or not owned by this company.");
                }

                announcement.CompanyAnnouncementStatus = newStatus;
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

        public async Task<MutationResult> UpdateCompanyEventStatusAsync(int companyEventId, string newStatus, CancellationToken cancellationToken = default)
        {
            var email = await ResolveCompanyEmailAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                return MutationResult.Failed("Unable to resolve company context.");
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var normalizedEmail = email.ToLowerInvariant();

                var companyEvent = await context.CompanyEvents
                    .FirstOrDefaultAsync(e => e.Id == companyEventId && 
                        e.CompanyEmailUsedToUploadEvent != null && 
                        e.CompanyEmailUsedToUploadEvent.ToLower() == normalizedEmail, cancellationToken);

                if (companyEvent == null)
                {
                    return MutationResult.Failed("Event not found or not owned by this company.");
                }

                companyEvent.CompanyEventStatus = newStatus;
                await context.SaveChangesAsync(cancellationToken);
                await RefreshDashboardCacheAsync(cancellationToken);
                return MutationResult.Succeeded(companyEventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company event status");
                return MutationResult.Failed($"Error: {ex.Message}");
            }
        }

        // Private helper methods
        private async Task<CompanyDashboardData> BuildDashboardDataAsync(string email, CancellationToken cancellationToken)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var normalizedEmail = email.ToLowerInvariant();

            var company = await context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CompanyEmail.ToLower() == normalizedEmail, cancellationToken);

            if (company == null)
            {
                return new CompanyDashboardData
                {
                    IsAuthenticated = true,
                    IsRegisteredCompany = false,
                    Email = email,
                    LastRefreshedUtc = DateTimeOffset.UtcNow,
                    Lookups = await BuildLookupsAsync(context, cancellationToken)
                };
            }

            var jobs = await context.CompanyJobs
                .AsNoTracking()
                .Where(j => j.EmailUsedToUploadJobs != null && j.EmailUsedToUploadJobs.ToLower() == normalizedEmail)
                .OrderByDescending(j => j.UploadDateTime)
                .ToListAsync(cancellationToken);

            var internships = await context.CompanyInternships
                .AsNoTracking()
                .Where(i => i.CompanyEmailUsedToUploadInternship != null && i.CompanyEmailUsedToUploadInternship.ToLower() == normalizedEmail)
                .OrderByDescending(i => i.CompanyInternshipUploadDate)
                .ToListAsync(cancellationToken);

            var theses = await context.CompanyTheses
                .AsNoTracking()
                .Where(t => t.CompanyEmailUsedToUploadThesis != null && t.CompanyEmailUsedToUploadThesis.ToLower() == normalizedEmail)
                .OrderByDescending(t => t.CompanyThesisUploadDateTime)
                .ToListAsync(cancellationToken);

            var announcements = await context.AnnouncementsAsCompany
                .AsNoTracking()
                .Where(a => a.CompanyAnnouncementCompanyEmail != null && a.CompanyAnnouncementCompanyEmail.ToLower() == normalizedEmail)
                .OrderByDescending(a => a.CompanyAnnouncementUploadDate)
                .ToListAsync(cancellationToken);

            var companyEvents = await context.CompanyEvents
                .AsNoTracking()
                .Where(e => e.CompanyEmailUsedToUploadEvent != null && e.CompanyEmailUsedToUploadEvent.ToLower() == normalizedEmail)
                .OrderByDescending(e => e.CompanyEventUploadedDate)
                .ToListAsync(cancellationToken);

            var professorEvents = await context.ProfessorEvents
                .AsNoTracking()
                .OrderByDescending(e => e.ProfessorEventUploadedDate)
                .Take(25)
                .ToListAsync(cancellationToken);

            List<AnnouncementAsResearchGroup> researchGroupAnnouncements = new();
            try
            {
                researchGroupAnnouncements = await context.AnnouncementAsResearchGroup
                    .Include(a => a.ResearchGroup)
                    .AsNoTracking()
                    .Where(a => a.ResearchGroupAnnouncementStatus == "Δημοσιευμένη")
                    .OrderByDescending(a => a.ResearchGroupAnnouncementUploadDate)
                    .ToListAsync(cancellationToken);
            }
            catch (SqlException ex) when (ex.Number == 208)
            {
                _logger.LogWarning(ex, "Research group announcements table missing; skipping load");
            }

            var jobApplications = await GetJobApplicationsForDashboardAsync(context, normalizedEmail, cancellationToken);
            var internshipApplications = await GetInternshipApplicationsForDashboardAsync(context, normalizedEmail, cancellationToken);
            var thesisApplications = await GetThesisApplicationsForDashboardAsync(context, normalizedEmail, cancellationToken);

            var interestInProfessorEvents = await context.InterestInProfessorEventsAsCompany
                .AsNoTracking()
                .Where(i => i.CompanyDetails != null && i.CompanyDetails.CompanyEmailShowInterestForProfessorEvent.ToLower() == normalizedEmail)
                .ToListAsync(cancellationToken);

            var interestInCompanyEvents = await context.InterestInCompanyEventAsProfessor
                .AsNoTracking()
                .Where(i => i.CompanyDetails != null && i.CompanyDetails.CompanyEmailWhereProfessorShowInterestForCompanyEvent.ToLower() == normalizedEmail)
                .ToListAsync(cancellationToken);

            return new CompanyDashboardData
            {
                IsAuthenticated = true,
                IsRegisteredCompany = true,
                Email = email,
                CompanyProfile = company,
                Jobs = jobs,
                Internships = internships,
                Theses = theses,
                Announcements = announcements,
                CompanyEvents = companyEvents,
                ProfessorEvents = professorEvents,
                ResearchGroupAnnouncements = researchGroupAnnouncements,
                JobApplications = jobApplications,
                InternshipApplications = internshipApplications,
                ThesisApplications = thesisApplications,
                InterestInProfessorEvents = interestInProfessorEvents,
                InterestInCompanyEvents = interestInCompanyEvents,
                LastRefreshedUtc = DateTimeOffset.UtcNow,
                Lookups = await BuildLookupsAsync(context, cancellationToken)
            };
        }

        private static async Task<IReadOnlyList<CompanyJobApplied>> GetJobApplicationsForDashboardAsync(
            AppDbContext context,
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            return await context.CompanyJobsApplied
                .AsNoTracking()
                .Where(a => a.CompanysEmailWhereStudentAppliedForCompanyJob != null && a.CompanysEmailWhereStudentAppliedForCompanyJob.ToLower() == normalizedEmail)
                .OrderByDescending(a => a.DateTimeStudentAppliedForCompanyJob)
                .Take(50)
                .ToListAsync(cancellationToken);
        }

        private static async Task<IReadOnlyList<InternshipApplied>> GetInternshipApplicationsForDashboardAsync(
            AppDbContext context,
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            return await context.InternshipsApplied
                .AsNoTracking()
                .Where(a => a.CompanyEmailWhereStudentAppliedForInternship != null && a.CompanyEmailWhereStudentAppliedForInternship.ToLower() == normalizedEmail)
                .OrderByDescending(a => a.DateTimeStudentAppliedForInternship)
                .Take(50)
                .ToListAsync(cancellationToken);
        }

        private static async Task<IReadOnlyList<CompanyThesisApplied>> GetThesisApplicationsForDashboardAsync(
            AppDbContext context,
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            return await context.CompanyThesesApplied
                .AsNoTracking()
                .Where(a => a.CompanyEmailWhereStudentAppliedForThesis != null && a.CompanyEmailWhereStudentAppliedForThesis.ToLower() == normalizedEmail)
                .OrderByDescending(a => a.DateTimeStudentAppliedForThesis)
                .Take(50)
                .ToListAsync(cancellationToken);
        }

        private static async Task<CompanyDashboardLookups> BuildLookupsAsync(AppDbContext context, CancellationToken cancellationToken)
        {
            var areas = await context.Areas.AsNoTracking().OrderBy(a => a.AreaName).ToListAsync(cancellationToken);
            var skills = await context.Skills.AsNoTracking().OrderBy(s => s.SkillName).ToListAsync(cancellationToken);
            var professors = await context.Professors.AsNoTracking().OrderBy(p => p.ProfSurname).Take(150).ToListAsync(cancellationToken);
            var researchGroups = await context.ResearchGroups.AsNoTracking().OrderBy(r => r.ResearchGroupName).Take(150).ToListAsync(cancellationToken);

            var regions = DefaultRegionToTownsMap.Keys.OrderBy(r => r).ToList();

            return new CompanyDashboardLookups
            {
                Areas = areas,
                Skills = skills,
                Professors = professors,
                ResearchGroups = researchGroups,
                Regions = regions,
                RegionToTownsMap = DefaultRegionToTownsMap
            };
        }

        private static void ApplyDecision(CompanyJobApplied application, ApplicationDecision decision)
        {
            switch (decision)
            {
                case ApplicationDecision.Accept:
                    application.CompanyPositionStatusAppliedAtTheCompanySide = "Επιτυχής";
                    application.CompanyPositionStatusAppliedAtTheStudentSide = "Επιτυχής";
                    break;
                case ApplicationDecision.Reject:
                    application.CompanyPositionStatusAppliedAtTheCompanySide = "Απορρίφθηκε";
                    application.CompanyPositionStatusAppliedAtTheStudentSide = "Απορρίφθηκε";
                    break;
                case ApplicationDecision.Cancel:
                    application.CompanyPositionStatusAppliedAtTheCompanySide = "Ακυρώθηκε";
                    application.CompanyPositionStatusAppliedAtTheStudentSide = "Ακυρώθηκε";
                    break;
            }
        }

        private static void ApplyDecision(InternshipApplied application, ApplicationDecision decision)
        {
            switch (decision)
            {
                case ApplicationDecision.Accept:
                    application.InternshipStatusAppliedAtTheCompanySide = "Επιτυχής";
                    application.InternshipStatusAppliedAtTheStudentSide = "Επιτυχής";
                    break;
                case ApplicationDecision.Reject:
                    application.InternshipStatusAppliedAtTheCompanySide = "Απορρίφθηκε";
                    application.InternshipStatusAppliedAtTheStudentSide = "Απορρίφθηκε";
                    break;
                case ApplicationDecision.Cancel:
                    application.InternshipStatusAppliedAtTheCompanySide = "Ακυρώθηκε";
                    application.InternshipStatusAppliedAtTheStudentSide = "Ακυρώθηκε";
                    break;
            }
        }

        private static void ApplyDecision(CompanyThesisApplied application, ApplicationDecision decision)
        {
            switch (decision)
            {
                case ApplicationDecision.Accept:
                    application.CompanyThesisStatusAppliedAtCompanySide = "Έχει γίνει Αποδοχή";
                    application.CompanyThesisStatusAppliedAtStudentSide = "Επιτυχής";
                    break;
                case ApplicationDecision.Reject:
                    application.CompanyThesisStatusAppliedAtCompanySide = "Έχει Απορριφθεί";
                    application.CompanyThesisStatusAppliedAtStudentSide = "Απορρίφθηκε";
                    break;
                case ApplicationDecision.Cancel:
                    application.CompanyThesisStatusAppliedAtCompanySide = "Ακυρώθηκε";
                    application.CompanyThesisStatusAppliedAtStudentSide = "Ακυρώθηκε";
                    break;
            }
        }

        private static async Task<IReadOnlyList<Student>> LoadStudentsByEmailsAsync(
            AppDbContext context,
            IEnumerable<string> studentEmails,
            CancellationToken cancellationToken)
        {
            var emailList = studentEmails
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.ToLowerInvariant())
                .ToList();

            if (emailList.Count == 0)
            {
                return Array.Empty<Student>();
            }

            return await context.Students
                .AsNoTracking()
                .Where(s => s.Email != null && emailList.Contains(s.Email.ToLower()))
                .OrderBy(s => s.Surname)
                .ThenBy(s => s.Name)
                .ToListAsync(cancellationToken);
        }

        private async Task<string?> ResolveCompanyEmailAsync(CancellationToken cancellationToken)
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
                _logger.LogError(ex, "Unable to resolve the current company email.");
                return null;
            }
        }
    }
}

