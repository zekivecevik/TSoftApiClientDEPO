using System.Security.Cryptography;
using System.Text;
using TSoftApiClient.Models;

namespace TSoftApiClient.Services
{
    /// <summary>
    /// Lisans Yönetim Servisi
    /// </summary>
    public class LicenseService
    {
        private readonly ILogger<LicenseService> _logger;
        private static List<License> _licenses = new();
        private static int _nextId = 1;
        private const string LICENSE_FILE_PATH = "license.dat";

        public LicenseService(ILogger<LicenseService> logger)
        {
            _logger = logger;
            InitializeDefaultLicense();
        }

        /// <summary>
        /// Varsayılan deneme lisansı oluştur
        /// </summary>
        private void InitializeDefaultLicense()
        {
            if (_licenses.Count == 0)
            {
                var defaultLicense = new License
                {
                    Id = _nextId++,
                    LicenseKey = GenerateLicenseKey("TRIAL"),
                    CompanyName = "Trial User",
                    ContactEmail = "trial@example.com",
                    Type = LicenseType.Trial,
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    IsActive = true,
                    MaxUsers = 3
                };

                _licenses.Add(defaultLicense);
                _logger.LogInformation("🔑 Default trial license created: {Key} (Expires: {Date})",
                    defaultLicense.LicenseKey,
                    defaultLicense.ExpiresAt.ToString("dd/MM/yyyy"));
            }
        }

        /// <summary>
        /// Lisans key'i oluştur
        /// </summary>
        private string GenerateLicenseKey(string prefix = "TSOFT")
        {
            var guid = Guid.NewGuid().ToString("N").ToUpper();
            var segments = new[]
            {
                guid.Substring(0, 4),
                guid.Substring(4, 4),
                guid.Substring(8, 4),
                guid.Substring(12, 4)
            };

            return $"{prefix}-{string.Join("-", segments)}";
        }

