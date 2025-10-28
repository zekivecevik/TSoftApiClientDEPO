using TSoftApiClient.Services;

namespace TSoftApiClient.Middleware
{
    /// <summary>
    /// Lisans kontrol middleware - Her istekte lisansı kontrol eder
    /// </summary>
    public class LicenseValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LicenseValidationMiddleware> _logger;

        // Lisans kontrolü yapılmayacak path'ler
        private static readonly string[] ExcludedPaths = new[]
        {
            "/account/login",
            "/login",
            "/api/license/validate",
            "/api/license/activate",
            "/api/license/machine-id",
            "/license",
            "/css",
            "/js",
            "/lib",
            "/favicon.ico"
        };

        public LicenseValidationMiddleware(
            RequestDelegate next,
            ILogger<LicenseValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, LicenseService licenseService)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // Excluded path kontrolü
            if (ExcludedPaths.Any(excluded => path.StartsWith(excluded)))
            {
                await _next(context);
                return;
            }

            try
            {
                // Aktif lisansı getir
                var license = licenseService.GetActiveLicense();

                if (license == null)
                {
                    _logger.LogWarning("⚠️ No active license found");
                    await RedirectToLicensePage(context, "Aktif lisans bulunamadı. Lütfen lisans aktive edin.");
                    return;
                }

                // Lisansı doğrula
                var validation = licenseService.ValidateLicense(license.LicenseKey);

                if (!validation.IsValid)
                {
                    _logger.LogWarning("⚠️ License validation failed: {Message}", validation.Message);
                    await RedirectToLicensePage(context, validation.Message);
                    return;
                }

                // Süresi dolmak üzere ise uyarı ekle
                if (validation.IsExpiringSoon)
                {
                    context.Items["LicenseWarning"] = $"Lisansınızın süresi {validation.DaysRemaining} gün içinde dolacak!";
                }

                // Lisans bilgilerini context'e ekle
                context.Items["License"] = license;
                context.Items["DaysRemaining"] = validation.DaysRemaining;

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 License validation middleware error");
                await RedirectToLicensePage(context, "Lisans doğrulama hatası oluştu.");
            }
        }

        private async Task RedirectToLicensePage(HttpContext context, string message)
        {
            // API isteğiyse JSON döndür
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                {
                    success = false,
                    message = message,
                    licenseExpired = true
                }));
                return;
            }

            // Web isteğiyse lisans sayfasına yönlendir
            context.Response.Redirect($"/License?message={Uri.EscapeDataString(message)}");
        }
    }

    /// <summary>
    /// Middleware extension
    /// </summary>
    public static class LicenseValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseLicenseValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LicenseValidationMiddleware>();
        }
    }
}