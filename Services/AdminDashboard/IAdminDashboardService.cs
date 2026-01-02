using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QuizManager.Models;

namespace QuizManager.Services.AdminDashboard
{
    public interface IAdminDashboardService
    {
        Task<IReadOnlyList<StudentWithAuth0Details>> GetStudentsWithAuth0DetailsAsync(CancellationToken cancellationToken = default);
        Task<AdminAnalyticsData> GetAnalyticsDataAsync(CancellationToken cancellationToken = default);
    }
}
