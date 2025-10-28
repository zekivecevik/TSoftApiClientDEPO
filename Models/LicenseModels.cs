namespace TSoftApiClient.Models
{
    /// <summary>
    /// Lisans modeli
    /// </summary>
    public class License
    {
        public int Id { get; set; }
        public string LicenseKey { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string ContactEmail { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; } = true;
        public LicenseType Type { get; set; } = LicenseType.Monthly;
        public int MaxUsers { get; set; } = 5;
        public string? Notes { get; set; }

        // Ek özellikler
        public string? MachineId { get; set; }
        public DateTime? LastChecked { get; set; }
        public string? Features { get; set; } // JSON string: hangi özellikler aktif
    }

    /// <summary>
    /// Lisans türleri
    /// </summary>
    public enum LicenseType
    {
        Trial = 0,      // 7 gün deneme
        Monthly = 1,    // 1 ay
        Quarterly = 2,  // 3 ay
        Yearly = 3,     // 1 yýl
        Lifetime = 4    // Sýnýrsýz
    }

    /// <summary>
    /// Lisans doðrulama sonucu
    /// </summary>
    public class LicenseValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = "";
        public License? License { get; set; }
        public int DaysRemaining { get; set; }
        public bool IsExpired { get; set; }
        public bool IsExpiringSoon { get; set; } // 7 gün kala uyarý
    }

    /// <summary>
    /// Lisans aktivasyon isteði
    /// </summary>
    public class ActivateLicenseRequest
    {
        public required string LicenseKey { get; set; }
        public string? CompanyName { get; set; }
        public string? ContactEmail { get; set; }
        public string? MachineId { get; set; }
    }

    /// <summary>
    /// Lisans oluþturma isteði (Admin için)
    /// </summary>
    public class CreateLicenseRequest
    {
        public required string CompanyName { get; set; }
        public required string ContactEmail { get; set; }
        public LicenseType Type { get; set; } = LicenseType.Monthly;
        public int MaxUsers { get; set; } = 5;
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Lisans istatistikleri
    /// </summary>
    public class LicenseStatistics
    {
        public int TotalLicenses { get; set; }
        public int ActiveLicenses { get; set; }
        public int ExpiredLicenses { get; set; }
        public int ExpiringSoonLicenses { get; set; }
        public Dictionary<LicenseType, int> LicensesByType { get; set; } = new();
    }
}