using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Social.Core.Entities;
using Social.Core.Interfaces;
using Social.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Social.Infrastructure.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditLogRepository>? _logger;

        public AuditLogRepository(ApplicationDbContext context, ILogger<AuditLogRepository>? logger = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger;
        }

        public async Task AddAsync(AuditLog log, CancellationToken cancellationToken = default)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));

            try
            {
                await _context.AuditLogs.AddAsync(log, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to persist audit log entry for action {ActionType} on {TargetEntity}:{TargetId}", log.ActionType, log.TargetEntity, log.TargetId);
            }
        }

        public async Task<(List<AuditLog> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            string? actionType = null,
            string? adminId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.AuditLogs.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(actionType))
                {
                    query = query.Where(a => a.ActionType == actionType);
                }

                if (!string.IsNullOrWhiteSpace(adminId))
                {
                    query = query.Where(a => a.AdminId == adminId);
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(a => a.TimestampUtc >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(a => a.TimestampUtc <= toDate.Value);
                }

                var totalCount = await query.CountAsync(cancellationToken);

                var items = await query
                    .OrderByDescending(a => a.TimestampUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "AuditLogs query failed or table is unavailable. Returning empty audit trail.");
                return (new List<AuditLog>(), 0);
            }
        }
    }
}
