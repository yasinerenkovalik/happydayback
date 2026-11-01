using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using System.Threading.RateLimiting;
using System.Globalization;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using HappyDay.Api.Auth;
using HappyDay.Api.Middleware;
using HappyDay.Api.Services;
using HappyDay.Api.Startup;
using HappyDay.Api.Services.Mail;
using HappyDay.Application;
using HappyDay.Application.Common.Exceptions;
using HappyDay.Application.Common.Security;
using HappyDay.Application.Interface.Repository;
using HappyDay.Application.Interface.Services;
using HappyDay.Application.Validations.Company;
using HappyDay.Persistance;
using HappyDay.Persistance.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "wwwroot"
});

// ---------------- Services ----------------

// API her zaman nokta ondalık ayracı kullanmalı. Aksi halde sunucu kültürü tr-TR
// olduğunda form'daki "40.98426" değeri 4098426 olarak ayrıştırılıyor.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture(CultureInfo.InvariantCulture.Name);
    options.AddSupportedCultures(CultureInfo.InvariantCulture.Name);
    options.AddSupportedUICultures(CultureInfo.InvariantCulture.Name);
});

builder.Services.AddControllers();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CompanyCreateValidator>();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var logEmailsOnly = builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(builder.Configuration["Smtp:Host"]);
if (logEmailsOnly)
{
    // Geliştirmede SMTP yoksa doğrulama/şifre sıfırlama e-postaları konsola yazılır
    builder.Services.AddScoped<HappyDay.Application.Common.Email.IEmailSender, LogEmailSender>();
}
else
{
    builder.Services.AddScoped<
        HappyDay.Application.Common.Email.IEmailSender,
        HappyDay.Api.Services.Mail.MailService
    >();
}
// Program.cs
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));


// DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString) || connectionString == "UseFromSecretsOrEnv")
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection tanimli degil. Gelistirmede appsettings.Development.json, sunucuda ConnectionStrings__DefaultConnection ortam degiskeni ile ayarlayin.");
builder.Services.AddDbContext<HappyDayContext>(options =>
    options.UseNpgsql(connectionString));

// Katman servisleri
builder.Services.AddPersistanceLayerServices();
builder.Services.AddAplicationLayerServices();

// İsteği yapan kullanıcı (firma sahiplik kontrolleri için)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Password hashing
var security = builder.Configuration.GetSection("Security");
var workFactor = security.GetValue<int?>("PasswordWorkFactor") ?? 12;
// Pepper repoda tutulmaz: geliştirmede user-secrets, sunucuda Security__Pepper ortam değişkeni.
// Mevcut şifreler bu değerle hash'lendiği için değeri değiştirmek tüm girişleri bozar.
var pepper = security.GetValue<string>("Pepper");
if (string.IsNullOrWhiteSpace(pepper))
    throw new InvalidOperationException(
        "Security:Pepper tanimli degil. User-secrets veya Security__Pepper ortam degiskeni ile ayarlayin.");

builder.Services.AddSingleton<IPasswordHasher>(
    _ => new BcryptPasswordHasher(workFactor, pepper)
);

// CORS — izinli origin'ler config'den gelir, varsayilan olarak localhost Vite
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000", "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        if (allowedOrigins.Length == 0 || allowedOrigins.Contains("*"))
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
        else
        {
            // Oturum cookie'leri başka origin'deki frontend'e de gitsin (VITE_API_URL ile ayrı alan adı)
            policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
        }
    });
});

// Yayında API, Coolify'ın Traefik'inin arkasında çalışır: istek Traefik'ten gelir. Ziyaretçinin IP'si (hız sınırı)
// ve https bilgisi (önizlemedeki görsel adresleri) X-Forwarded-For / X-Forwarded-Proto başlıklarındadır.
// Proxy'nin docker ağındaki adresi önceden bilinmediği için hepsine güvenilir; bu yüzden API'nin portu
// dışarı açılmamalı, yalnızca Traefik üzerinden erişilmelidir. Traefik, dışarıdan gelen X-Forwarded-* başlıklarını
// kendi değeriyle değiştirir; ForwardLimit 1 olduğu için yalnızca Traefik'in eklediği son değer okunur.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Rate limiting — login / mesaj gonderme gibi spam'e acik endpoint'ler icin.
// Entegrasyon testleri aynı istemciden çok sayıda giriş yaptığı için RateLimiting:Enabled=false ile kapatır.
var rateLimitingEnabled = builder.Configuration.GetValue("RateLimiting:Enabled", true);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    void PerIpPolicy(string name, int permitsPerMinute) =>
        options.AddPolicy(name, httpContext => rateLimitingEnabled
            ? RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitsPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                })
            : RateLimitPartition.GetNoLimiter("disabled"));

    PerIpPolicy("Auth", 10);
    // Token yenileme: sayfa açılışlarında ve 15 dakikada bir; giriş kadar sıkı sınırlanmaz
    PerIpPolicy("Refresh", 30);
    PerIpPolicy("PublicWrite", 5);
});

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("CanCompany", policy =>
        policy.RequireRole("Admin", "Company"));
});

