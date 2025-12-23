using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QuizManager.Models;

namespace QuizManager.Services.StudentDashboard
{
    public interface IStudentDashboardService
    {
        Task<StudentDashboardData> LoadDashboardDataAsync(CancellationToken cancellationToken = default);
        
        Task RefreshDashboardCacheAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> GetJobTitleSuggestionsAsync(string searchTerm, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> GetCompanyNameSuggestionsAsync(string searchTerm, CancellationToken cancellationToken = default);

        Task<bool> WithdrawCompanyThesisApplicationAsync(long rngForThesisApplied, CancellationToken cancellationToken = default);

        Task<bool> WithdrawProfessorThesisApplicationAsync(long rngForThesisApplied, CancellationToken cancellationToken = default);

        Task<bool> WithdrawCompanyInternshipApplicationAsync(long rngForInternshipApplied, CancellationToken cancellationToken = default);

        Task<bool> WithdrawProfessorInternshipApplicationAsync(long rngForInternshipApplied, CancellationToken cancellationToken = default);

        Task<bool> WithdrawJobApplicationAsync(long rngForJobApplied, CancellationToken cancellationToken = default);

        Task<(byte[] Data, string FileName)?> GetCompanyInternshipAttachmentAsync(long internshipId, CancellationToken cancellationToken = default);

        Task<Company?> GetCompanyByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<Professor?> GetProfessorByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<Dictionary<long, CompanyInternship>> GetCompanyInternshipsByIdsAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default);

        Task<Dictionary<long, ProfessorInternship>> GetProfessorInternshipsByIdsAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default);

        Task<CompanyInternship?> GetCompanyInternshipByRngAsync(long rng, CancellationToken cancellationToken = default);

        Task<ProfessorInternship?> GetProfessorInternshipByRngAsync(long rng, CancellationToken cancellationToken = default);

        Task<Student?> GetStudentByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<Student?> GetStudentByUniqueIdAsync(string uniqueId, CancellationToken cancellationToken = default);

        Task<CompanyEventInterestResult?> ShowInterestInCompanyEventAsync(long eventRng, bool needsTransport, string? chosenLocation, CancellationToken cancellationToken = default);

        Task<bool> WithdrawCompanyEventInterestAsync(long eventRng, CancellationToken cancellationToken = default);

        Task<ProfessorEventInterestResult?> ShowInterestInProfessorEventAsync(long eventRng, bool needsTransport, string? chosenLocation, CancellationToken cancellationToken = default);

        Task<bool> WithdrawProfessorEventInterestAsync(long eventRng, CancellationToken cancellationToken = default);
    }
}
