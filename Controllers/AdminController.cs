using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSoftApiClient.Services;

namespace TSoftApiClient.Controllers
{
    /// <summary>
    /// Admin Panel Ana Controller
    /// </summary>
    [Authorize(Roles = "Admin")]
    [Route("Admin")]
    public class AdminController : Controller
    {
        private readonly AuthService _authService;
        private readonly AuditLogService _auditLogService;
        private readonly TSoftApiService _tsoftService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            AuthService authService,
            AuditLogService auditLogService,
            TSoftApiService tsoftService,
            ILogger<AdminController> logger)
        {
            _authService = authService;
            _auditLogService = auditLogService;
            _tsoftService = tsoftService;
            _logger = logger;
        }

        /// <summary>
        /// Admin Dashboard
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var users = _authService.GetAllUsers();
                var logs = _auditLogService.GetLogs(page: 1, pageSize: 20);
                var stats = _auditLogService.GetStatistics();

                ViewBag.Users = users;
                ViewBag.Logs = logs;
                ViewBag.Stats = stats;
                ViewBag.ActiveUsers = users.Count; // Bu örnekte hepsi aktif

                // Kullanýcý aktivite istatistikleri
                var userActivity = logs
                    .GroupBy(l => l.Username)
                    .Select(g => new
                    {
                        Username = g.Key,
                        ActionCount = g.Count(),
                        LastActivity = g.Max(l => l.Timestamp)
                    })
                    .OrderByDescending(x => x.ActionCount)
                    .Take(5)
                    .ToList();

                ViewBag.UserActivity = userActivity;

                // T-Soft API istatistikleri
                try
                {
                    var productsTask = _tsoftService.GetProductsAsync(limit: 1);
                    var ordersTask = _tsoftService.GetOrdersAsync(limit: 1);
                    var customersTask = _tsoftService.GetCustomersAsync(limit: 1);

                    await Task.WhenAll(productsTask, ordersTask, customersTask);

                    ViewBag.TotalProducts = productsTask.Result.Success ? (productsTask.Result.Data?.Count ?? 0) : 0;
                    ViewBag.TotalOrders = ordersTask.Result.Success ? (ordersTask.Result.Data?.Count ?? 0) : 0;
                    ViewBag.TotalCustomers = customersTask.Result.Success ? (customersTask.Result.Data?.Count ?? 0) : 0;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch API statistics");
                    ViewBag.TotalProducts = 0;
                    ViewBag.TotalOrders = 0;
                    ViewBag.TotalCustomers = 0;
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                ViewBag.Error = "Dashboard yüklenirken hata oluþtu";
                return View();
            }
        }
    }
}