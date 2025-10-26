using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSoftApiClient.Models;
using TSoftApiClient.Services;

namespace TSoftApiClient.Controllers
{
    /// <summary>
    /// Admin Kullanýcý Yönetimi Controller
    /// </summary>
    [Authorize(Roles = "Admin")]
    [Route("Admin/Users")]
    public class AdminUsersController : Controller
    {
        private readonly AuthService _authService;
        private readonly AuditLogService _auditLogService;
        private readonly ILogger<AdminUsersController> _logger;

        public AdminUsersController(
            AuthService authService,
            AuditLogService auditLogService,
            ILogger<AdminUsersController> logger)
        {
            _authService = authService;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        /// <summary>
        /// Kullanýcý listesi sayfasý
        /// </summary>
        [HttpGet("")]
        public IActionResult Index()
        {
            try
            {
                var users = _authService.GetAllUsers();
                var currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;

                ViewBag.CurrentUserId = currentUserId;
                ViewBag.TotalUsers = users.Count;
                ViewBag.AdminCount = users.Count(u => u.Role == Roles.Admin);
                ViewBag.ManagerCount = users.Count(u => u.Role == Roles.Manager);
                ViewBag.ViewerCount = users.Count(u => u.Role == Roles.Viewer);

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users");
                TempData["Error"] = "Kullanýcýlar yüklenirken hata oluþtu";
                return RedirectToAction("Index", "Admin");
            }
        }

        /// <summary>
        /// Yeni kullanýcý ekleme sayfasý
        /// </summary>
        [HttpGet("Create")]
        public IActionResult Create()
        {
            ViewBag.Roles = Roles.All;
            return View();
        }

        /// <summary>
        /// Yeni kullanýcý ekleme iþlemi
        /// </summary>
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string username, string email, string password, string fullName, string role)
        {
            try
            {
                var currentUsername = User?.Identity?.Name ?? "Unknown";
                var currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;

                var (success, message, user) = _authService.CreateUser(username, email, password, fullName, role);

                if (success && user != null)
                {
                    _auditLogService.LogCreate(
                        currentUserId,
                        currentUsername,
                        "User",
                        user.Id.ToString(),
                        $"Created user: {username} ({role})",
                        HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
                    );

                    TempData["Success"] = "Kullanýcý baþarýyla oluþturuldu!";
                    return RedirectToAction("Index");
                }

                ViewBag.Error = message;
                ViewBag.Roles = Roles.All;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                ViewBag.Error = "Kullanýcý oluþturulurken hata oluþtu";
                ViewBag.Roles = Roles.All;
                return View();
            }
        }

        /// <summary>
        /// Kullanýcý silme iþlemi
        /// </summary>
        [HttpPost("Delete/{userId}")]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int userId)
        {
            try
            {
                var currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var currentUsername = User?.Identity?.Name ?? "Unknown";

                if (currentUserId == userId)
                {
                    TempData["Error"] = "Kendi hesabýnýzý silemezsiniz!";
                    return RedirectToAction("Index");
                }

                var users = _authService.GetAllUsers();
                var userToDelete = users.FirstOrDefault(u => u.Id == userId);

                if (userToDelete == null)
                {
                    TempData["Error"] = "Kullanýcý bulunamadý";
                    return RedirectToAction("Index");
                }

                var success = _authService.DeleteUser(userId);

                if (success)
                {
                    _auditLogService.LogDelete(
                        currentUserId,
                        currentUsername,
                        "User",
                        userId.ToString(),
                        $"Deleted user: {userToDelete.Username}",
                        HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
                    );

                    TempData["Success"] = "Kullanýcý baþarýyla silindi!";
                }
                else
                {
                    TempData["Error"] = "Kullanýcý silinemedi";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                TempData["Error"] = "Kullanýcý silinirken hata oluþtu";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Kullanýcý aktif/pasif durumu deðiþtirme
        /// </summary>
        [HttpPost("ToggleStatus/{userId}")]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleStatus(int userId)
        {
            try
            {
                var currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var currentUsername = User?.Identity?.Name ?? "Unknown";

                if (currentUserId == userId)
                {
                    TempData["Error"] = "Kendi hesabýnýzýn durumunu deðiþtiremezsiniz!";
                    return RedirectToAction("Index");
                }

                var success = _authService.ToggleUserStatus(userId);

                if (success)
                {
                    _auditLogService.LogUpdate(
                        currentUserId,
                        currentUsername,
                        "User",
                        userId.ToString(),
                        "Toggled user status",
                        HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
                    );

                    TempData["Success"] = "Kullanýcý durumu deðiþtirildi!";
                }
                else
                {
                    TempData["Error"] = "Ýþlem baþarýsýz";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status");
                TempData["Error"] = "Durum deðiþtirilirken hata oluþtu";
                return RedirectToAction("Index");
            }
        }
    }
}