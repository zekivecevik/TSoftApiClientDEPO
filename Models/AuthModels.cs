namespace TSoftApiClient.Models
{
    /// <summary>
    /// Kullanıcı modeli - Authentication için
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "Viewer"; // Admin, Manager, Viewer
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public string? ApiKey { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }

    /// <summary>
    /// Giriş isteği
    /// </summary>
    public class LoginRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    /// <summary>
    /// Giriş yanıtı
    /// </summary>
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public UserInfo? User { get; set; }
    }

    /// <summary>
    /// Kullanıcı bilgisi (şifre olmadan)
    /// </summary>
    public class UserInfo
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "";
        public string? ApiKey { get; set; }
    }

    /// <summary>
    /// Audit Log modeli - Tüm işlemleri kaydet
    /// </summary>
    public class AuditLog
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string Username { get; set; } = "";
        public string Action { get; set; } = ""; // Login, Logout, Create, Update, Delete, View
        public string Entity { get; set; } = ""; // Product, Order, Customer, Category
        public string? EntityId { get; set; }
        public string? Details { get; set; }
        public string IpAddress { get; set; } = "";
        public string UserAgent { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool Success { get; set; } = true;
    }

    /// <summary>
    /// Rol tanımları
    /// </summary>
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string Viewer = "Viewer";

        public static readonly string[] All = { Admin, Manager, Viewer };
    }

    /// <summary>
    /// API Key modeli
    /// </summary>
    public class ApiKeyInfo
    {
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public string Role { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}