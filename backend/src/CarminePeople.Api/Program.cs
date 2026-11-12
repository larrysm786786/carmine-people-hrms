using System.Text;
using Asp.Versioning;
using CarminePeople.Api.Middleware;
using CarminePeople.Application.Auth.Services;
using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Infrastructure.Auth;
using CarminePeople.Infrastructure.Persistence;
using CarminePeople.Infrastructure.Persistence.Seed;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

// Auth services
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CarminePeople.Application.Employees.Services.EmployeeService>();
builder.Services.AddScoped<CarminePeople.Application.Employees.Services.FaceEnrollmentService>();
builder.Services.AddScoped<CarminePeople.Application.Common.Time.CompanyClock>();
builder.Services.AddScoped<CarminePeople.Application.Attendance.Services.AttendanceService>();
builder.Services.AddScoped<CarminePeople.Application.Attendance.Services.AttendanceAnomalyService>();
builder.Services.AddScoped<CarminePeople.Application.Leave.Services.LeaveService>();
builder.Services.AddScoped<CarminePeople.Application.Leave.Services.LeavePredictionService>();
builder.Services.AddScoped<CarminePeople.Application.Overtime.Services.OvertimeService>();
builder.Services.AddScoped<CarminePeople.Application.ShiftSwap.Services.ShiftSwapService>();
builder.Services.AddScoped<CarminePeople.Application.Payroll.Services.PayrollService>();
builder.Services.Configure<CarminePeople.Infrastructure.Notifications.FcmOptions>(builder.Configuration.GetSection("Fcm"));
builder.Services.AddScoped<IPushNotificationSender, CarminePeople.Infrastructure.Notifications.FirebasePushNotificationSender>();
builder.Services.AddScoped<CarminePeople.Application.Notifications.Services.NotificationService>();
builder.Services.Configure<CarminePeople.Infrastructure.Notifications.SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IEmailSender, CarminePeople.Infrastructure.Notifications.SmtpEmailSender>();
builder.Services.Configure<CarminePeople.Infrastructure.Notifications.PayrollOptions>(builder.Configuration.GetSection("Payroll"));
builder.Services.AddScoped<CarminePeople.Application.Announcements.Services.AnnouncementService>();
builder.Services.Configure<CarminePeople.Infrastructure.Storage.DocumentStorageOptions>(builder.Configuration.GetSection("DocumentStorage"));
builder.Services.AddScoped<IDocumentStorage, CarminePeople.Infrastructure.Storage.LocalFileDocumentStorage>();
builder.Services.AddScoped<CarminePeople.Application.Documents.Services.DocumentService>();
builder.Services.AddScoped<CarminePeople.Application.Reports.Services.ReportService>();
builder.Services.AddScoped<CarminePeople.Application.Dashboard.Services.DashboardService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditLogger, CarminePeople.Api.Auditing.AuditLogger>();
builder.Services.AddScoped<CarminePeople.Application.Audit.Services.AuditQueryService>();
builder.Services.AddScoped<CarminePeople.Application.Organization.Services.OrganizationService>();
builder.Services.AddScoped<CarminePeople.Application.Roles.Services.RoleService>();
builder.Services.AddHostedService<CarminePeople.Api.BackgroundJobs.ForgottenCheckoutJob>();
builder.Services.AddHostedService<CarminePeople.Api.BackgroundJobs.AttendanceReminderJob>();
builder.Services.AddHostedService<CarminePeople.Api.BackgroundJobs.CheckoutReminderJob>();
builder.Services.AddHostedService<CarminePeople.Api.BackgroundJobs.MonthlyAttendanceReportJob>();
builder.Services.AddHostedService<CarminePeople.Api.BackgroundJobs.MonthlySalaryJob>();
builder.Services.AddHostedService<CarminePeople.Api.BackgroundJobs.WorkHourShortfallJob>();

// AI HR assistant (Groq). Ai:Groq:ApiKey must be set for real responses; until
// then AiAssistantService.IsConfigured is false and the controller returns a
// friendly "not configured yet" message instead of calling out.
builder.Services.Configure<CarminePeople.Infrastructure.Ai.GroqOptions>(builder.Configuration.GetSection("Ai:Groq"));
builder.Services.AddHttpClient<CarminePeople.Application.Common.Interfaces.IAiClient, CarminePeople.Infrastructure.Ai.GroqAiClient>();
builder.Services.AddScoped<CarminePeople.Application.Ai.Services.AiAssistantService>();

