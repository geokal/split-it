using QuizManager.Models;
using System.Threading;
using System.Threading.Tasks;

namespace QuizManager.Services.Authentication
{
    /// <summary>
    /// Repository interface for audit log CRUD operations
    /// </summary>
    public interface IAuditLogRepository
    {
        /// <summary>
        /// Add a new audit log entry
        /// </summary>
        Task<AuditLog> AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get audit logs for a specific email
        /// </summary>
        Task<List<AuditLog>> GetByEmailAsync(string email, int count = 100, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get audit logs for a specific action type
        /// </summary>
        Task<List<AuditLog>> GetByActionAsync(string action, int count = 100, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get recent audit logs
        /// </summary>
        Task<List<AuditLog>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get failed authentication attempts
        /// </summary>
        Task<List<AuditLog>> GetFailedAttemptsAsync(int count = 50, CancellationToken cancellationToken = default);
    }
}
