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

namespace QuizManager.Services.StudentDashboard
{
    public class StudentDashboardService : IStudentDashboardService
    {
        private const string DashboardCachePrefix = "student-dashboard:";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<StudentDashboardService> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly SemaphoreSlim _dashboardLock = new(1, 1);

        public StudentDashboardService(
            AuthenticationStateProvider authenticationStateProvider,
            IDbContextFactory<AppDbContext> dbContextFactory,
            ILogger<StudentDashboardService> logger,
            IMemoryCache memoryCache)
        {
            _authenticationStateProvider = authenticationStateProvider;
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public async Task<StudentDashboardData> LoadDashboardDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (user?.Identity?.IsAuthenticated != true)
                {
                    return StudentDashboardData.Empty;
                }

                var email = ResolveUserEmail(user);
                if (string.IsNullOrWhiteSpace(email))
                {
                    return StudentDashboardData.Empty;
                }

                // Check cache first
                if (_memoryCache.TryGetValue(DashboardCachePrefix + email, out StudentDashboardData cached))
                {
                    return cached;
                }

                // Acquire lock for thread-safe cache population
                await _dashboardLock.WaitAsync(cancellationToken);
                try
                {
                    // Double-check after acquiring lock
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load student dashboard data");
                return StudentDashboardData.Empty;
            }
        }

        public async Task RefreshDashboardCacheAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (user?.Identity?.IsAuthenticated != true)
                {
                    return;
                }

                var email = ResolveUserEmail(user);
                if (string.IsNullOrWhiteSpace(email))
                {
                    return;
                }

                _memoryCache.Remove(DashboardCachePrefix + email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh student dashboard cache");
            }
        }

