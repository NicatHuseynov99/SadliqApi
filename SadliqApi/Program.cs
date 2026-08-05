using Business.Abstract;
using Business.Concrete;
using Business.Concrete.Notifications;
using Business.Security.Jwt;
using Business.Utilities.Email;
using SadliqApi.Audit;
using SadliqApi.Texts;
using SadliqApi.Visits;
using Core.Utilities.Security.Encryption;
using DataAccess.Abstract;
using DataAccess.Concrete;
using DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// EF entity-lərində qarşılıqlı (iki tərəfli) referanslar olduğu üçün JSON-da dövr yaranmasının qarşısını alır
builder.Services.AddControllers(options =>
    {
        // Hər sorğu fəaliyyət jurnalına düşür (oxuma sorğularının bir hissəsi istisna olmaqla)
        options.Filters.Add<AuditActionFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();

// Swagger-i JWT Bearer dəstəyi ilə konfiqurasiya et
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Token daxil et. Nümunə: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

});

// PostgreSQL bağlantısı
builder.Services.AddDbContext<SadliqDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// DAL qeydiyyatı
builder.Services.AddScoped<IUserDal, EfUserDal>();
builder.Services.AddScoped<IVenueDal, EfVenueDal>();
builder.Services.AddScoped<IVenueImageDal, EfVenueImageDal>();
builder.Services.AddScoped<IHallDal, EfHallDal>();
builder.Services.AddScoped<IHallMediaDal, EfHallMediaDal>();
builder.Services.AddScoped<IMenuDal, EfMenuDal>();
builder.Services.AddScoped<IMenuItemDal, EfMenuItemDal>();
builder.Services.AddScoped<IHallAvailabilityDal, EfHallAvailabilityDal>();
builder.Services.AddScoped<IReservationDal, EfReservationDal>();
builder.Services.AddScoped<IFavoriteDal, EfFavoriteDal>();
builder.Services.AddScoped<IReviewDal, EfReviewDal>();
builder.Services.AddScoped<IPasswordResetTokenDal, EfPasswordResetTokenDal>();
builder.Services.AddScoped<IVenueStaffDal, EfVenueStaffDal>();
builder.Services.AddScoped<IVenueFeatureDal, EfVenueFeatureDal>();
builder.Services.AddScoped<IVenueStaffHallDal, EfVenueStaffHallDal>();
builder.Services.AddScoped<IAuditLogDal, EfAuditLogDal>();
builder.Services.AddScoped<IPageViewDal, EfPageViewDal>();
builder.Services.AddScoped<IReviewReportDal, EfReviewReportDal>();
builder.Services.AddScoped<IPlatformSettingDal, EfPlatformSettingDal>();
builder.Services.AddScoped<IReviewEditDal, EfReviewEditDal>();
builder.Services.AddScoped<ICityDal, EfCityDal>();
builder.Services.AddScoped<IFeatureDal, EfFeatureDal>();
builder.Services.AddScoped<IAppTextDal, EfAppTextDal>();
builder.Services.AddScoped<INotificationDal, EfNotificationDal>();
builder.Services.AddScoped<INotificationCampaignDal, EfNotificationCampaignDal>();
builder.Services.AddScoped<INotificationPreferenceDal, EfNotificationPreferenceDal>();
builder.Services.AddScoped<IAudienceDal, EfAudienceDal>();
builder.Services.AddScoped<IReservationCounterOfferDal, EfReservationCounterOfferDal>();
builder.Services.AddScoped<IVenueRequestDal, EfVenueRequestDal>();
builder.Services.AddScoped<IVenueOfferDal, EfVenueOfferDal>();

// Servis qeydiyyatı
builder.Services.AddScoped<IAuthService, AuthManager>();
builder.Services.AddScoped<ITokenHelper, JwtHelper>();
builder.Services.AddScoped<IVenueService, VenueManager>();
builder.Services.AddScoped<IHallService, HallManager>();
builder.Services.AddScoped<IMenuService, MenuManager>();
builder.Services.AddScoped<IReservationService, ReservationManager>();
builder.Services.AddScoped<IFavoriteService, FavoriteManager>();
builder.Services.AddScoped<IReviewService, ReviewManager>();
builder.Services.AddScoped<IVenueAccessService, VenueAccessManager>();
builder.Services.AddScoped<IVenueStaffService, VenueStaffManager>();
builder.Services.AddScoped<IAdminService, AdminManager>();
builder.Services.AddScoped<IAuditService, AuditManager>();
builder.Services.AddScoped<IVisitService, VisitManager>();
builder.Services.AddScoped<IReviewModerationService, ReviewModerationManager>();
builder.Services.AddScoped<IAdminReservationService, AdminReservationManager>();
builder.Services.AddScoped<IPlatformSettingsService, PlatformSettingsManager>();
builder.Services.AddScoped<IAdminVenueService, AdminVenueManager>();
builder.Services.AddScoped<IDataExplorerService, DataExplorerManager>();
builder.Services.AddScoped<IVenueApplicationService, VenueApplicationManager>();
builder.Services.AddScoped<IVenueRequestService, VenueRequestManager>();
builder.Services.AddScoped<IOwnerStatsService, OwnerStatsManager>();

