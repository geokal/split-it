using System;
using System.Collections.Generic;
using QuizManager.Models;

namespace QuizManager.Services.FrontPage
{
    public record FrontPageNewsArticle(string Title, string Url, string? Date, string? Category);

    public record WeatherSnapshot(double TemperatureCelsius, bool IsDaytime, string? ConditionText);

    public record FrontPageDataState(
        IReadOnlyList<FrontPageNewsArticle> UoaNews,
        IReadOnlyList<FrontPageNewsArticle> SvseNews,
        IReadOnlyList<AnnouncementAsCompany> CompanyAnnouncements,
        IReadOnlyList<AnnouncementAsProfessor> ProfessorAnnouncements,
        IReadOnlyList<AnnouncementAsResearchGroup> ResearchGroupAnnouncements,
        IReadOnlyList<CompanyEvent> CompanyEvents,
        IReadOnlyList<ProfessorEvent> ProfessorEvents,
        WeatherSnapshot? Weather,
        DateTimeOffset LastUpdated)
    {
        public static FrontPageDataState Empty { get; } = new(
            Array.Empty<FrontPageNewsArticle>(),
            Array.Empty<FrontPageNewsArticle>(),
            Array.Empty<AnnouncementAsCompany>(),
            Array.Empty<AnnouncementAsProfessor>(),
            Array.Empty<AnnouncementAsResearchGroup>(),
            Array.Empty<CompanyEvent>(),
            Array.Empty<ProfessorEvent>(),
            null,
            DateTimeOffset.MinValue);

        public bool IsLoaded => LastUpdated > DateTimeOffset.MinValue;
    }
}