        private async Task<StudentDashboardData> BuildDashboardDataAsync(string email, CancellationToken cancellationToken)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var student = await context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Email == email, cancellationToken);

        if (student == null)
        {
            return new StudentDashboardData
            {
                IsAuthenticated = true,
                Email = email,
                IsRegisteredStudent = false
            };
        }

                // Load Company Thesis Applications
                var companyThesisApplications = await context.CompanyThesesApplied
                    .Include(a => a.StudentDetails)
                    .Include(a => a.CompanyDetails)
                    .Where(app => app.StudentEmailAppliedForThesis == email &&
                                  app.StudentUniqueIDAppliedForThesis == student.Student_UniqueID)
                    .OrderByDescending(app => app.DateTimeStudentAppliedForThesis)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var companyThesisIds = companyThesisApplications
                    .Select(app => app.RNGForCompanyThesisApplied)
                    .Distinct()
                    .ToList();

                var companyThesisCache = await LoadCompanyThesisCacheAsync(context, companyThesisIds, cancellationToken);

                // Load Professor Thesis Applications
                var professorThesisApplications = await context.ProfessorThesesApplied
                    .Include(a => a.StudentDetails)
                    .Include(a => a.ProfessorDetails)
                    .Where(app => app.StudentEmailAppliedForProfessorThesis == email &&
                                  app.StudentUniqueIDAppliedForProfessorThesis == student.Student_UniqueID)
                    .OrderByDescending(app => app.DateTimeStudentAppliedForProfessorThesis)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var professorThesisIds = professorThesisApplications
                    .Select(app => app.RNGForProfessorThesisApplied)
                    .Distinct()
                    .ToList();

                var professorThesisCache = await LoadProfessorThesisCacheAsync(context, professorThesisIds, cancellationToken);

                // Load Job Applications
                var jobApplications = await context.CompanyJobsApplied
                    .Include(a => a.StudentDetails)
                    .Include(a => a.CompanyDetails)
                    .Where(app => app.StudentEmailAppliedForCompanyJob == email &&
                                  app.StudentUniqueIDAppliedForCompanyJob == student.Student_UniqueID)
                    .OrderByDescending(app => app.DateTimeStudentAppliedForCompanyJob)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var jobIds = jobApplications
                    .Select(app => app.RNGForCompanyJobApplied)
                    .Distinct()
                    .ToList();

                var jobCache = await LoadJobCacheAsync(context, jobIds, cancellationToken);

                // Load Company Internship Applications
                var companyInternshipApplications = await context.InternshipsApplied
                    .Include(a => a.StudentDetails)
                    .Include(a => a.CompanyDetails)
                    .Where(app => app.StudentEmailAppliedForInternship == email &&
                                  app.StudentUniqueIDAppliedForInternship == student.Student_UniqueID)
                    .OrderByDescending(app => app.DateTimeStudentAppliedForInternship)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var companyInternshipIds = companyInternshipApplications
                    .Select(app => app.RNGForInternshipApplied)
                    .Distinct()
                    .ToList();

                var companyInternshipCache = await LoadCompanyInternshipCacheAsync(context, companyInternshipIds, cancellationToken);

                // Load Professor Internship Applications
                var professorInternshipApplications = await context.ProfessorInternshipsApplied
                    .Include(a => a.StudentDetails)
                    .Include(a => a.ProfessorDetails)
                    .Where(app => app.StudentEmailAppliedForProfessorInternship == email &&
                                  app.StudentUniqueIDAppliedForProfessorInternship == student.Student_UniqueID)
                    .OrderByDescending(app => app.DateTimeStudentAppliedForProfessorInternship)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var professorInternshipIds = professorInternshipApplications
                    .Select(app => app.RNGForProfessorInternshipApplied)
                    .Distinct()
                    .ToList();

                var professorInternshipCache = await LoadProfessorInternshipCacheAsync(context, professorInternshipIds, cancellationToken);

                // Load Event Interest IDs
                var companyEventInterestIds = await context.InterestInCompanyEvents
                    .AsNoTracking()
                    .Where(e => e.StudentUniqueIDShowInterestForEvent == student.Student_UniqueID &&
                                e.StudentEmailShowInterestForEvent == email)
                    .Select(e => e.RNGForCompanyEventInterest)
                    .ToListAsync(cancellationToken);

                var professorEventInterestIds = await context.InterestInProfessorEvents
                    .AsNoTracking()
                    .Where(e => e.StudentUniqueIDShowInterestForEvent == student.Student_UniqueID &&
                                e.StudentEmailShowInterestForEvent == email)
                    .Select(e => e.RNGForProfessorEventInterest)
                    .ToListAsync(cancellationToken);

                // Load Event Interests (full records)
                var companyEventInterests = await context.InterestInCompanyEvents
                    .AsNoTracking()
                    .Where(e => e.StudentUniqueIDShowInterestForEvent == student.Student_UniqueID &&
                                e.StudentEmailShowInterestForEvent == email)
                    .OrderByDescending(e => e.DateTimeStudentShowInterest)
                    .ToListAsync(cancellationToken);

                var professorEventInterests = await context.InterestInProfessorEvents
                    .AsNoTracking()
                    .Where(e => e.StudentUniqueIDShowInterestForEvent == student.Student_UniqueID &&
                                e.StudentEmailShowInterestForEvent == email)
                    .OrderByDescending(e => e.DateTimeStudentShowInterest)
                    .ToListAsync(cancellationToken);

                return new StudentDashboardData
                {
                    IsAuthenticated = true,
                    IsRegisteredStudent = true,
                    Email = email,
                    Student = student,
                    CompanyThesisApplications = companyThesisApplications,
                    CompanyThesisCache = companyThesisCache,
                    ProfessorThesisApplications = professorThesisApplications,
                    ProfessorThesisCache = professorThesisCache,
                    JobApplications = jobApplications,
                    JobCache = jobCache,
                    CompanyInternshipApplications = companyInternshipApplications,
                    CompanyInternshipCache = companyInternshipCache,
                    ProfessorInternshipApplications = professorInternshipApplications,
                    ProfessorInternshipCache = professorInternshipCache,
                    CompanyThesisIdsApplied = companyThesisIds.ToHashSet(),
                    ProfessorThesisIdsApplied = professorThesisIds.ToHashSet(),
                    JobIdsApplied = jobIds.ToHashSet(),
                    CompanyInternshipIdsApplied = companyInternshipIds.ToHashSet(),
                    ProfessorInternshipIdsApplied = professorInternshipIds.ToHashSet(),
                    CompanyEventInterestIds = companyEventInterestIds.ToHashSet(),
                    ProfessorEventInterestIds = professorEventInterestIds.ToHashSet(),
                    CompanyEventInterests = companyEventInterests,
                    ProfessorEventInterests = professorEventInterests
                };
        }

        // TODO: Implement remaining methods by extracting from MainLayout.razor.cs
        // Following the pattern but using actual MainLayout code, not reference implementation

        public async Task<IReadOnlyList<string>> GetJobTitleSuggestionsAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            {
                return Array.Empty<string>();
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                return await context.CompanyJobs
                    .AsNoTracking()
                    .Where(job => job.PositionTitle != null &&
                                   Microsoft.EntityFrameworkCore.EF.Functions.Like(job.PositionTitle, $"%{searchTerm}%"))
                    .Select(job => job.PositionTitle!)
                    .Distinct()
                    .OrderBy(title => title)
                    .Take(10)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch job title suggestions");
                return Array.Empty<string>();
            }
        }

        public async Task<IReadOnlyList<string>> GetCompanyNameSuggestionsAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            {
                return Array.Empty<string>();
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                return await context.CompanyJobs
                    .Include(job => job.Company)
                    .Where(job => job.Company != null &&
                                   Microsoft.EntityFrameworkCore.EF.Functions.Like(job.Company.CompanyName, $"%{searchTerm}%"))
                    .Select(job => job.Company!.CompanyName)
                    .Distinct()
                    .OrderBy(name => name)
                    .Take(10)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch company name suggestions");
                return Array.Empty<string>();
            }
        }

        public async Task<bool> WithdrawProfessorThesisApplicationAsync(long rngForThesisApplied, CancellationToken cancellationToken = default)
        {
            try
            {
                var email = await ResolveCurrentUserEmailAsync(cancellationToken);
                if (string.IsNullOrEmpty(email))
                {
                    return false;
                }

                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var application = await context.ProfessorThesesApplied
                    .FirstOrDefaultAsync(app => app.RNGForProfessorThesisApplied == rngForThesisApplied &&
                                                app.StudentEmailAppliedForProfessorThesis == email, cancellationToken);

                if (application == null)
                {
                    return false;
                }

                // Update status
                application.ProfessorThesisStatusAppliedAtStudentSide = "Αποσύρθηκε από τον φοιτητή";
                application.ProfessorThesisStatusAppliedAtProfessorSide = "Αποσύρθηκε από τον φοιτητή";

                await context.SaveChangesAsync(cancellationToken);
                await RefreshDashboardCacheAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to withdraw professor thesis application");
                return false;
            }
        }

        public async Task<bool> WithdrawCompanyThesisApplicationAsync(long rngForThesisApplied, CancellationToken cancellationToken = default)
        {
            try
            {
                var email = await ResolveCurrentUserEmailAsync(cancellationToken);
                if (string.IsNullOrEmpty(email))
                {
                    return false;
                }

                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var application = await context.CompanyThesesApplied
                    .FirstOrDefaultAsync(app => app.RNGForCompanyThesisApplied == rngForThesisApplied &&
                                                app.StudentEmailAppliedForThesis == email, cancellationToken);

                if (application == null)
                {
                    return false;
                }

                // Update status
                application.CompanyThesisStatusAppliedAtStudentSide = "Αποσύρθηκε από τον φοιτητή";
                application.CompanyThesisStatusAppliedAtCompanySide = "Αποσύρθηκε από τον φοιτητή";

                await context.SaveChangesAsync(cancellationToken);
                _memoryCache.Remove(DashboardCachePrefix + email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to withdraw company thesis application");
                return false;
            }
        }

        public async Task<bool> WithdrawCompanyInternshipApplicationAsync(long rngForInternshipApplied, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var application = await context.InternshipsApplied
                    .Include(app => app.StudentDetails)
                    .FirstOrDefaultAsync(app => app.RNGForInternshipApplied == rngForInternshipApplied, cancellationToken);

                if (application == null)
                {
                    return false;
                }

                application.InternshipStatusAppliedAtTheCompanySide = "Αποσύρθηκε από τον φοιτητή";
                application.InternshipStatusAppliedAtTheStudentSide = "Αποσύρθηκε από τον φοιτητή";

                context.PlatformActions.Add(new PlatformActions
                {
                    UserRole_PerformedAction = "STUDENT",
                    ForWhat_PerformedAction = "COMPANY_INTERNSHIP",
                    HashedPositionRNG_PerformedAction = HashingHelper.HashLong(rngForInternshipApplied),
                    TypeOfAction_PerformedAction = "SELFWITHDRAW",
                    DateTime_PerformedAction = DateTime.Now
                });

                await context.SaveChangesAsync(cancellationToken);
                await RefreshDashboardCacheAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to withdraw company internship application");
                return false;
            }
        }

        public async Task<bool> WithdrawProfessorInternshipApplicationAsync(long rngForInternshipApplied, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var application = await context.ProfessorInternshipsApplied
                    .Include(app => app.StudentDetails)
                    .FirstOrDefaultAsync(app => app.RNGForProfessorInternshipApplied == rngForInternshipApplied, cancellationToken);

                if (application == null)
                {
                    return false;
                }

                application.InternshipStatusAppliedAtTheProfessorSide = "Αποσύρθηκε από τον φοιτητή";
                application.InternshipStatusAppliedAtTheStudentSide = "Αποσύρθηκε από τον φοιτητή";

                context.PlatformActions.Add(new PlatformActions
                {
                    UserRole_PerformedAction = "STUDENT",
                    ForWhat_PerformedAction = "PROFESSOR_INTERNSHIP",
                    HashedPositionRNG_PerformedAction = HashingHelper.HashLong(rngForInternshipApplied),
                    TypeOfAction_PerformedAction = "SELFWITHDRAW",
                    DateTime_PerformedAction = DateTime.Now
                });

                await context.SaveChangesAsync(cancellationToken);
                await RefreshDashboardCacheAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to withdraw professor internship application");
                return false;
            }
        }

        public async Task<bool> WithdrawJobApplicationAsync(long rngForJobApplied, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var application = await context.CompanyJobsApplied
                    .Include(app => app.StudentDetails)
                    .Include(app => app.CompanyDetails)
                    .FirstOrDefaultAsync(app => app.RNGForCompanyJobApplied == rngForJobApplied, cancellationToken);

                if (application == null)
                {
                    return false;
                }

                const string withdrawnStatus = "Αποσύρθηκε από τον φοιτητή";
                application.CompanyPositionStatusAppliedAtTheCompanySide = withdrawnStatus;
                application.CompanyPositionStatusAppliedAtTheStudentSide = withdrawnStatus;

                context.PlatformActions.Add(new PlatformActions
                {
                    UserRole_PerformedAction = "STUDENT",
                    ForWhat_PerformedAction = "COMPANY_JOB",
                    HashedPositionRNG_PerformedAction = HashingHelper.HashLong(rngForJobApplied),
                    TypeOfAction_PerformedAction = "SELFWITHDRAW",
                    DateTime_PerformedAction = DateTime.Now
                });

                await context.SaveChangesAsync(cancellationToken);
                await RefreshDashboardCacheAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to withdraw job application");
                return false;
            }
        }

        public async Task<(byte[] Data, string FileName)?> GetCompanyInternshipAttachmentAsync(long internshipId, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var internship = await context.CompanyInternships
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.RNGForInternshipUploadedAsCompany == internshipId, cancellationToken);

                if (internship?.CompanyInternshipAttachment == null || internship.CompanyInternshipAttachment.Length == 0)
                {
                    return null;
                }

                var fileName = $"Internship_Attachment_{internshipId}.pdf";
                return (internship.CompanyInternshipAttachment, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch internship attachment");
                return null;
            }
        }

        public async Task<Company?> GetCompanyByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                return await context.Companies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(company => company.CompanyEmail == email, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load company details for {Email}", email);
                return null;
            }
        }

        public async Task<Professor?> GetProfessorByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                return await context.Professors
                    .AsNoTracking()
                    .FirstOrDefaultAsync(professor => professor.ProfEmail == email, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load professor details for {Email}", email);
                return null;
            }
        }

        public async Task<Dictionary<long, CompanyInternship>> GetCompanyInternshipsByIdsAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default)
        {
            var idList = ids?.ToList() ?? new List<long>();
            if (idList.Count == 0)
            {
                return new Dictionary<long, CompanyInternship>();
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var internships = await context.CompanyInternships
                .Include(i => i.Company)
                .AsNoTracking()
                .Where(i => idList.Contains(i.RNGForInternshipUploadedAsCompany))
                .ToListAsync(cancellationToken);

            return internships.ToDictionary(i => i.RNGForInternshipUploadedAsCompany, i => i);
        }

        public async Task<Dictionary<long, ProfessorInternship>> GetProfessorInternshipsByIdsAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default)
        {
            var idList = ids?.ToList() ?? new List<long>();
            if (idList.Count == 0)
            {
                return new Dictionary<long, ProfessorInternship>();
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var internships = await context.ProfessorInternships
                .Include(i => i.Professor)
                .AsNoTracking()
                .Where(i => idList.Contains(i.RNGForInternshipUploadedAsProfessor))
                .ToListAsync(cancellationToken);

            return internships.ToDictionary(i => i.RNGForInternshipUploadedAsProfessor, i => i);
        }

        public async Task<CompanyInternship?> GetCompanyInternshipByRngAsync(long rng, CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.CompanyInternships
                .Include(i => i.Company)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.RNGForInternshipUploadedAsCompany == rng, cancellationToken);
        }

        public async Task<ProfessorInternship?> GetProfessorInternshipByRngAsync(long rng, CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.ProfessorInternships
                .Include(i => i.Professor)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.RNGForInternshipUploadedAsProfessor == rng, cancellationToken);
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
                .FirstOrDefaultAsync(s => s.Email == email, cancellationToken);
        }

        public async Task<Student?> GetStudentByUniqueIdAsync(string uniqueId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(uniqueId))
            {
                return null;
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Student_UniqueID == uniqueId, cancellationToken);
        }

        public async Task<CompanyEventInterestResult?> ShowInterestInCompanyEventAsync(
            long eventRng,
            bool needsTransport,
            string? chosenLocation,
            CancellationToken cancellationToken = default)
        {
            if (eventRng <= 0)
            {
                return null;
            }

            try
            {
                var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;
                var email = ResolveUserEmail(user);

                if (string.IsNullOrWhiteSpace(email))
                {
                    return null;
                }

                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var student = await context.Students
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Email == email, cancellationToken);

                if (student == null)
                {
                    return null;
                }

                var companyEvent = await context.CompanyEvents
                    .Include(e => e.Company)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.RNGForEventUploadedAsCompany == eventRng, cancellationToken);

                if (companyEvent == null || companyEvent.CompanyEventStatus != "Δημοσιευμένη")
                {
                    return null;
                }

                var existingInterest = await context.InterestInCompanyEvents
                    .AnyAsync(i =>
                        i.StudentUniqueIDShowInterestForEvent == student.Student_UniqueID &&
                        i.StudentEmailShowInterestForEvent == student.Email &&
                        i.RNGForCompanyEventInterest == eventRng,
                        cancellationToken);

                if (existingInterest)
                {
                    return null;
                }

                var company = companyEvent.Company
                    ?? await context.Companies
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.CompanyEmail == companyEvent.CompanyEmailUsedToUploadEvent, cancellationToken);

                var normalizedLocation = chosenLocation ?? string.Empty;
                await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var interest = new InterestInCompanyEvent
                    {
                        RNGForCompanyEventInterest = eventRng,
                        RNGForCompanyEventInterest_HashedAsUniqueID = companyEvent.RNGForEventUploadedAsCompany_HashedAsUniqueID ?? string.Empty,
                        DateTimeStudentShowInterest = DateTime.Now,
                        CompanyEventStatusAtStudentSide = "Έχετε Δείξει Ενδιαφέρον",
                        CompanyEventStatusAtCompanySide = "Προς Επεξεργασία",
                        CompanyEmailWhereStudentShowedInterest = companyEvent.CompanyEmailUsedToUploadEvent ?? string.Empty,
                        CompanyUniqueIDWhereStudentShowedInterest = company?.Company_UniqueID ?? string.Empty,
                        StudentEmailShowInterestForEvent = student.Email ?? string.Empty,
                        StudentUniqueIDShowInterestForEvent = student.Student_UniqueID,
                        StudentTransportNeedWhenShowInterestForCompanyEvent = needsTransport ? "Ναι" : "Όχι",
                        StudentTransportChosenLocationWhenShowInterestForCompanyEvent = normalizedLocation,
                        StudentDetails = new InterestInCompanyEvent_StudentDetails
                        {
                            StudentUniqueIDShowInterestForCompanyEvent = student.Student_UniqueID,
                            StudentEmailShowInterestForCompanyEvent = student.Email ?? string.Empty,
                            DateTimeStudentShowInterestForCompanyEvent = DateTime.Now,
                            RNGForCompanyEventShowInterestAsStudent_HashedAsUniqueID = companyEvent.RNGForEventUploadedAsCompany_HashedAsUniqueID ?? string.Empty
                        },
                        CompanyDetails = new InterestInCompanyEvent_CompanyDetails
                        {
                            CompanyUniqueIDWhereStudentShowInterestForCompanyEvent = company?.Company_UniqueID ?? string.Empty,
                            CompanyEmailWhereStudentShowInterestForCompanyEvent = companyEvent.CompanyEmailUsedToUploadEvent ?? string.Empty
                        }
                    };

                    context.InterestInCompanyEvents.Add(interest);
                    context.PlatformActions.Add(new PlatformActions
                    {
                        UserRole_PerformedAction = "STUDENT",
                        ForWhat_PerformedAction = "COMPANY_EVENT",
                        HashedPositionRNG_PerformedAction = HashingHelper.HashLong(eventRng),
                        TypeOfAction_PerformedAction = "SHOW_INTEREST",
                        DateTime_PerformedAction = DateTime.Now
                    });

                    await context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return new CompanyEventInterestResult(companyEvent, company, student, needsTransport, normalizedLocation);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show interest for company event {EventRng}", eventRng);
                return null;
            }
        }

        public async Task<bool> WithdrawCompanyEventInterestAsync(long eventRng, CancellationToken cancellationToken = default)
        {
            if (eventRng <= 0)
            {
                return false;
            }

            try
            {
                var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;
                var email = ResolveUserEmail(user);

                if (string.IsNullOrWhiteSpace(email))
                {
                    return false;
                }

                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var student = await context.Students
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Email == email, cancellationToken);

                if (student == null)
                {
                    return false;
                }

                var interest = await context.InterestInCompanyEvents
                    .Include(i => i.StudentDetails)
                    .Include(i => i.CompanyDetails)
                    .FirstOrDefaultAsync(i =>
                        i.RNGForCompanyEventInterest == eventRng &&
                        i.StudentUniqueIDShowInterestForEvent == student.Student_UniqueID &&
                        i.StudentEmailShowInterestForEvent == student.Email,
                        cancellationToken);

                if (interest == null)
                {
                    return false;
                }

                context.InterestInCompanyEvents.Remove(interest);
                context.PlatformActions.Add(new PlatformActions
                {
                    UserRole_PerformedAction = "STUDENT",
                    ForWhat_PerformedAction = "COMPANY_EVENT",
                    HashedPositionRNG_PerformedAction = HashingHelper.HashLong(eventRng),
                    TypeOfAction_PerformedAction = "WITHDRAW_INTEREST",
                    DateTime_PerformedAction = DateTime.Now
                });

                await context.SaveChangesAsync(cancellationToken);
                await RefreshDashboardCacheAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to withdraw company event interest for {EventRng}", eventRng);
                return false;
            }
        }

        public async Task<ProfessorEventInterestResult?> ShowInterestInProfessorEventAsync(
            long eventRng,
            bool needsTransport,
            string? chosenLocation,
            CancellationToken cancellationToken = default)
        {
            if (eventRng <= 0)
            {
                return null;
            }

            try
            {
                var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;
                var email = ResolveUserEmail(user);

                if (string.IsNullOrWhiteSpace(email))
                {
                    return null;
                }

                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var student = await context.Students
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Email == email, cancellationToken);

                if (student == null)
                {
                    return null;
                }

                var professorEvent = await context.ProfessorEvents
                    .Include(e => e.Professor)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.RNGForEventUploadedAsProfessor == eventRng, cancellationToken);

                if (professorEvent == null || professorEvent.ProfessorEventStatus != "Δημοσιευμένη")
                {
                    return null;
                }

                var existingInterest = await context.InterestInProfessorEvents
                    .AnyAsync(i =>
                        i.StudentUniqueIDShowInterestForEvent == student.Student_UniqueID &&
                        i.StudentEmailShowInterestForEvent == student.Email &&
                        i.RNGForProfessorEventInterest == eventRng,
                        cancellationToken);

                if (existingInterest)
                {
                    return null;
                }

                var professor = professorEvent.Professor
                    ?? await context.Professors
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.ProfEmail == professorEvent.ProfessorEventResponsiblePersonEmail, cancellationToken);

                var normalizedLocation = chosenLocation ?? string.Empty;
                await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var interest = new InterestInProfessorEvent
                    {
                        RNGForProfessorEventInterest = eventRng,
                        RNGForProfessorEventInterest_HashedAsUniqueID = professorEvent.RNGForEventUploadedAsProfessor_HashedAsUniqueID ?? string.Empty,
                        DateTimeStudentShowInterest = DateTime.Now,
                        ProfessorEventStatusAtStudentSide = "Έχετε Δείξει Ενδιαφέρον",
                        ProfessorEventStatusAtProfessorSide = "Προς Επεξεργασία",
                        ProfessorEmailWhereStudentShowedInterest = professorEvent.ProfessorEventResponsiblePersonEmail ?? string.Empty,
                        ProfessorUniqueIDWhereStudentShowedInterest = professor?.Professor_UniqueID ?? string.Empty,
                        StudentEmailShowInterestForEvent = student.Email ?? string.Empty,
                        StudentUniqueIDShowInterestForEvent = student.Student_UniqueID,
                        StudentTransportNeedWhenShowInterestForProfessorEvent = needsTransport ? "Ναι" : "Όχι",
                        StudentTransportChosenLocationWhenShowInterestForProfessorEvent = normalizedLocation,
                        StudentDetails = new InterestInProfessorEvent_StudentDetails
                        {
                            StudentUniqueIDShowInterestForProfessorEvent = student.Student_UniqueID,
                            StudentEmailShowInterestForProfessorEvent = student.Email ?? string.Empty,
                            DateTimeStudentShowInterestForProfessorEvent = DateTime.Now,
                            RNGForProfessorEventShowInterestAsStudent_HashedAsUniqueID = professorEvent.RNGForEventUploadedAsProfessor_HashedAsUniqueID ?? string.Empty
                        },
                        ProfessorDetails = new InterestInProfessorEvent_ProfessorDetails
                        {
                            ProfessorUniqueIDWhereStudentShowInterestForProfessorEvent = professor?.Professor_UniqueID ?? string.Empty,
                            ProfessorEmailWhereStudentShowInterestForProfessorEvent = professorEvent.ProfessorEventResponsiblePersonEmail ?? string.Empty
                        }
                    };

                    context.InterestInProfessorEvents.Add(interest);
                    context.PlatformActions.Add(new PlatformActions
                    {
                        UserRole_PerformedAction = "STUDENT",
                        ForWhat_PerformedAction = "PROFESSOR_EVENT",
                        HashedPositionRNG_PerformedAction = HashingHelper.HashLong(eventRng),
                        TypeOfAction_PerformedAction = "SHOW_INTEREST",
                        DateTime_PerformedAction = DateTime.Now
                    });

                    await context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return new ProfessorEventInterestResult(professorEvent, professor, student, needsTransport, normalizedLocation);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show interest for professor event {EventRng}", eventRng);
                return null;
            }
        }

        public async Task<bool> WithdrawProfessorEventInterestAsync(long eventRng, CancellationToken cancellationToken = default)
        {
            if (eventRng <= 0)
            {
                return false;
            }

            try
            {
                var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;
                var email = ResolveUserEmail(user);

                if (string.IsNullOrWhiteSpace(email))
                {
                    return false;
                }

                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var student = await context.Students
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Email == email, cancellationToken);

                if (student == null)
                {
                    return false;
                }

                var interest = await context.InterestInProfessorEvents
                    .Include(i => i.StudentDetails)
                    .Include(i => i.ProfessorDetails)
                    .FirstOrDefaultAsync(i =>
                        i.RNGForProfessorEventInterest == eventRng &&
                        i.StudentUniqueIDShowInterestForEvent == student.Student_UniqueID &&
                        i.StudentEmailShowInterestForEvent == student.Email,
                        cancellationToken);

                if (interest == null)
                {
                    return false;
                }

                context.InterestInProfessorEvents.Remove(interest);
                context.PlatformActions.Add(new PlatformActions
                {
                    UserRole_PerformedAction = "STUDENT",
                    ForWhat_PerformedAction = "PROFESSOR_EVENT",
                    HashedPositionRNG_PerformedAction = HashingHelper.HashLong(eventRng),
                    TypeOfAction_PerformedAction = "WITHDRAW_INTEREST",
                    DateTime_PerformedAction = DateTime.Now
                });

                await context.SaveChangesAsync(cancellationToken);
                await RefreshDashboardCacheAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to withdraw professor event interest for {EventRng}", eventRng);
                return false;
            }
        }

        private static string? ResolveUserEmail(ClaimsPrincipal user)
        {
            // Match MainLayout.razor.cs pattern - uses "name" claim for email (Auth0)
            return user.FindFirst("name")?.Value
                ?? user.FindFirst(ClaimTypes.Email)?.Value
                ?? user.Claims.FirstOrDefault(c => string.Equals(c.Type, "email", StringComparison.OrdinalIgnoreCase))?.Value
                ?? user.Identity?.Name;
        }

        private async Task<string?> ResolveCurrentUserEmailAsync(CancellationToken cancellationToken)
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var email = ResolveUserEmail(user);
            return string.IsNullOrWhiteSpace(email) ? null : email;
        }

        // Helper methods for loading caches
        private static async Task<Dictionary<long, CompanyThesis>> LoadCompanyThesisCacheAsync(
            AppDbContext context,
            IReadOnlyCollection<long> thesisIds,
            CancellationToken cancellationToken)
        {
            if (thesisIds.Count == 0)
            {
                return new Dictionary<long, CompanyThesis>();
            }

            var theses = await context.CompanyTheses
                .Include(thesis => thesis.Company)
                .Where(thesis => thesisIds.Contains(thesis.RNGForThesisUploadedAsCompany))
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return theses.ToDictionary(thesis => thesis.RNGForThesisUploadedAsCompany, thesis => thesis);
        }

        private static async Task<Dictionary<long, ProfessorThesis>> LoadProfessorThesisCacheAsync(
            AppDbContext context,
            IReadOnlyCollection<long> thesisIds,
            CancellationToken cancellationToken)
        {
            if (thesisIds.Count == 0)
            {
                return new Dictionary<long, ProfessorThesis>();
            }

            var theses = await context.ProfessorTheses
                .Include(thesis => thesis.Professor)
                .Where(thesis => thesisIds.Contains(thesis.RNGForThesisUploaded))
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return theses.ToDictionary(thesis => thesis.RNGForThesisUploaded, thesis => thesis);
        }

        private static async Task<Dictionary<long, CompanyJob>> LoadJobCacheAsync(
            AppDbContext context,
            IReadOnlyCollection<long> jobIds,
            CancellationToken cancellationToken)
        {
            if (jobIds.Count == 0)
            {
                return new Dictionary<long, CompanyJob>();
            }

            var jobs = await context.CompanyJobs
                .Include(job => job.Company)
                .Where(job => jobIds.Contains(job.RNGForPositionUploaded))
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return jobs.ToDictionary(job => job.RNGForPositionUploaded, job => job);
        }

        private static async Task<Dictionary<string, AllInternships>> LoadCompanyInternshipCacheAsync(
            AppDbContext context,
            IReadOnlyCollection<long> internshipIds,
            CancellationToken cancellationToken)
        {
            if (internshipIds.Count == 0)
            {
                return new Dictionary<string, AllInternships>();
            }

            var internships = await context.CompanyInternships
                .Include(internship => internship.Company)
                .Where(internship => internshipIds.Contains(internship.RNGForInternshipUploadedAsCompany))
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return internships.ToDictionary(
                internship => internship.RNGForInternshipUploadedAsCompany.ToString(),
                internship => new AllInternships
                {
                    InternshipType = InternshipType.Company.ToString(),
                    CompanyName = internship.Company?.CompanyName,
                    CompanyEmail = internship.CompanyEmailUsedToUploadInternship,
                    InternshipTitle = internship.CompanyInternshipTitle,
                    InternshipAreas = internship.CompanyInternshipAreas,
                    InternshipStatus = internship.CompanyUploadedInternshipStatus,
                    InternshipActivePeriod = internship.CompanyInternshipActivePeriod,
                    InternshipFinishEstimation = internship.CompanyInternshipFinishEstimation,
                    RNGForCompanyInternship = internship.RNGForInternshipUploadedAsCompany,
                    CompanyInternshipUploadDate = internship.CompanyInternshipUploadDate,
                    InternshipFundingType = internship.CompanyInternshipESPA,
                    RNGForCompanyInternship_HashedAsUniqueID = internship.RNGForInternshipUploadedAsCompany_HashedAsUniqueID,
                    InternshipTypeName = internship.CompanyInternshipType,
                    InternshipTransportOffer = internship.CompanyInternshipTransportOffer,
                    InternshipPerifereiaLocation = internship.CompanyInternshipPerifereiaLocation,
                    InternshipDimosLocation = internship.CompanyInternshipDimosLocation,
                    InternshipDescription = internship.CompanyInternshipDescription,
                    ThesisType = InternshipType.Company
                });
        }

        private static async Task<Dictionary<string, AllInternships>> LoadProfessorInternshipCacheAsync(
            AppDbContext context,
            IReadOnlyCollection<long> internshipIds,
            CancellationToken cancellationToken)
        {
            if (internshipIds.Count == 0)
            {
                return new Dictionary<string, AllInternships>();
            }

            var internships = await context.ProfessorInternships
                .Include(internship => internship.Professor)
                .Where(internship => internshipIds.Contains(internship.RNGForInternshipUploadedAsProfessor))
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return internships.ToDictionary(
                internship => internship.RNGForInternshipUploadedAsProfessor.ToString(),
                internship => new AllInternships
                {
                    InternshipType = InternshipType.Professor.ToString(),
                    ProfessorName = internship.Professor?.ProfName,
                    ProfessorSurname = internship.Professor?.ProfSurname,
                    ProfessorDepartment = internship.Professor?.ProfDepartment,
                    ProfessorEmail = internship.Professor?.ProfEmail,
                    InternshipTitle = internship.ProfessorInternshipTitle,
                    ProfessorInternshipAreas = internship.ProfessorInternshipAreas,
                    InternshipAreas = internship.ProfessorInternshipAreas,
                    ProfessorInternshipStatus = internship.ProfessorUploadedInternshipStatus,
                    InternshipActivePeriod = internship.ProfessorInternshipActivePeriod,
                    InternshipFinishEstimation = internship.ProfessorInternshipFinishEstimation,
                    RNGForProfessorInternship = internship.RNGForInternshipUploadedAsProfessor,
                    ProfessorInternshipUploadDate = internship.ProfessorInternshipUploadDate,
                    InternshipFundingType = internship.ProfessorInternshipESPA,
                    RNGForProfessorInternship_HashedAsUniqueID = internship.RNGForInternshipUploadedAsProfessor_HashedAsUniqueID,
                    InternshipTypeName = internship.ProfessorInternshipType,
                    InternshipPerifereiaLocation = internship.ProfessorInternshipPerifereiaLocation,
                    InternshipDimosLocation = internship.ProfessorInternshipDimosLocation,
                    InternshipDescription = internship.ProfessorInternshipDescription,
                    ThesisType = InternshipType.Professor
                });
        }
    }
}
