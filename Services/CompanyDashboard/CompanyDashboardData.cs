using System;
using System.Collections.Generic;
using QuizManager.Models;

namespace QuizManager.Services.CompanyDashboard
{
    public class CompanyDashboardData
    {
        public static CompanyDashboardData Empty { get; } = new CompanyDashboardData();

        public bool IsAuthenticated { get; init; }
        public bool IsRegisteredCompany { get; init; }
        public string? Email { get; init; }
        public Company? CompanyProfile { get; init; }
        public DateTimeOffset? LastRefreshedUtc { get; init; }

        public IReadOnlyList<CompanyJob> Jobs { get; init; } = Array.Empty<CompanyJob>();
        public IReadOnlyList<CompanyInternship> Internships { get; init; } = Array.Empty<CompanyInternship>();
        public IReadOnlyList<CompanyThesis> Theses { get; init; } = Array.Empty<CompanyThesis>();
        public IReadOnlyList<AnnouncementAsCompany> Announcements { get; init; } = Array.Empty<AnnouncementAsCompany>();
        public IReadOnlyList<CompanyEvent> CompanyEvents { get; init; } = Array.Empty<CompanyEvent>();
        public IReadOnlyList<ProfessorEvent> ProfessorEvents { get; init; } = Array.Empty<ProfessorEvent>();
        public IReadOnlyList<AnnouncementAsResearchGroup> ResearchGroupAnnouncements { get; init; } = Array.Empty<AnnouncementAsResearchGroup>();

        public IReadOnlyList<CompanyJobApplied> JobApplications { get; init; } = Array.Empty<CompanyJobApplied>();
        public IReadOnlyList<InternshipApplied> InternshipApplications { get; init; } = Array.Empty<InternshipApplied>();
        public IReadOnlyList<CompanyThesisApplied> ThesisApplications { get; init; } = Array.Empty<CompanyThesisApplied>();

        public IReadOnlyList<InterestInProfessorEventAsCompany> InterestInProfessorEvents { get; init; } = Array.Empty<InterestInProfessorEventAsCompany>();
        public IReadOnlyList<InterestInCompanyEventAsProfessor> InterestInCompanyEvents { get; init; } = Array.Empty<InterestInCompanyEventAsProfessor>();

        public CompanyDashboardLookups Lookups { get; init; } = CompanyDashboardLookups.Empty;
    }

    public class CompanyDashboardLookups
    {
        public static CompanyDashboardLookups Empty { get; } = new CompanyDashboardLookups();

        public IReadOnlyList<Area> Areas { get; init; } = Array.Empty<Area>();
        public IReadOnlyList<Skill> Skills { get; init; } = Array.Empty<Skill>();
        public IReadOnlyList<Professor> Professors { get; init; } = Array.Empty<Professor>();
        public IReadOnlyList<ResearchGroup> ResearchGroups { get; init; } = Array.Empty<ResearchGroup>();
        public IReadOnlyList<string> Regions { get; init; } = Array.Empty<string>();
        public IReadOnlyDictionary<string, IReadOnlyList<string>> RegionToTownsMap { get; init; }
            = new Dictionary<string, IReadOnlyList<string>>();
    }

    // Filter classes for search operations
    public class ProfessorSearchFilter
    {
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public string? Department { get; set; }
        public string? School { get; set; }
        public string? AreasOfInterest { get; set; }
        public int MaxResults { get; set; } = 25;
    }

    public class ResearchGroupSearchFilter
    {
        public string? Name { get; set; }
        public string? Areas { get; set; }
        public string? Skills { get; set; }
        public int MaxResults { get; set; } = 25;
    }

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

    public class ProfessorThesisSearchFilter
    {
        public string? ProfessorName { get; set; }
        public string? ProfessorSurname { get; set; }
        public string? ThesisTitle { get; set; }
        public DateTime? EarliestStartDate { get; set; }
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
        Job,
        Internship,
        Thesis
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

    public class ApplicantDetailsRequest
    {
        public ApplicationType ApplicationType { get; set; }
        public long ApplicationRng { get; set; }
    }

    public class ApplicantDetailsResult
    {
        public ApplicationType ApplicationType { get; init; }
        public IReadOnlyList<Student> Students { get; init; } = Array.Empty<Student>();
        public IReadOnlyList<CompanyJobApplied> JobApplications { get; init; } = Array.Empty<CompanyJobApplied>();
        public IReadOnlyList<InternshipApplied> InternshipApplications { get; init; } = Array.Empty<InternshipApplied>();
        public IReadOnlyList<CompanyThesisApplied> ThesisApplications { get; init; } = Array.Empty<CompanyThesisApplied>();
    }

    public class CompanyEventInterestDetails
    {
        public InterestInCompanyEvent Application { get; init; } = null!;
        public Student? Student { get; init; }
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

    public class ResearchGroupDetails
    {
        public string ResearchGroupEmail { get; init; } = string.Empty;
        public IReadOnlyList<FacultyMemberInfo> FacultyMembers { get; init; } = Array.Empty<FacultyMemberInfo>();
        public IReadOnlyList<NonFacultyMemberInfo> NonFacultyMembers { get; init; } = Array.Empty<NonFacultyMemberInfo>();
        public IReadOnlyList<SpinOffCompanyInfo> SpinOffCompanies { get; init; } = Array.Empty<SpinOffCompanyInfo>();
        public int ActiveResearchActionsCount { get; init; }
        public int PatentsCount { get; init; }
    }
}

