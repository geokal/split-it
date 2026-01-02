using System.Collections.Generic;

namespace QuizManager.Services.AdminDashboard
{
    public class AdminAnalyticsData
    {
        public Dictionary<string, int> AreaDistribution { get; init; } = new();
        public Dictionary<string, int> SkillDistribution { get; init; } = new();
    }

    public class AdminDashboardData
    {
        public IReadOnlyList<StudentWithAuth0Details> Students { get; init; } = new List<StudentWithAuth0Details>();
        public AdminAnalyticsData Analytics { get; init; } = new();
    }

    public class StudentWithAuth0Details
    {
        public string Name { get; init; } = "";
        public string Surname { get; init; } = "";
        public string Email { get; init; } = "";
        public string Department { get; init; } = "";
        public string School { get; init; } = "";
        public string AreasOfExpertise { get; init; } = "";
        public string Keywords { get; init; } = "";
        public DateTime? LastProfileUpdate { get; init; }
        public DateTime? LastCVUpdate { get; init; }
    }
}
