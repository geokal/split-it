using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuizManager.Data;
using QuizManager.Models;
using System.Threading;
using System.Threading.Tasks;

namespace QuizManager.Services.Authentication
{
    /// <summary>
    /// Service for retrieving and caching user profile data
    /// </summary>
    public class UserProfileService : IUserProfileService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ICacheService _cacheService;
        private readonly ILogger<UserProfileService> _logger;

        public UserProfileService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            ICacheService cacheService,
            ILogger<UserProfileService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Student?> GetStudentProfileAsync(string email, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"student_profile_{email}";
            var cached = await _cacheService.GetAsync<Student>(cacheKey, cancellationToken);
            if (cached != null)
            {
                _logger.LogDebug("Returning cached student profile for {email}", email);
                return cached;
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var student = await context.Students.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Email == email, cancellationToken);

            if (student != null)
            {
                await _cacheService.SetAsync(cacheKey, student, TimeSpan.FromMinutes(5), cancellationToken);
                _logger.LogDebug("Cached student profile for {email}", email);
            }

            return student;
        }

        public async Task<Company?> GetCompanyProfileAsync(string email, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"company_profile_{email}";
            var cached = await _cacheService.GetAsync<Company>(cacheKey, cancellationToken);
            if (cached != null)
            {
                _logger.LogDebug("Returning cached company profile for {email}", email);
                return cached;
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var company = await context.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.CompanyEmail == email, cancellationToken);

            if (company != null)
            {
                await _cacheService.SetAsync(cacheKey, company, TimeSpan.FromMinutes(5), cancellationToken);
                _logger.LogDebug("Cached company profile for {email}", email);
            }

            return company;
        }

        public async Task<Professor?> GetProfessorProfileAsync(string email, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"professor_profile_{email}";
            var cached = await _cacheService.GetAsync<Professor>(cacheKey, cancellationToken);
            if (cached != null)
            {
                _logger.LogDebug("Returning cached professor profile for {email}", email);
                return cached;
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var professor = await context.Professors.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProfEmail == email, cancellationToken);

            if (professor != null)
            {
                await _cacheService.SetAsync(cacheKey, professor, TimeSpan.FromMinutes(5), cancellationToken);
                _logger.LogDebug("Cached professor profile for {email}", email);
            }

            return professor;
        }

        public async Task<ResearchGroup?> GetResearchGroupProfileAsync(string email, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"research_group_profile_{email}";
            var cached = await _cacheService.GetAsync<ResearchGroup>(cacheKey, cancellationToken);
            if (cached != null)
            {
                _logger.LogDebug("Returning cached research group profile for {email}", email);
                return cached;
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var researchGroup = await context.ResearchGroups.AsNoTracking()
                .FirstOrDefaultAsync(r => r.ResearchGroupEmail == email, cancellationToken);

            if (researchGroup != null)
            {
                await _cacheService.SetAsync(cacheKey, researchGroup, TimeSpan.FromMinutes(5), cancellationToken);
                _logger.LogDebug("Cached research group profile for {email}", email);
            }

            return researchGroup;
        }

        public async Task InvalidateProfileCacheAsync(string email, CancellationToken cancellationToken = default)
        {
            var cacheKeys = new[]
            {
                $"student_profile_{email}",
                $"company_profile_{email}",
                $"professor_profile_{email}",
                $"research_group_profile_{email}"
            };

            foreach (var key in cacheKeys)
            {
                await _cacheService.RemoveAsync(key, cancellationToken);
            }

            _logger.LogInformation("Invalidated profile cache for {email}", email);
        }
    }
}
