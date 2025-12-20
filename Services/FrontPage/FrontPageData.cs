using System.Collections.Generic;
using QuizManager.Models;

namespace QuizManager.Services.FrontPage
{
    public class FrontPageData
    {
        public static FrontPageData Empty { get; } = new FrontPageData();

        public IReadOnlyList<CompanyEvent> CompanyEvents { get; init; } = Array.Empty<CompanyEvent>();
        
        public IReadOnlyList<ProfessorEvent> ProfessorEvents { get; init; } = Array.Empty<ProfessorEvent>();
        
        public IReadOnlyList<AnnouncementAsCompany> CompanyAnnouncements { get; init; } = Array.Empty<AnnouncementAsCompany>();
        
        public IReadOnlyList<AnnouncementAsProfessor> ProfessorAnnouncements { get; init; } = Array.Empty<AnnouncementAsProfessor>();
        
        public IReadOnlyList<AnnouncementAsResearchGroup> ResearchGroupAnnouncements { get; init; } = Array.Empty<AnnouncementAsResearchGroup>();
    }
}