// Auth (JWT) — secret repoda tutulmaz: geliştirmede user-secrets, sunucuda JwtSettings__Secret ortam değişkeni
var jwtSection = builder.Configuration.GetSection("JwtSettings");
var jwtSecret = jwtSection.GetValue<string>("Secret");
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
    throw new InvalidOperationException(
        "JwtSettings:Secret tanimli degil veya 32 karakterden kisa. User-secrets veya JwtSettings__Secret ortam degiskeni ile ayarlayin.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            ValidIssuer = jwtSection.GetValue<string>("Issuer"),
            ValidAudience = jwtSection.GetValue<string>("Audience"),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };

        options.Events = new JwtBearerEvents
        {
            // Tarayıcı token'ı httpOnly cookie'de taşır; Authorization başlığı (Swagger, diğer istemciler) önceliklidir
            OnMessageReceived = context =>
            {
                if (!context.Request.Headers.ContainsKey("Authorization")
                    && context.Request.Cookies.TryGetValue(AuthCookies.AccessToken, out var token))
                {
                    context.Token = token;
                    context.HttpContext.Items[AuthCookies.FromCookieItem] = true;
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                // Token imzası geçerli olsa da oturumu kapatılmışsa (çıkış, şifre değişikliği, pasifleştirme) reddedilir
                var sessionId = context.Principal is { } principal ? SessionClaims.GetSessionId(principal) : null;
                var sessions = context.HttpContext.RequestServices.GetRequiredService<IAuthSessionRepository>();
                if (sessionId is null || !await sessions.IsActiveAsync(sessionId.Value))
                {
                    context.Fail("Oturum sonlandırılmış.");
                    return;
                }

                // CSRF: cookie ile gelen ve veri değiştiren isteklerde özel başlık zorunlu
                var request = context.HttpContext.Request;
                var unsafeMethod = !(HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) || HttpMethods.IsOptions(request.Method));
                if (unsafeMethod && context.HttpContext.Items.ContainsKey(AuthCookies.FromCookieItem) && !AuthCookies.HasClientHeader(request))
                {
                    context.Fail($"Cookie ile yapılan isteklerde {AuthCookies.ClientHeader} başlığı zorunlu.");
                }
            },
        };
    });

builder.Services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(365));
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

// Kestrel konfigürasyonu mutlaka Build'ten ÖNCE olmalı!
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MinRequestBodyDataRate = null;
    options.Limits.MaxRequestBodySize = 15 * 1024 * 1024; // 15 MB
});

// Port kodda sabitlenmez: Docker'da ASPNETCORE_URLS / ASPNETCORE_HTTP_PORTS (8080),
// yerelde launchSettings.json kullanılır.
var app = builder.Build();

if (logEmailsOnly)
{
    // Kayıt "bağlantı gönderildi" der ama e-posta gitmez; açılışta açıkça söyle
    app.Logger.LogWarning("SMTP ayarlı değil (Smtp:Host): e-postalar GÖNDERİLMEYECEK, yalnızca bu konsola yazılacak. Ayarlamak için README'deki E-mail bölümüne bakın.");
}


// ---------------- Middleware ----------------
app.UseForwardedHeaders(); // en başta: sonraki her şey ziyaretçinin gerçek IP'sini ve şemasını görsün
app.UseMiddleware<SecurityHeadersMiddleware>();

// Swagger geliştirmede hep açık; sunucuda yalnızca Swagger__Enabled=true ile (iş bitince kapatın)
if (app.Environment.IsDevelopment() || app.Configuration.GetValue("Swagger:Enabled", false))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HappyDay API v1");
        c.RoutePrefix = "swagger";
    });
}

if (!app.Environment.IsDevelopment())
{
    // Tarayıcı bu alan adına bir yıl boyunca yalnızca HTTPS ile gelsin
    app.UseHsts();
}

// Global exception handling — yakalanmayan hatalar 500 + stack trace sizmasin
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    context.Response.ContentType = "application/json";

    var feature = context.Features.Get<IExceptionHandlerFeature>();

    // Geçersiz dosya yüklemesi istemci hatasıdır; mesajı kullanıcıya gösterilebilir
    if (feature?.Error is FileValidationException fileError)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new
        {
            isSuccess = false,
            message = fileError.Message
        });
        return;
    }

    // Tekil indeks ihlali (ör. aynı anda gelen iki kayıt isteği ön kontrolü birlikte geçtiğinde)
    if (feature?.Error is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } unique })
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        await context.Response.WriteAsJsonAsync(new
        {
            isSuccess = false,
            message = unique.ConstraintName is "IX_Users_Email_Lower" or "IX_Companies_Email_Lower"
                ? "Bu e-posta ile zaten bir hesap var."
                : "Bu kayıt zaten mevcut."
        });
        return;
    }

    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    app.Logger.LogError(feature?.Error, "Islenmeyen hata: {Path}", context.Request.Path);

    await context.Response.WriteAsJsonAsync(new
    {
        isSuccess = false,
        message = "Beklenmeyen bir hata olustu. Lutfen daha sonra tekrar deneyin."
    });
}));

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("DefaultCors");
app.UseRequestLocalization();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Kök adres boş 404 dönünce bazı tarayıcılar dosya indiriyordu
app.MapGet("/", () => Results.Text("Mutlu Günüm API çalışıyor. Sağlık kontrolü: /health"));

await app.PrepareDatabaseAsync();

app.Run();

// Entegrasyon testleri (WebApplicationFactory<Program>) için
public partial class Program;