        /// <summary>
        /// Lisans doğrula
        /// </summary>
        public LicenseValidationResult ValidateLicense(string licenseKey)
        {
            try
            {
                var license = _licenses.FirstOrDefault(l => l.LicenseKey == licenseKey);

                if (license == null)
                {
                    return new LicenseValidationResult
                    {
                        IsValid = false,
                        Message = "Geçersiz lisans key'i",
                        IsExpired = true
                    };
                }

                if (!license.IsActive)
                {
                    return new LicenseValidationResult
                    {
                        IsValid = false,
                        Message = "Lisans devre dışı bırakılmış",
                        License = license,
                        IsExpired = true
                    };
                }

                var now = DateTime.UtcNow;
                var isExpired = license.ExpiresAt <= now;
                var daysRemaining = (int)(license.ExpiresAt - now).TotalDays;
                var isExpiringSoon = daysRemaining <= 7 && daysRemaining > 0;

                license.LastChecked = now;

                if (isExpired)
                {
                    _logger.LogWarning("⚠️ License expired: {Key} (Expired on: {Date})",
                        licenseKey,
                        license.ExpiresAt.ToString("dd/MM/yyyy"));

                    return new LicenseValidationResult
                    {
                        IsValid = false,
                        Message = $"Lisans süresi dolmuş (Bitiş: {license.ExpiresAt:dd/MM/yyyy})",
                        License = license,
                        IsExpired = true,
                        DaysRemaining = 0
                    };
                }

                _logger.LogDebug("✅ License valid: {Key} ({Days} days remaining)",
                    licenseKey,
                    daysRemaining);

                return new LicenseValidationResult
                {
                    IsValid = true,
                    Message = $"Lisans geçerli ({daysRemaining} gün kaldı)",
                    License = license,
                    IsExpired = false,
                    DaysRemaining = daysRemaining,
                    IsExpiringSoon = isExpiringSoon
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 License validation error");
                return new LicenseValidationResult
                {
                    IsValid = false,
                    Message = "Lisans doğrulama hatası",
                    IsExpired = true
                };
            }
        }

        /// <summary>
        /// Aktif lisansı getir
        /// </summary>
        public License? GetActiveLicense()
        {
            return _licenses
                .Where(l => l.IsActive && l.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(l => l.ExpiresAt)
                .FirstOrDefault();
        }

        /// <summary>
        /// Yeni lisans oluştur (Admin)
        /// </summary>
        public (bool success, string message, License? license) CreateLicense(CreateLicenseRequest request)
        {
            try
            {
                var duration = request.Type switch
                {
                    LicenseType.Trial => TimeSpan.FromDays(7),
                    LicenseType.Monthly => TimeSpan.FromDays(30),
                    LicenseType.Quarterly => TimeSpan.FromDays(90),
                    LicenseType.Yearly => TimeSpan.FromDays(365),
                    LicenseType.Lifetime => TimeSpan.FromDays(36500), // 100 yıl
                    _ => TimeSpan.FromDays(30)
                };

                var license = new License
                {
                    Id = _nextId++,
                    LicenseKey = GenerateLicenseKey("TSOFT"),
                    CompanyName = request.CompanyName,
                    ContactEmail = request.ContactEmail,
                    Type = request.Type,
                    ExpiresAt = DateTime.UtcNow.Add(duration),
                    MaxUsers = request.MaxUsers,
                    Notes = request.Notes,
                    IsActive = true
                };

                _licenses.Add(license);

                _logger.LogInformation("✅ License created: {Key} for {Company} (Type: {Type}, Expires: {Date})",
                    license.LicenseKey,
                    license.CompanyName,
                    license.Type,
                    license.ExpiresAt.ToString("dd/MM/yyyy"));

                return (true, "Lisans başarıyla oluşturuldu", license);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error creating license");
                return (false, "Lisans oluşturulurken hata oluştu", null);
            }
        }

        /// <summary>
        /// Lisans aktive et
        /// </summary>
        public (bool success, string message, License? license) ActivateLicense(ActivateLicenseRequest request)
        {
            try
            {
                var license = _licenses.FirstOrDefault(l => l.LicenseKey == request.LicenseKey);

                if (license == null)
                {
                    return (false, "Geçersiz lisans key'i", null);
                }

                if (license.ExpiresAt <= DateTime.UtcNow)
                {
                    return (false, "Lisans süresi dolmuş", null);
                }

                // Machine ID kontrolü (opsiyonel)
                if (!string.IsNullOrEmpty(license.MachineId) &&
                    license.MachineId != request.MachineId)
                {
                    return (false, "Bu lisans başka bir makineye kayıtlı", null);
                }

                // İlk aktivasyon
                if (string.IsNullOrEmpty(license.MachineId))
                {
                    license.MachineId = request.MachineId;
                    license.CompanyName = request.CompanyName ?? license.CompanyName;
                    license.ContactEmail = request.ContactEmail ?? license.ContactEmail;
                }

                license.LastChecked = DateTime.UtcNow;
                license.IsActive = true;

                _logger.LogInformation("✅ License activated: {Key} for {Company}",
                    license.LicenseKey,
                    license.CompanyName);

                return (true, "Lisans başarıyla aktive edildi", license);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error activating license");
                return (false, "Lisans aktive edilirken hata oluştu", null);
            }
        }

        /// <summary>
        /// Lisans süresini uzat
        /// </summary>
        public (bool success, string message) ExtendLicense(string licenseKey, int days)
        {
            try
            {
                var license = _licenses.FirstOrDefault(l => l.LicenseKey == licenseKey);

                if (license == null)
                {
                    return (false, "Lisans bulunamadı");
                }

                var oldExpiry = license.ExpiresAt;
                license.ExpiresAt = license.ExpiresAt.AddDays(days);

                _logger.LogInformation("✅ License extended: {Key} ({OldDate} → {NewDate})",
                    licenseKey,
                    oldExpiry.ToString("dd/MM/yyyy"),
                    license.ExpiresAt.ToString("dd/MM/yyyy"));

                return (true, $"Lisans süresi {days} gün uzatıldı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error extending license");
                return (false, "Lisans uzatılırken hata oluştu");
            }
        }

        /// <summary>
        /// Lisansı iptal et
        /// </summary>
        public bool RevokeLicense(string licenseKey)
        {
            var license = _licenses.FirstOrDefault(l => l.LicenseKey == licenseKey);
            if (license == null) return false;

            license.IsActive = false;
            _logger.LogWarning("🚫 License revoked: {Key}", licenseKey);
            return true;
        }

        /// <summary>
        /// Tüm lisansları getir
        /// </summary>
        public List<License> GetAllLicenses()
        {
            return _licenses.OrderByDescending(l => l.CreatedAt).ToList();
        }

        /// <summary>
        /// Lisans istatistikleri
        /// </summary>
        public LicenseStatistics GetStatistics()
        {
            var now = DateTime.UtcNow;

            return new LicenseStatistics
            {
                TotalLicenses = _licenses.Count,
                ActiveLicenses = _licenses.Count(l => l.IsActive && l.ExpiresAt > now),
                ExpiredLicenses = _licenses.Count(l => l.ExpiresAt <= now),
                ExpiringSoonLicenses = _licenses.Count(l => l.IsActive &&
                    l.ExpiresAt > now &&
                    (l.ExpiresAt - now).TotalDays <= 7),
                LicensesByType = _licenses
                    .GroupBy(l => l.Type)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        /// <summary>
        /// Süresi dolmak üzere olan lisansları getir
        /// </summary>
        public List<License> GetExpiringSoonLicenses(int daysThreshold = 7)
        {
            var now = DateTime.UtcNow;
            var threshold = now.AddDays(daysThreshold);

            return _licenses
                .Where(l => l.IsActive && l.ExpiresAt > now && l.ExpiresAt <= threshold)
                .OrderBy(l => l.ExpiresAt)
                .ToList();
        }

        /// <summary>
        /// Makine ID'sini oluştur (benzersiz)
        /// </summary>
        public static string GenerateMachineId()
        {
            try
            {
                var computerName = Environment.MachineName;
                var userName = Environment.UserName;
                var osVersion = Environment.OSVersion.ToString();

                var combined = $"{computerName}-{userName}-{osVersion}";
                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                return Convert.ToBase64String(hash).Substring(0, 16);
            }
            catch
            {
                return Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper();
            }
        }
    }
}