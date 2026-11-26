using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Hms.Api.Middleware;
using FluentValidation;
using Hms.Application.Common;
using Hms.Application.Security;
using Hms.Domain.Enums;
using Hms.Application.Services;
using Hms.Infrastructure.Auth;
using Hms.Infrastructure.Jobs;
using Hms.Infrastructure.Identity;
using Hms.Infrastructure.Persistence;
using Hms.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;

// Composition root: wiring is intentionally explicit so layer boundaries and auth policy generation are visible.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddProblemDetails();
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ValidationBehavior<,>).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(ValidationBehavior<,>).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddOpenApi();

// Persistence: Npgsql plus snake_case naming matches the existing clinic schema.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is required.");
builder.Services.AddDbContext<HmsDbContext>(options => options
    .UseNpgsql(connectionString)
    .UseSnakeCaseNamingConvention());

builder.Services.AddIdentityCore<AppUser>(options =>
    {
        options.Password.RequiredLength = 10;
        options.Password.RequireNonAlphanumeric = true;
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<HmsDbContext>()
    .AddDefaultTokenProviders();

// Authentication/authorization: one policy per permission; Admin receives permission claims at login.
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services.AddOptions<JwtOptions>()
    .Bind(jwtSection)
    .Validate(x => !string.IsNullOrWhiteSpace(x.Issuer) && !string.IsNullOrWhiteSpace(x.Audience), "JWT issuer and audience are required.")
    .Validate(x => x.SigningKey.Length >= 32, "Jwt:SigningKey must be at least 32 characters.")
    .ValidateOnStart();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, configured) =>
    {
        var jwt = configured.Value;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in PermissionCatalog.All)
    {
        options.AddPolicy(permission, policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim("permission", permission));
    }

    // Handover is nurse-only by design. The role claim exists in every issued token,
    // including tokens issued before newer feature permissions were introduced.
    options.AddPolicy(AppPolicies.NurseOnly, policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim("staff_role", StaffRole.Nurse.ToString()));
});

builder.Services.AddCors(options => options.AddPolicy("web", policy =>
{
    policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200"])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
}));

builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddScoped<IAuditWriter, AuditWriter>();
builder.Services.AddScoped<INumberGenerator, NumberGenerator>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IAppointmentPricingService, AppointmentPricingService>();
builder.Services.AddScoped<IClinicalService, ClinicalService>();
builder.Services.AddScoped<IPharmacyService, PharmacyService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IHmoService, HmoService>();
builder.Services.AddScoped<ITariffPricingService, TariffPricingService>();
builder.Services.AddScoped<IFitnessService, FitnessService>();
builder.Services.AddScoped<IHrService, HrService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAuditQueryService, AuditQueryService>();
// Application ports are implemented in Infrastructure; controllers never resolve them directly.
builder.Services.AddScoped<IReferenceService, ReferenceService>();
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection(FileStorageOptions.SectionName));
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
builder.Services.AddScoped<IPatientProfileService, PatientProfileService>();
builder.Services.AddScoped<IStaffProfileService, StaffProfileService>();
builder.Services.AddScoped<INurseService, NurseService>();
builder.Services.AddScoped<IFundingService, FundingService>();
builder.Services.AddScoped<IStocksService, StocksService>();
builder.Services.AddScoped<IResultsService, ResultsService>();
builder.Services.AddScoped<IStockImportService, StockImportService>();
builder.Services.AddHostedService<ExpiryWorker>();

var app = builder.Build();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapOpenApi("/openapi/v1.json");
app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTimeOffset.UtcNow }));
app.MapGet("/", () => Results.Ok(new { name = "HMS API", docs = "/openapi/v1.json", health = "/health" }));

// Development convenience: migrate and seed before accepting traffic.
if (builder.Configuration.GetValue("Database:AutoMigrateAndSeed", true))
{
    await DbSeeder.SeedAsync(app.Services);
}

app.Run();

public partial class Program;