// Validation
builder.Services.AddValidatorsFromAssembly(typeof(CarminePeople.Application.Auth.Validators.LoginRequestValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddFluentValidationAutoValidation();

// JWT bearer authentication.
// Resolved from IOptions<JwtSettings> inside the configure delegate (not a build-time
// local) so this always matches whatever configuration JwtTokenService used to sign
// the token — including host-builder-time overrides such as WebApplicationFactory's
// ConfigureAppConfiguration in integration tests, which apply after top-level builder
// statements have already run.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<Microsoft.Extensions.Options.IOptions<JwtSettings>>((options, jwtOptions) =>
    {
        var jwtSettings = jwtOptions.Value;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization(options =>
{
    // Sourced from Permissions.All so a new permission constant automatically gets a
    // matching policy — this previously hand-maintained list had already drifted out
    // of sync (missing overtime/shift-swap) before this fix.
    foreach (var permission in CarminePeople.Domain.Constants.Permissions.All)
    {
        options.AddPolicy(permission, policy => policy.RequireClaim("permission", permission));
    }
});

// API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Carmine People API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Rate limiting on auth endpoints (login, forgot-password) to blunt brute force / credential stuffing.
// Real production limits everywhere except the integration-test environment: a growing
// integration-test suite that shares one CarminePeopleApiFactory/test class naturally makes
// far more than 10 auth calls/min against a single in-process TestServer (no real distinct
// clients to partition by), so the production auth limit would eventually make the test
// suite itself flaky rather than testing anything real. Testing gets a much higher ceiling
// instead of no limiter at all, so a genuine runaway-request bug would still be caught.
var isTestingEnvironment = builder.Environment.IsEnvironment("Testing");
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Brute-force guard for the credential endpoints (login, forgot/reset password). Partitioned
    // per client IP: an unpartitioned limiter is one shared bucket for the whole company, so the
    // 11th login of a busy minute failed for everyone. Session endpoints (/me, /refresh, /logout)
    // are deliberately not on this policy — the admin app calls /me on every page load.
    options.AddPolicy("auth", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(ip, _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = isTestingEnvironment ? 1000 : 10,
            QueueLimit = 0,
        });
    });
    // AI chat proxies a Groq API call per request — a tighter, dedicated limit
    // caps worst-case cost/quota exposure independent of the general per-IP limiter below.
    options.AddFixedWindowLimiter("ai", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = isTestingEnvironment ? 1000 : 15;
        limiterOptions.QueueLimit = 0;
    });
    // Global fallback for every other endpoint (employees, attendance, documents, reports,
    // etc.) that doesn't opt into a named policy above — partitioned per client IP so one
    // caller can't starve others, and generous enough not to bother normal UI usage.
    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(ip, _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = isTestingEnvironment ? 100_000 : 300,
            QueueLimit = 0,
        });
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AdminAndMobile", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// Fail fast rather than silently signing JWTs with a publicly-known placeholder key in any
// non-Development environment — a missing real secret should stop deployment, not produce
// tokens anyone can forge.
if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    var jwtSettings = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<JwtSettings>>().Value;
    if (string.IsNullOrWhiteSpace(jwtSettings.SigningKey) || jwtSettings.SigningKey.StartsWith("REPLACE_WITH", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            "Jwt:SigningKey is not configured (still the placeholder value). Set it via the JWT_SIGNING_KEY environment variable before starting in a non-Development environment.");
    }
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

// TestServer (WebApplicationFactory) has no real TLS listener; redirecting there would
// strip the Authorization header on the scheme-changing redirect and break every
// authenticated integration test call. Real hosting environments always have HTTPS bound.
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    // This is a JSON API with no server-rendered HTML, so a locked-down CSP costs nothing.
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none'");
    if (context.Request.IsHttps)
    {
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    }
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await db.Database.MigrateAsync();
    await DevelopmentSeeder.SeedAsync(db, hasher);
    // Keeps an already-seeded DB in sync with permission codes introduced after it was first
    // seeded (DevelopmentSeeder only runs once, on an empty DB) — safe to run every startup.
    await CarminePeople.Infrastructure.Persistence.Seed.PermissionSyncSeeder.SyncAsync(db);
}

app.UseCors("AdminAndMobile");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
