using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuizManager.Data;
using QuizManager.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuizManager.Services.Authentication
{
    /// <summary>
    /// Repository implementation for audit log CRUD operations
    /// </summary>
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<AuditLogRepository> _logger;

        public AuditLogRepository(
            IDbContextFactory<AppDbContext> dbContextFactory,
            ILogger<AuditLogRepository> logger
        )
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<AuditLog> AddAsync(
            AuditLog auditLog,
            CancellationToken cancellationToken = default
        )
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            context.AuditLogs.Add(auditLog);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Audit log entry created: {Email} - {Action} - {Role}",
                auditLog.Email,
                auditLog.Action,
                auditLog.Role
            );

            return auditLog;
        }

        public async Task<List<AuditLog>> GetByEmailAsync(
            string email,
            int count = 100,
            CancellationToken cancellationToken = default
        )
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await context.AuditLogs
                .AsNoTracking()
                .Where(al => al.Email == email)
                .OrderByDescending(al => al.Timestamp)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<AuditLog>> GetByActionAsync(
            string action,
            int count = 100,
            CancellationToken cancellationToken = default
        )
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await context.AuditLogs
                .AsNoTracking()
                .Where(al => al.Action == action)
                .OrderByDescending(al => al.Timestamp)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<AuditLog>> GetRecentAsync(
            int count = 50,
            CancellationToken cancellationToken = default
        )
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await context.AuditLogs
                .AsNoTracking()
                .OrderByDescending(al => al.Timestamp)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<AuditLog>> GetFailedAttemptsAsync(
            int count = 50,
            CancellationToken cancellationToken = default
        )
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await context.AuditLogs
                .AsNoTracking()
                .Where(al => !al.Success)
                .OrderByDescending(al => al.Timestamp)
                .Take(count)
                .ToListAsync(cancellationToken);
        }
    }
}
