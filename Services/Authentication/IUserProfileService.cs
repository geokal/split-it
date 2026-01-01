using Microsoft.EntityFrameworkCore;
using QuizManager.Data;
using QuizManager.Models;
using System.Threading;
using System.Threading.Tasks;

namespace QuizManager.Services.Authentication
{
    /// <summary>
    /// Service for retrieving and caching user profile data
    /// </summary>
    public interface IUserProfileService
    {
        /// <summary>
        /// Gets the student profile for the current user
        /// </summary>
        Task<Student?> GetStudentProfileAsync(string email, CancellationToken cancellationToken);
    
        /// <summary>
        /// Gets the company profile for the current user
        /// </summary>
        Task<Company?> GetCompanyProfileAsync(string email, CancellationToken cancellationToken);
    
        /// <summary>
        /// Gets the professor profile for the current user
        /// </summary>
        Task<Professor?> GetProfessorProfileAsync(string email, CancellationToken cancellationToken);
    
        /// <summary>
        /// Gets the research group profile for the current user
        /// </summary>
        Task<ResearchGroup?> GetResearchGroupProfileAsync(string email, CancellationToken cancellationToken);
    
        /// <summary>
        /// Invalidates cached profile data for a specific user
        /// </summary>
        Task InvalidateProfileCacheAsync(string email, CancellationToken cancellationToken = default);
    }
}
