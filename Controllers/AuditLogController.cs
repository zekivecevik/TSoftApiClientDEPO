using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSoftApiClient.Models;
using TSoftApiClient.Services;

namespace TSoftApiClient.Controllers
{
    /// <summary>
    /// Audit Log API Controller - Sistem loglarýný yönet
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public class AuditLogController : ControllerBase
    {
        private readonly AuditLogService _auditLog;
        private readonly ILogger<AuditLogController> _logger;

        public AuditLogController(
            AuditLogService auditLog,
            ILogger<AuditLogController> logger)
        {
            _auditLog = auditLog;
            _logger = logger;
        }

        /// <summary>
        /// Tüm loglarý listele
        /// </summary>
        [HttpGet]
        public IActionResult GetLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            var logs = _auditLog.GetLogs(page, pageSize);
            return Ok(new { success = true, data = logs, page, pageSize });
        }

        /// <summary>
        /// Kullanýcýya göre loglarý getir
        /// </summary>
        [HttpGet("by-user/{userId}")]
        public IActionResult GetLogsByUser(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var logs = _auditLog.GetLogsByUser(userId, page, pageSize);
            return Ok(new { success = true, data = logs, page, pageSize });
        }

        /// <summary>
        /// Aksiyona göre loglarý getir
        /// </summary>
        [HttpGet("by-action/{action}")]
        public IActionResult GetLogsByAction(string action, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var logs = _auditLog.GetLogsByAction(action, page, pageSize);
            return Ok(new { success = true, data = logs, page, pageSize });
        }

        /// <summary>
        /// Entity'ye göre loglarý getir
        /// </summary>
        [HttpGet("by-entity/{entity}")]
        public IActionResult GetLogsByEntity(string entity, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var logs = _auditLog.GetLogsByEntity(entity, page, pageSize);
            return Ok(new { success = true, data = logs, page, pageSize });
        }

        /// <summary>
        /// Tarih aralýðýna göre loglarý getir
        /// </summary>
        [HttpGet("by-date-range")]
        public IActionResult GetLogsByDateRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100)
        {
            var logs = _auditLog.GetLogsByDateRange(startDate, endDate, page, pageSize);
            return Ok(new { success = true, data = logs, page, pageSize });
        }

        /// <summary>
        /// Ýstatistikler
        /// </summary>
        [HttpGet("statistics")]
        public IActionResult GetStatistics()
        {
            var stats = _auditLog.GetStatistics();
            return Ok(new { success = true, data = stats });
        }

        /// <summary>
        /// Eski loglarý temizle (Sadece Admin)
        /// </summary>
        [HttpDelete("clean-old")]
        [Authorize(Roles = Roles.Admin)]
        public IActionResult CleanOldLogs([FromQuery] int daysToKeep = 90)
        {
            var removedCount = _auditLog.CleanOldLogs(daysToKeep);

            var currentUsername = User.Identity?.Name ?? "System";
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            _auditLog.Log(
                currentUserId,
                currentUsername,
                "CleanOldLogs",
                "AuditLog",
                details: $"Cleaned {removedCount} logs older than {daysToKeep} days",
                ipAddress: ipAddress
            );

            return Ok(new
            {
                success = true,
                message = $"{removedCount} eski log kaydý temizlendi",
                removedCount
            });
        }
    }
}