using System.Collections.Generic;
using QuizManager.Models;

namespace QuizManager.Services.StudentDashboard
{
    public class StudentDashboardData
    {
        public static StudentDashboardData Empty { get; } = new StudentDashboardData();

        public bool IsAuthenticated { get; init; }
        public bool IsRegisteredStudent { get; init; }
        public string? Email { get; init; }
        public Student? Student { get; init; }

        public IReadOnlyList<CompanyThesisApplied> CompanyThesisApplications { get; init; } = new List<CompanyThesisApplied>();
        public IReadOnlyDictionary<long, CompanyThesis> CompanyThesisCache { get; init; } = new Dictionary<long, CompanyThesis>();

        public IReadOnlyList<ProfessorThesisApplied> ProfessorThesisApplications { get; init; } = new List<ProfessorThesisApplied>();
        public IReadOnlyDictionary<long, ProfessorThesis> ProfessorThesisCache { get; init; } = new Dictionary<long, ProfessorThesis>();

        public IReadOnlyList<CompanyJobApplied> JobApplications { get; init; } = new List<CompanyJobApplied>();
        public IReadOnlyDictionary<long, CompanyJob> JobCache { get; init; } = new Dictionary<long, CompanyJob>();

        public IReadOnlyList<InternshipApplied> CompanyInternshipApplications { get; init; } = new List<InternshipApplied>();
        public IReadOnlyDictionary<string, AllInternships> CompanyInternshipCache { get; init; } = new Dictionary<string, AllInternships>();

        public IReadOnlyList<ProfessorInternshipApplied> ProfessorInternshipApplications { get; init; } = new List<ProfessorInternshipApplied>();
        public IReadOnlyDictionary<string, AllInternships> ProfessorInternshipCache { get; init; } = new Dictionary<string, AllInternships>();

        public HashSet<long> CompanyThesisIdsApplied { get; init; } = new HashSet<long>();
        public HashSet<long> ProfessorThesisIdsApplied { get; init; } = new HashSet<long>();
        public HashSet<long> JobIdsApplied { get; init; } = new HashSet<long>();
        public HashSet<long> CompanyInternshipIdsApplied { get; init; } = new HashSet<long>();
        public HashSet<long> ProfessorInternshipIdsApplied { get; init; } = new HashSet<long>();

        public HashSet<long> CompanyEventInterestIds { get; init; } = new HashSet<long>();
        public HashSet<long> ProfessorEventInterestIds { get; init; } = new HashSet<long>();
        public IReadOnlyList<InterestInCompanyEvent> CompanyEventInterests { get; init; } = new List<InterestInCompanyEvent>();
        public IReadOnlyList<InterestInProfessorEvent> ProfessorEventInterests { get; init; } = new List<InterestInProfessorEvent>();
    }

    public record StudentCompanySearchRequest
    {
        public string? Email { get; init; }
        public string? Name { get; init; }
        public string? Type { get; init; }
        public string? Activity { get; init; }
        public string? Town { get; init; }
        public IReadOnlyList<string> Areas { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> DesiredSkills { get; init; } = Array.Empty<string>();
    }
}