// Bildiriş axını: kanallar sıra ilə qeydiyyatdan keçir, göndərici onları
// istifadəçinin seçimlərinə görə süzür. Yeni kanal əlavə etmək = bir sətir.
builder.Services.AddScoped<INotificationChannel, InAppNotificationChannel>();
builder.Services.AddScoped<INotificationChannel, EmailNotificationChannel>();
builder.Services.AddScoped<INotificationChannel, SmsNotificationChannel>();
builder.Services.AddScoped<INotificationService, NotificationManager>();
builder.Services.AddScoped<INotificationCampaignService, NotificationCampaignManager>();
builder.Services.AddHostedService<SadliqApi.Notifications.CampaignDispatchService>();

// Mətn kataloqu: bazadan bir dəfə oxunur, yaddaşda saxlanılır
builder.Services.AddSingleton<ITextCatalog, TextCatalog>();

// Fəaliyyət jurnalı: kontekst sorğu boyunca yaşayır, yazı isə arxa fonda gedir
builder.Services.AddScoped<IAuditContext, AuditContext>();
builder.Services.AddSingleton<IAuditWriter, AuditWriter>();
builder.Services.AddHostedService<AuditBackgroundService>();

// Ziyarətçi statistikası: yazı arxa fonda, köhnə qeydlər gündə bir dəfə təmizlənir
builder.Services.AddSingleton<IVisitWriter, VisitWriter>();
builder.Services.AddHostedService<VisitBackgroundService>();
builder.Services.AddHostedService<VisitCleanupService>();

// Cavabsız rezerv sorğuları: müddəti bitən tutulmalar boşaldılır, sahibə xatırladılır
builder.Services.AddHostedService<SadliqApi.Reservations.ReservationHoldService>();

// Heç bir saraya bağlanmayan köhnə yüklənmiş fayllar gündə bir dəfə silinir
builder.Services.AddHostedService<SadliqApi.Files.UploadCleanupService>();

// E-poçt: SMTP konfiqurasiya olunmayıbsa məktublar log-a yazılır
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("EmailOptions"));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// appsettings.json-dan JWT parametrlərini oxu
var tokenOptions = builder.Configuration.GetSection("TokenOptions").Get<TokenOptions>()
    ?? throw new InvalidOperationException("appsettings.json-da 'TokenOptions' bölməsi tapılmadı.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = tokenOptions.Issuer,
            ValidAudience = tokenOptions.Audience,
            IssuerSigningKey = SecurityKeyHelper.CreateSecurityKey(tokenOptions.SecurityKey)
        };
    });

builder.Services.AddAuthorization();

// Sorğu tezliyinə məhdudiyyət. Hədəf üç yerdir: parol sınağı, rəy spamı,
// fayl yükləmə. Limit IP üzrədir — hesab açmadan da hücum etmək olar.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"success\":false,\"message\":\"Çox sayda sorğu göndərdiniz. Bir az gözləyin.\"}", token);
    };

    // Giriş, qeydiyyat, parol bərpası — dəqiqədə 8 cəhd
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        ClientKey(context),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 8, Window = TimeSpan.FromMinutes(1) }));

    // Rəy, rezerv, şikayət kimi məzmun yaradan əməliyyatlar — dəqiqədə 10
    options.AddPolicy("write", context => RateLimitPartition.GetFixedWindowLimiter(
        ClientKey(context),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));

    // Fayl yükləmə — saatda 30 fayl
    options.AddPolicy("upload", context => RateLimitPartition.GetFixedWindowLimiter(
        ClientKey(context),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromHours(1) }));
});

// Giriş etmiş istifadəçi öz hesabı üzrə, qonaq isə IP üzrə sayılır
static string ClientKey(HttpContext context) =>
    context.User?.Identity?.IsAuthenticated == true
        ? $"user:{context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value}"
        : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

// Frontend-in (localhost:3000) API-yə müraciətinə icazə verir
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SadliqApi V1");
        
    });
}

app.UseHttpsRedirection();

// Yüklənən şəkil və videolar wwwroot/uploads qovluğundan verilir
app.UseStaticFiles();

app.UseCors("AllowFrontend");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
