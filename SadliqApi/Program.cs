using Business.Abstract;
using Business.Concrete;
using Business.Security.Jwt;
using Core.Utilities.Security.Encryption;
using DataAccess.Abstract;
using DataAccess.Concrete;
using DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// EF entity-lərində qarşılıqlı (iki tərəfli) referanslar olduğu üçün JSON-da dövr yaranmasının qarşısını alır
builder.Services.AddControllers()
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

    // Endpoint-lərin Swagger-də C# metod adı ilə görünməsi (məs: api/venues/CreateAsync)
    c.CustomOperationIds(apiDesc =>
        apiDesc.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor cad
            ? cad.MethodInfo.Name
            : null);
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

// Servis qeydiyyatı
builder.Services.AddScoped<IAuthService, AuthManager>();
builder.Services.AddScoped<ITokenHelper, JwtHelper>();
builder.Services.AddScoped<IVenueService, VenueManager>();
builder.Services.AddScoped<IHallService, HallManager>();
builder.Services.AddScoped<IMenuService, MenuManager>();
builder.Services.AddScoped<IReservationService, ReservationManager>();

// appsettings.json-dan JWT parametrlərini oxu
var tokenOptions = builder.Configuration.GetSection("TokenOptions").Get<TokenOptions>();

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SadliqApi V1");
        c.DisplayOperationId();
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
