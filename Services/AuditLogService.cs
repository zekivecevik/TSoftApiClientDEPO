using TSoftApiClient.Models;

namespace TSoftApiClient.Services
{
    /// <summary>
    /// Audit Log Service - Tüm kullanıcı işlemlerini kaydet
    /// </summary>
    public class AuditLogService
    {
        private readonly ILogger<AuditLogService> _logger;
        private static readonly List<AuditLog> _logs = new();
        private static int _nextId = 1;

        public AuditLogService(ILogger<AuditLogService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Log kaydı oluştur
        /// </summary>
        public void Log(
            int? userId,
            string username,
            string action,
            string entity,
            string? entityId = null,
            string? details = null,
            string ipAddress = "",
            string userAgent = "",
            bool success = true)
        {
            try
            {
                var log = new AuditLog
                {
                    Id = _nextId++,
                    UserId = userId,
                    Username = username,
                    Action = action,
                    Entity = entity,
                    EntityId = entityId,
                    Details = details,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Success = success,
                    Timestamp = DateTime.UtcNow
                };

                _logs.Add(log);

                var logIcon = success ? "✅" : "❌";
                _logger.LogInformation(
                    "{Icon} AUDIT [{Action}] {Entity} by {Username} - Success: {Success}",
                    logIcon, action, entity, username, success);

                if (!string.IsNullOrEmpty(details))
                {
                    _logger.LogDebug("   Details: {Details}", details);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Failed to create audit log");
            }
        }

        /// <summary>
        /// Login log'u
        /// </summary>
        public void LogLogin(string username, bool success, string ipAddress, string userAgent)
        {
            Log(
                userId: null,
                username: username,
                action: "Login",
                entity: "Auth",
                details: success ? "Successful login" : "Failed login attempt",
                ipAddress: ipAddress,
                userAgent: userAgent,
                success: success
            );
        }

        /// <summary>
        /// Logout log'u
        /// </summary>
        public void LogLogout(int userId, string username, string ipAddress)
        {
            Log(
                userId: userId,
                username: username,
                action: "Logout",
                entity: "Auth",
                ipAddress: ipAddress,
                success: true
            );
        }

        /// <summary>
        /// View log'u
        /// </summary>
        public void LogView(int userId, string username, string entity, string? entityId = null, string ipAddress = "")
        {
            Log(
                userId: userId,
                username: username,
                action: "View",
                entity: entity,
                entityId: entityId,
                ipAddress: ipAddress
            );
        }

        /// <summary>
        /// Create log'u
        /// </summary>
        public void LogCreate(int userId, string username, string entity, string? entityId = null, string? details = null, string ipAddress = "")
        {
            Log(
                userId: userId,
                username: username,
                action: "Create",
                entity: entity,
                entityId: entityId,
                details: details,
                ipAddress: ipAddress
            );
        }

        /// <summary>
        /// Update log'u
        /// </summary>
        public void LogUpdate(int userId, string username, string entity, string? entityId = null, string? details = null, string ipAddress = "")
        {
            Log(
                userId: userId,
                username: username,
                action: "Update",
                entity: entity,
                entityId: entityId,
                details: details,
                ipAddress: ipAddress
            );
        }

        /// <summary>
        /// Delete log'u
        /// </summary>
        public void LogDelete(int userId, string username, string entity, string? entityId = null, string? details = null, string ipAddress = "")
        {
            Log(
                userId: userId,
                username: username,
                action: "Delete",
                entity: entity,
                entityId: entityId,
                details: details,
                ipAddress: ipAddress
            );
        }

        /// <summary>
        /// Tüm logları getir
        /// </summary>
        public List<AuditLog> GetLogs(int page = 1, int pageSize = 100)
        {
            return _logs
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        /// <summary>
        /// Kullanıcıya göre logları getir
        /// </summary>
        public List<AuditLog> GetLogsByUser(int userId, int page = 1, int pageSize = 50)
        {
            return _logs
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        /// <summary>
        /// Aksiyona göre logları getir
        /// </summary>
        public List<AuditLog> GetLogsByAction(string action, int page = 1, int pageSize = 50)
        {
            return _logs
                .Where(l => l.Action.Equals(action, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        /// <summary>
        /// Entity'ye göre logları getir
        /// </summary>
        public List<AuditLog> GetLogsByEntity(string entity, int page = 1, int pageSize = 50)
        {
            return _logs
                .Where(l => l.Entity.Equals(entity, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        /// <summary>
        /// Tarih aralığına göre logları getir
        /// </summary>
        public List<AuditLog> GetLogsByDateRange(DateTime startDate, DateTime endDate, int page = 1, int pageSize = 100)
        {
            return _logs
                .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        /// <summary>
        /// İstatistikler
        /// </summary>
        public object GetStatistics()
        {
            var totalLogs = _logs.Count;
            var successfulActions = _logs.Count(l => l.Success);
            var failedActions = _logs.Count(l => !l.Success);

            var topUsers = _logs
                .GroupBy(l => l.Username)
                .Select(g => new
                {
                    Username = g.Key,
                    ActionCount = g.Count()
                })
                .OrderByDescending(x => x.ActionCount)
                .Take(10)
                .ToList();

            var topActions = _logs
                .GroupBy(l => l.Action)
                .Select(g => new
                {
                    Action = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            var topEntities = _logs
                .GroupBy(l => l.Entity)
                .Select(g => new
                {
                    Entity = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            var recentLogs = _logs
                .OrderByDescending(l => l.Timestamp)
                .Take(20)
                .ToList();

            return new
            {
                TotalLogs = totalLogs,
                SuccessfulActions = successfulActions,
                FailedActions = failedActions,
                SuccessRate = totalLogs > 0 ? (double)successfulActions / totalLogs * 100 : 0,
                TopUsers = topUsers,
                TopActions = topActions,
                TopEntities = topEntities,
                RecentLogs = recentLogs
            };
        }

        /// <summary>
        /// Log temizleme (eski kayıtları sil)
        /// </summary>
        public int CleanOldLogs(int daysToKeep = 90)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            var removedCount = _logs.RemoveAll(l => l.Timestamp < cutoffDate);

            _logger.LogInformation("🗑️ Cleaned {Count} old audit logs (older than {Days} days)", removedCount, daysToKeep);

            return removedCount;
        }
    }
}