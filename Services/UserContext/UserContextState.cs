using System;
using QuizManager.Models;

namespace QuizManager.Services.UserContext
{
    public record UserContextState(
        bool IsAuthenticated,
        string Role,
        string Email,
        bool IsStudentRegistered,
        bool IsCompanyRegistered,
        bool IsProfessorRegistered,
        bool IsResearchGroupRegistered,
        Student? Student,
        Company? Company,
        Professor? Professor,
        ResearchGroup? ResearchGroup,
        DateTimeOffset RetrievedAt)
    {
        public static UserContextState Anonymous { get; } = new(false, string.Empty, string.Empty, false, false, false, false, null, null, null, null, DateTimeOffset.MinValue);
    }
}

