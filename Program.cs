using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TSoftApiClient.Services;
using TSoftApiClient.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(); // MVC desteği
builder.Services.AddControllers();

// ============================================================================
// CUSTOM SERVICES
// ============================================================================
builder.Services.AddSingleton<WarehouseService>();
builder.Services.AddSingleton<LicenseService>();      // ✅ LİSANS SERVİSİ

// ============================================================================
// SESSION SUPPORT (for web authentication state)
// ============================================================================
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ============================================================================
// JWT AUTHENTICATION
// ============================================================================
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "YOUR_SUPER_SECRET_KEY_MINIMUM_32_CHARACTERS_LONG_FOR_SECURITY";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TSoftERP";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TSoftERP-Users";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Development için, production'da true yapın
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Cookie'den token al (MVC için)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Önce Authorization header'a bak
            if (string.IsNullOrEmpty(context.Token))
            {
                // Cookie'den token al
                context.Token = context.Request.Cookies["AuthToken"];
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Swagger/OpenAPI yapılandırması
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "T-Soft API Client with Authentication & License System",
        Version = "v1",
        Description = "T-Soft REST1 API ile entegrasyon için ASP.NET Core API + JWT Authentication + License Management",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Geliştirici",
            Email = "info@example.com"
        }
    });

    // JWT Bearer Authorization için Swagger yapılandırması
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // XML yorumları ekle (opsiyonel)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// HttpClient yapılandırması
builder.Services.AddHttpClient<TSoftApiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ============================================================================
// SERVICE REGISTRATION
// ============================================================================
builder.Services.AddScoped<TSoftApiService>();
builder.Services.AddSingleton<AuthService>();        // Singleton - in-memory user store
builder.Services.AddSingleton<AuditLogService>();    // Singleton - in-memory audit logs

// CORS yapılandırması (gerekirse)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "T-Soft API Client v1");
        options.RoutePrefix = "api/docs"; // Swagger'ı /api/docs'da aç
    });
}

app.UseStaticFiles(); // CSS, JS dosyaları için

app.UseCors("AllowAll");

app.UseRouting();

// ============================================================================
// AUTHENTICATION & AUTHORIZATION MIDDLEWARE (Sıralama önemli!)
// ============================================================================
app.UseSession();
app.UseAuthentication();  // ⚠️ ÖNCE Authentication
app.UseAuthorization();   // ⚠️ SONRA Authorization

// ============================================================================
// ✅ LİSANS DOĞRULAMA MIDDLEWARE (Authorization'dan SONRA!)
// ============================================================================
app.UseLicenseValidation();

// MVC Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// API Controllers
app.MapControllers();

// Basit test endpoint'i
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    message = "T-Soft API Client is running with Authentication & License System!"
}))
.WithName("HealthCheck")
.AllowAnonymous();

// Default users info endpoint (for demo)
app.MapGet("/api/auth/demo-users", () => Results.Ok(new
{
    message = "Demo kullanıcıları",
    users = new[]
    {
        new { username = "admin", password = "admin123", role = "Admin", description = "Tam yetki - Tüm işlemler" },
        new { username = "manager", password = "manager123", role = "Manager", description = "Yönetici - Çoğu işlem" },
        new { username = "viewer", password = "viewer123", role = "Viewer", description = "Görüntüleme - Sadece okuma" }
    }
}))
.WithName("DemoUsers")
.AllowAnonymous();

app.Run();