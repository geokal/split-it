using System;
using System.Collections.Generic;
using QuizManager.Models;

namespace QuizManager.Services.ResearchGroupDashboard
{
    public class ResearchGroupDashboardData
    {
        public static ResearchGroupDashboardData Empty { get; } = new ResearchGroupDashboardData();

        public bool IsAuthenticated { get; init; }
        public bool IsRegisteredResearchGroup { get; init; }
        public string? Email { get; init; }
        public ResearchGroup? ResearchGroupProfile { get; init; }
        public DateTimeOffset? LastRefreshedUtc { get; init; }

        public IReadOnlyList<AnnouncementAsResearchGroup> Announcements { get; init; } = Array.Empty<AnnouncementAsResearchGroup>();
        public IReadOnlyList<ResearchGroupFacultyMemberInfo> FacultyMembers { get; init; } = Array.Empty<ResearchGroupFacultyMemberInfo>();
        public IReadOnlyList<ResearchGroupMemberInfo> NonFacultyMembers { get; init; } = Array.Empty<ResearchGroupMemberInfo>();
        public IReadOnlyList<ResearchGroupResearchActionInfo> ResearchActions { get; init; } = Array.Empty<ResearchGroupResearchActionInfo>();
        public IReadOnlyList<ResearchGroupPatentInfo> Patents { get; init; } = Array.Empty<ResearchGroupPatentInfo>();
        public IReadOnlyList<ResearchGroupSpinOffInfo> SpinOffCompanies { get; init; } = Array.Empty<ResearchGroupSpinOffInfo>();

        public ResearchGroupDashboardStatistics Statistics { get; init; } = ResearchGroupDashboardStatistics.Empty;
        public ResearchGroupDashboardLookups Lookups { get; init; } = ResearchGroupDashboardLookups.Empty;
    }

    public class ResearchGroupDashboardStatistics
    {
        public static ResearchGroupDashboardStatistics Empty { get; } = new ResearchGroupDashboardStatistics();

        public int FacultyMembers { get; init; }
        public int Collaborators { get; init; }
        public int ActiveResearchActions { get; init; }
        public int InactiveResearchActions { get; init; }
        public int ActivePatents { get; init; }
        public int InactivePatents { get; init; }
    }

    public class ResearchGroupDashboardLookups
    {
        public static ResearchGroupDashboardLookups Empty { get; } = new ResearchGroupDashboardLookups();

        public IReadOnlyList<Area> Areas { get; init; } = Array.Empty<Area>();
        public IReadOnlyList<Skill> Skills { get; init; } = Array.Empty<Skill>();
        public IReadOnlyList<string> Regions { get; init; } = Array.Empty<string>();
        public IReadOnlyDictionary<string, IReadOnlyList<string>> RegionToTownsMap { get; init; }
            = new Dictionary<string, IReadOnlyList<string>>();
    }

    public record ResearchGroupFacultyMemberInfo(
        string FirstName,
        string LastName,
        string? Email,
        string? School,
        string? Department,
        byte[]? Image,
        string? Role,
        string? ScholarProfile);

    public record ResearchGroupMemberInfo(
        string FirstName,
        string LastName,
        string? Email,
        string? School,
        string? Department,
        string? LevelOfStudies,
        byte[]? Image,
        DateTime? RegistrationDate,
        string? Role,
        string? ScholarProfile);

    public record ResearchGroupResearchActionInfo(
        string ProjectTitle,
        string? ProjectAcronym,
        string? GrantAgreementNumber,
        DateTime? StartDate,
        DateTime? EndDate,
        string? ProjectCoordinator,
        string? ElkeCode,
        string? ScientificResponsibleEmail,
        string? ProjectStatus);

    public record ResearchGroupPatentInfo(
        string PatentTitle,
        string? PatentType,
        string? PatentDoi,
        string? PatentUrl,
        string? PatentDescription,
        string? PatentStatus,
        DateTime? FilingDate);

    public record ResearchGroupSpinOffInfo(
        string CompanyTitle,
        string CompanyAfm,
        string? CompanyDescription,
        string? CompanyWebsite);

    public record ResearchGroupAnnouncementRequest
    {
        public long? Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Status { get; init; } = "Μη Δημοσιευμένη";
        public DateTime TimeToBeActive { get; init; } = DateTime.Today;
        public byte[]? Attachment { get; init; }
        public string? AttachmentContentType { get; init; }
        public string? AttachmentFileName { get; init; }
    }

    public class MutationResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public long? EntityId { get; init; }

        public static MutationResult Succeeded(long? id = null) => new MutationResult
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
        public string ContentType { get; init; } = "application/octet-stream";
    }

    public record ResearchGroupCompanySearchRequest
    {
        public string? Email { get; init; }
        public string? Name { get; init; }
        public string? Type { get; init; }
        public string? Activity { get; init; }
        public string? Town { get; init; }
        public IReadOnlyList<string> Areas { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> DesiredSkills { get; init; } = Array.Empty<string>();
    }

    public record ResearchGroupProfessorSearchRequest
    {
        public string? NameOrSurname { get; init; }
        public string? School { get; init; }
        public string? Department { get; init; }
        public IReadOnlyList<string> Areas { get; init; } = Array.Empty<string>();
    }
}

