using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSoftApiClient.Models;
using TSoftApiClient.Services;

namespace TSoftApiClient.Controllers
{
    /// <summary>
    /// Lisans Yönetimi MVC Controller
    /// </summary>
    public class LicenseMvcController : Controller
    {
        private readonly LicenseService _licenseService;
        private readonly ILogger<LicenseMvcController> _logger;

        public LicenseMvcController(
            LicenseService licenseService,
            ILogger<LicenseMvcController> logger)
        {
            _licenseService = licenseService;
            _logger = logger;
        }

        /// <summary>
        /// Lisans sayfası - Admin için yönetim paneli, User için aktivasyon
        /// </summary>
        [Route("/License")]
        [AllowAnonymous]
        public IActionResult Index(string? message)
        {
            var license = _licenseService.GetActiveLicense();

            if (license != null)
            {
                var validation = _licenseService.ValidateLicense(license.LicenseKey);
                ViewBag.CurrentLicense = license;
                ViewBag.Validation = validation;
            }

            ViewBag.Message = message;
            ViewBag.MachineId = LicenseService.GenerateMachineId();

            // Admin ise tüm lisansları ve istatistikleri ekle
            if (User.IsInRole("Admin"))
            {
                ViewBag.AllLicenses = _licenseService.GetAllLicenses();
                ViewBag.Statistics = _licenseService.GetStatistics();
                ViewBag.ExpiringSoon = _licenseService.GetExpiringSoonLicenses();
            }

            return View("~/Views/License/Index.cshtml");
        }

        /// <summary>
        /// Lisans aktivasyon POST
        /// </summary>
        [HttpPost]
        [Route("/License/Activate")]
        [AllowAnonymous]
        public IActionResult Activate([FromForm] ActivateLicenseRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.MachineId))
                {
                    request.MachineId = LicenseService.GenerateMachineId();
                }

                var (success, message, license) = _licenseService.ActivateLicense(request);

                if (success)
                {
                    TempData["Success"] = message;
                    _logger.LogInformation("✅ License activated via web: {Key}", request.LicenseKey);
                    return RedirectToAction("Index", "Home");
                }

                TempData["Error"] = message;
                return RedirectToAction("Index", new { message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 License activation error");
                TempData["Error"] = "Lisans aktive edilirken hata oluştu";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Yeni lisans oluşturma sayfası (Admin only)
        /// </summary>
        [Route("/License/Create")]
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View("~/Views/License/Create.cshtml");
        }

        /// <summary>
        /// Yeni lisans oluştur POST (Admin only)
        /// </summary>
        [Route("/License/Create")]
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Create([FromForm] CreateLicenseRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View("~/Views/License/Create.cshtml", request);
                }

                var (success, message, license) = _licenseService.CreateLicense(request);

                if (success)
                {
                    TempData["Success"] = $"Lisans oluşturuldu: {license!.LicenseKey}";
                    TempData["LicenseKey"] = license.LicenseKey;
                    return RedirectToAction("Index");
                }

                ViewBag.Error = message;
                return View("~/Views/License/Create.cshtml", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 License creation error");
                ViewBag.Error = "Lisans oluşturulurken hata oluştu";
                return View("~/Views/License/Create.cshtml", request);
            }
        }

        /// <summary>
        /// Lisans süresini uzat (Admin only)
        /// </summary>
        [HttpPost]
        [Route("/License/Extend")]
        [Authorize(Roles = "Admin")]
        public IActionResult Extend([FromForm] string licenseKey, [FromForm] int days)
        {
            var (success, message) = _licenseService.ExtendLicense(licenseKey, days);

            if (success)
            {
                TempData["Success"] = message;
            }
            else
            {
                TempData["Error"] = message;
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Lisansı iptal et (Admin only)
        /// </summary>
        [HttpPost]
        [Route("/License/Revoke")]
        [Authorize(Roles = "Admin")]
        public IActionResult Revoke([FromForm] string licenseKey)
        {
            var success = _licenseService.RevokeLicense(licenseKey);

            if (success)
            {
                TempData["Success"] = "Lisans iptal edildi";
            }
            else
            {
                TempData["Error"] = "Lisans bulunamadı";
            }

            return RedirectToAction("Index");
        }
    }
}
