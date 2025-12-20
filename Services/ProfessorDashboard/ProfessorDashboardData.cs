using System;
using System.Collections.Generic;
using QuizManager.Models;

namespace QuizManager.Services.ProfessorDashboard
{
    public class ProfessorDashboardData
    {
        public static ProfessorDashboardData Empty { get; } = new ProfessorDashboardData();

        public bool IsAuthenticated { get; init; }
        public bool IsRegisteredProfessor { get; init; }
        public string? Email { get; init; }
        public Professor? ProfessorProfile { get; init; }
        public DateTimeOffset? LastRefreshedUtc { get; init; }

        public IReadOnlyList<ProfessorThesis> Theses { get; init; } = Array.Empty<ProfessorThesis>();
        public IReadOnlyList<ProfessorInternship> Internships { get; init; } = Array.Empty<ProfessorInternship>();
        public IReadOnlyList<ProfessorEvent> Events { get; init; } = Array.Empty<ProfessorEvent>();
        public IReadOnlyList<AnnouncementAsProfessor> Announcements { get; init; } = Array.Empty<AnnouncementAsProfessor>();

        public IReadOnlyList<ProfessorThesisApplied> ThesisApplications { get; init; } = Array.Empty<ProfessorThesisApplied>();
        public IReadOnlyList<ProfessorInternshipApplied> InternshipApplications { get; init; } = Array.Empty<ProfessorInternshipApplied>();

        public IReadOnlyList<InterestInProfessorEvent> StudentEventInterests { get; init; } = Array.Empty<InterestInProfessorEvent>();
        public IReadOnlyList<InterestInProfessorEventAsCompany> CompanyEventInterests { get; init; } = Array.Empty<InterestInProfessorEventAsCompany>();

        public IReadOnlyList<CompanyEvent> CompanyEvents { get; init; } = Array.Empty<CompanyEvent>();
        public IReadOnlyList<CompanyThesis> CompanyTheses { get; init; } = Array.Empty<CompanyThesis>();

        public ProfessorDashboardLookups Lookups { get; init; } = ProfessorDashboardLookups.Empty;
    }

    public class ProfessorDashboardLookups
    {
        public static ProfessorDashboardLookups Empty { get; } = new ProfessorDashboardLookups();

        public IReadOnlyList<Area> Areas { get; init; } = Array.Empty<Area>();
        public IReadOnlyList<Skill> Skills { get; init; } = Array.Empty<Skill>();
        public IReadOnlyList<Company> Companies { get; init; } = Array.Empty<Company>();
        public IReadOnlyList<ResearchGroup> ResearchGroups { get; init; } = Array.Empty<ResearchGroup>();
    }

    // Filter classes
    public class StudentSearchFilter
    {
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? Department { get; set; }
        public string? School { get; set; }
        public string? AreasOfExpertise { get; set; }
        public string? Keywords { get; set; }
        public string? DegreeLevel { get; set; }
        public int MaxResults { get; set; } = 25;
    }

    public class CompanySearchFilter
    {
        public string? CompanyName { get; set; }
        public string? CompanyEmail { get; set; }
        public string? CompanyType { get; set; }
        public string? CompanyActivity { get; set; }
        public string? CompanyTown { get; set; }
        public string? CompanyAreas { get; set; }
        public string? CompanyDesiredSkills { get; set; }
        public int MaxResults { get; set; } = 25;
    }

    public class ResearchGroupSearchFilter
    {
        public string? Name { get; set; }
        public string? Areas { get; set; }
        public string? Skills { get; set; }
        public int MaxResults { get; set; } = 25;
    }

    public class CompanyThesisSearchFilter
    {
        public string? CompanyName { get; set; }
        public string? Title { get; set; }
        public string? Supervisor { get; set; }
        public string? Department { get; set; }
        public DateTime? EarliestStartDate { get; set; }
        public IReadOnlyList<string> RequiredSkills { get; set; } = Array.Empty<string>();
        public int MaxResults { get; set; } = 200;
    }

    // Result classes
    public class ApplicationDecisionRequest
    {
        public ApplicationType ApplicationType { get; set; }
        public long ApplicationRng { get; set; }
        public string StudentUniqueId { get; set; } = string.Empty;
        public ApplicationDecision Decision { get; set; }
    }

    public enum ApplicationType
    {
        Thesis,
        Internship
    }

    public enum ApplicationDecision
    {
        Accept,
        Reject,
        Cancel
    }

    public class MutationResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public int? EntityId { get; init; }

        public static MutationResult Succeeded(int? id = null) => new MutationResult
        {
            Success = true,
            EntityId = id
        };

        public static MutationResult Failed(string error) => new MutationResult
        {
            Success = false,
            Error = error
        };
    }

    public class AttachmentDownloadResult
    {
        public byte[] Data { get; init; } = Array.Empty<byte>();
        public string FileName { get; init; } = string.Empty;
        public string ContentType { get; init; } = string.Empty;
    }

    public class ResearchGroupDetails
    {
        public string ResearchGroupEmail { get; init; } = string.Empty;
        public IReadOnlyList<FacultyMemberInfo> FacultyMembers { get; init; } = Array.Empty<FacultyMemberInfo>();
        public IReadOnlyList<NonFacultyMemberInfo> NonFacultyMembers { get; init; } = Array.Empty<NonFacultyMemberInfo>();
        public IReadOnlyList<SpinOffCompanyInfo> SpinOffCompanies { get; init; } = Array.Empty<SpinOffCompanyInfo>();
        public int ActiveResearchActionsCount { get; init; }
        public int PatentsCount { get; init; }
    }

    public class FacultyMemberInfo
    {
        public string FullName { get; init; } = string.Empty;
        public string? Email { get; init; }
    }

    public class NonFacultyMemberInfo
    {
        public string FullName { get; init; } = string.Empty;
        public string? Email { get; init; }
    }

    public class SpinOffCompanyInfo
    {
        public string CompanyTitle { get; init; } = string.Empty;
        public string CompanyAFM { get; init; } = string.Empty;
    }
}

