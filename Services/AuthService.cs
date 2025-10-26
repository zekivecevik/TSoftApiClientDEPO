using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TSoftApiClient.Models;

namespace TSoftApiClient.Services
{
    /// <summary>
    /// Authentication Service - JWT Token, User Management, Security
    /// </summary>
    public class AuthService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly int _jwtExpiryMinutes;

        // In-Memory User Store (Production'da Database kullanılacak)
        private static List<User> _users = new List<User>
        {
            new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@tsoft.com",
                FullName = "System Administrator",
                Role = Roles.Admin,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                ApiKey = GenerateApiKey(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = 2,
                Username = "manager",
                Email = "manager@tsoft.com",
                FullName = "Store Manager",
                Role = Roles.Manager,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("manager123"),
                ApiKey = GenerateApiKey(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = 3,
                Username = "viewer",
                Email = "viewer@tsoft.com",
                FullName = "Data Viewer",
                Role = Roles.Viewer,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("viewer123"),
                ApiKey = GenerateApiKey(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        public AuthService(IConfiguration config, ILogger<AuthService> logger)
        {
            _config = config;
            _logger = logger;
            _jwtSecret = config["Jwt:Secret"] ?? "YOUR_SUPER_SECRET_KEY_MINIMUM_32_CHARACTERS_LONG_FOR_SECURITY";
            _jwtIssuer = config["Jwt:Issuer"] ?? "TSoftERP";
            _jwtAudience = config["Jwt:Audience"] ?? "TSoftERP-Users";
            _jwtExpiryMinutes = int.Parse(config["Jwt:ExpiryMinutes"] ?? "480"); // 8 hours default
        }

        /// <summary>
        /// Kullanıcı girişi
        /// </summary>
        public LoginResponse Login(LoginRequest request, string ipAddress, string userAgent)
        {
            try
            {
                _logger.LogInformation("🔑 Login attempt for user: {Username}", request.Username);

                var user = _users.FirstOrDefault(u =>
                    u.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase) &&
                    u.IsActive);

                if (user == null)
                {
                    _logger.LogWarning("❌ User not found or inactive: {Username}", request.Username);
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Kullanıcı adı veya şifre hatalı"
                    };
                }

                // Şifre kontrolü
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("❌ Invalid password for user: {Username}", request.Username);
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Kullanıcı adı veya şifre hatalı"
                    };
                }

                // JWT Token oluştur
                var token = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                // Refresh token kaydet
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                user.LastLoginAt = DateTime.UtcNow;

                _logger.LogInformation("✅ Login successful for user: {Username} (Role: {Role})", user.Username, user.Role);

                return new LoginResponse
                {
                    Success = true,
                    Message = "Giriş başarılı",
                    Token = token,
                    RefreshToken = refreshToken,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        FullName = user.FullName,
                        Role = user.Role,
                        ApiKey = user.ApiKey
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Login error for user: {Username}", request.Username);
                return new LoginResponse
                {
                    Success = false,
                    Message = "Giriş sırasında bir hata oluştu"
                };
            }
        }

        /// <summary>
        /// JWT Token oluştur
        /// </summary>
        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("FullName", user.FullName),
                new Claim("ApiKey", user.ApiKey ?? "")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtExpiryMinutes),
                Issuer = _jwtIssuer,
                Audience = _jwtAudience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Refresh Token oluştur
        /// </summary>
        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        /// <summary>
        /// Token'dan kullanıcı bilgisi al
        /// </summary>
        public User? GetUserFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtSecret);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = int.Parse(jwtToken.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);

                return _users.FirstOrDefault(u => u.Id == userId);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// API Key ile kullanıcı bul
        /// </summary>
        public User? GetUserByApiKey(string apiKey)
        {
            return _users.FirstOrDefault(u => u.ApiKey == apiKey && u.IsActive);
        }

        /// <summary>
        /// Yeni kullanıcı oluştur
        /// </summary>
        public (bool success, string message, User? user) CreateUser(string username, string email, string password, string fullName, string role)
        {
            try
            {
                if (_users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                {
                    return (false, "Bu kullanıcı adı zaten kullanılıyor", null);
                }

                if (_users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                {
                    return (false, "Bu e-posta adresi zaten kullanılıyor", null);
                }

                if (!Roles.All.Contains(role))
                {
                    return (false, "Geçersiz rol", null);
                }

                var user = new User
                {
                    Id = _users.Any() ? _users.Max(u => u.Id) + 1 : 1,
                    Username = username,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    FullName = fullName,
                    Role = role,
                    ApiKey = GenerateApiKey(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _users.Add(user);

                _logger.LogInformation("✅ User created: {Username} (Role: {Role})", username, role);

                return (true, "Kullanıcı başarıyla oluşturuldu", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error creating user: {Username}", username);
                return (false, "Kullanıcı oluşturulurken hata oluştu", null);
            }
        }

        /// <summary>
        /// Tüm kullanıcıları listele
        /// </summary>
        public List<UserInfo> GetAllUsers()
        {
            return _users.Select(u => new UserInfo
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.Role,
                ApiKey = u.ApiKey
            }).ToList();
        }

        /// <summary>
        /// API Key oluştur
        /// </summary>
        private static string GenerateApiKey()
        {
            return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Yeni API Key oluştur (mevcut kullanıcı için)
        /// </summary>
        public string RegenerateApiKey(int userId)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user == null) throw new Exception("Kullanıcı bulunamadı");

            user.ApiKey = GenerateApiKey();
            _logger.LogInformation("🔑 API Key regenerated for user: {Username}", user.Username);

            return user.ApiKey;
        }

        /// <summary>
        /// Kullanıcı silme
        /// </summary>
        public bool DeleteUser(int userId)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return false;

            _users.Remove(user);
            _logger.LogInformation("🗑️ User deleted: {Username}", user.Username);
            return true;
        }

        /// <summary>
        /// Kullanıcı aktif/pasif durumu değiştir
        /// </summary>
        public bool ToggleUserStatus(int userId)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return false;

            user.IsActive = !user.IsActive;
            _logger.LogInformation("🔄 User status toggled: {Username} - Active: {Active}", user.Username, user.IsActive);
            return true;
        }
    }
}