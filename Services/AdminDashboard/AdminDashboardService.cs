using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuizManager.Data;
using QuizManager.Models;
using QuizManager.Services.Authentication;
using QuizManager.Data;

namespace QuizManager.Services.AdminDashboard
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ICacheService _cacheService;
        private const string CacheKeyStudents = "admin_students";
        private const string CacheKeyAnalytics = "admin_analytics";

        public AdminDashboardService(IDbContextFactory<AppDbContext> dbFactory, ICacheService cacheService)
        {
            _dbFactory = dbFactory;
            _cacheService = cacheService;
        }

        public async Task<IReadOnlyList<StudentWithAuth0Details>> GetStudentsWithAuth0DetailsAsync(CancellationToken cancellationToken = default)
        {
            var cacheKey = $"{CacheKeyStudents}_all";
            
            var cached = await _cacheService.GetAsync<List<StudentWithAuth0Details>>(cacheKey, cancellationToken);
            if (cached != null)
            {
                return cached.AsReadOnly();
            }

            using var context = _dbFactory.CreateDbContext();
            
            var students = await context.Students
                .AsNoTracking()
                .OrderBy(s => s.Email)
                .ToListAsync(cancellationToken);

            var studentsWithDetails = students.Select(s => new StudentWithAuth0Details
            {
                Name = s.Name,
                Surname = s.Surname,
                Email = s.Email,
                Department = s.Department,
                School = s.School,
                AreasOfExpertise = s.AreasOfExpertise,
                Keywords = s.Keywords,
                LastProfileUpdate = s.LastProfileUpdate,
                LastCVUpdate = s.LastCVUpdate
            }).ToList();

            await _cacheService.SetAsync(cacheKey, studentsWithDetails, TimeSpan.FromMinutes(30), cancellationToken);
            return studentsWithDetails.AsReadOnly();
        }

        public async Task<AdminAnalyticsData> GetAnalyticsDataAsync(CancellationToken cancellationToken = default)
        {
            var cacheKey = $"{CacheKeyAnalytics}_all";
            
            var cached = await _cacheService.GetAsync<AdminAnalyticsData>(cacheKey, cancellationToken);
            if (cached != null)
            {
                return cached;
            }

            using var context = _dbFactory.CreateDbContext();

            var rawData = await context.Students
                .AsNoTracking()
                .Where(s => s.AreasOfExpertise != null || s.Keywords != null)
                .Select(s => new { s.AreasOfExpertise, s.Keywords })
                .ToListAsync(cancellationToken);

            var tempAreaDist = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var tempSkillDist = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in rawData)
            {
                if (!string.IsNullOrWhiteSpace(item.AreasOfExpertise))
                {
                    var areas = item.AreasOfExpertise.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var area in areas)
                    {
                        var trimmed = area.Trim();
                        if (tempAreaDist.ContainsKey(trimmed))
                            tempAreaDist[trimmed]++;
                        else
                            tempAreaDist[trimmed] = 1;
                    }
                }

                if (!string.IsNullOrWhiteSpace(item.Keywords))
                {
                    var skills = item.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var skill in skills)
                    {
                        var trimmed = skill.Trim();
                        if (tempSkillDist.ContainsKey(trimmed))
                            tempSkillDist[trimmed]++;
                        else
                            tempSkillDist[trimmed] = 1;
                    }
                }
            }

            var analyticsData = new AdminAnalyticsData
            {
                AreaDistribution = tempAreaDist,
                SkillDistribution = tempSkillDist
            };

            await _cacheService.SetAsync(cacheKey, analyticsData, TimeSpan.FromMinutes(30), cancellationToken);
            return analyticsData;
        }
    }
}
