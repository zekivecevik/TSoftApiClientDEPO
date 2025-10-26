using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSoftApiClient.Models;
using TSoftApiClient.Services;

namespace TSoftApiClient.Controllers
{
    /// <summary>
    /// Account MVC Controller - Login, Logout, Profile Pages
    /// </summary>
    public class AccountController : Controller
    {
        private readonly AuthService _authService;
        private readonly AuditLogService _auditLogService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            AuthService authService,
            AuditLogService auditLogService,
            ILogger<AccountController> logger)
        {
            _authService = authService;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        /// <summary>
        /// Login Page
        /// </summary>
        [HttpGet]
        [Route("/Account/Login")]
        [Route("/Login")]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            // Zaten giriş yapmışsa ana sayfaya yönlendir
            if (User?.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        /// <summary>
        /// Login POST
        /// </summary>
        [HttpPost]
        [Route("/Account/Login")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult Login([FromForm] LoginRequest request, string? returnUrl = null)
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var userAgent = Request.Headers["User-Agent"].ToString();

                var result = _authService.Login(request, ipAddress, userAgent);

                if (result.Success && result.Token != null)
                {
                    // Cookie'ye token ekle
                    Response.Cookies.Append("AuthToken", result.Token, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = false, // Development için false, production'da true
                        SameSite = SameSiteMode.Lax,
                        Expires = DateTimeOffset.UtcNow.AddHours(8)
                    });

                    // Session'a kullanıcı bilgilerini ekle
                    HttpContext.Session.SetString("Username", result.User?.Username ?? "");
                    HttpContext.Session.SetString("Role", result.User?.Role ?? "");
                    HttpContext.Session.SetInt32("UserId", result.User?.Id ?? 0);

                    _auditLogService.LogLogin(request.Username, true, ipAddress, userAgent);

                    _logger.LogInformation("✅ User logged in: {Username}", request.Username);

                    // Return URL varsa oraya, yoksa ana sayfaya
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToAction("Index", "Home");
                }

                _auditLogService.LogLogin(request.Username, false, ipAddress, userAgent);
                ViewBag.Error = result.Message;
                return View(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Login error");
                ViewBag.Error = "Giriş sırasında bir hata oluştu";
                return View(request);
            }
        }

        /// <summary>
        /// Logout
        /// </summary>
        [HttpGet]
        [Route("/Account/Logout")]
        [Route("/Logout")]
        public IActionResult Logout()
        {
            try
            {
                var username = HttpContext.Session.GetString("Username") ?? User?.Identity?.Name ?? "Unknown";
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                // Cookie'yi sil
                Response.Cookies.Delete("AuthToken");

                // Session'ı temizle
                HttpContext.Session.Clear();

                // Audit log
                if (userId > 0)
                {
                    _auditLogService.LogLogout(userId, username, ipAddress);
                }

                _logger.LogInformation("✅ User logged out: {Username}", username);

                TempData["Info"] = "Başarıyla çıkış yaptınız";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Logout error");
                return RedirectToAction("Login");
            }
        }

        /// <summary>
        /// Profile Page
        /// </summary>
        [HttpGet]
        [Route("/Account/Profile")]
        [Authorize]
        public IActionResult Profile()
        {
            try
            {
                var username = User?.Identity?.Name ?? HttpContext.Session.GetString("Username");
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;

                if (string.IsNullOrEmpty(username) || userId == 0)
                {
                    return RedirectToAction("Login");
                }

                var users = _authService.GetAllUsers();
                var user = users.FirstOrDefault(u => u.Username == username);

                if (user == null)
                {
                    return RedirectToAction("Login");
                }

                ViewBag.User = user;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Profile page error");
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Access Denied Page
        /// </summary>
        [HttpGet]
        [Route("/Account/AccessDenied")]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}