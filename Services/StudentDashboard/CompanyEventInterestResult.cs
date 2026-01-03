using QuizManager.Models;

namespace QuizManager.Services.StudentDashboard
{
    public record CompanyEventInterestResult(
        CompanyEvent CompanyEvent,
        Company? Company,
        Student Student,
        bool NeedsTransport,
        string? ChosenLocation);
}

