using QuizManager.Models;

namespace QuizManager.Services.StudentDashboard
{
    public record ProfessorEventInterestResult(
        ProfessorEvent ProfessorEvent,
        Professor? Professor,
        Student Student,
        bool NeedsTransport,
        string? ChosenLocation);
}

