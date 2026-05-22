using System.Text;
using LicenseManager.API.Authorization;
using LicenseManager.API.Hangfire;
using LicenseManager.API.Jobs;
using LicenseManager.API.Middleware;
using LicenseManager.Infrastructure;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// LOGGING (Serilog)
// =============================================================================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/licensemanager-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// =============================================================================
// SERVICES
// =============================================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "License Manager API",
        Version = "v1",
        Description = "Enterprise License Management System API",
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme",
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            },
            Array.Empty<string>()
        },
    });
});

// Infrastructure (DbContext, services, current user, JWT, password hashing, MFA)
builder.Services.AddInfrastructure(builder.Configuration);

// =============================================================================
// HANGFIRE (Background jobs - storage, server, dashboard auth filter, jobs)
// =============================================================================
builder.Services.Configure<HangfireOptions>(
    builder.Configuration.GetSection(HangfireOptions.SectionName));

var hangfireConnectionString = builder.Configuration.GetConnectionString("Hangfire")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "No connection string found for Hangfire. Set ConnectionStrings:Hangfire or ConnectionStrings:DefaultConnection.");

var hangfireSchema = builder.Configuration["Hangfire:SchemaName"] ?? "hangfire";
var prepareSchema = builder.Configuration.GetValue<bool?>("Hangfire:PrepareSchemaIfNecessary") ?? true;

builder.Services.AddHangfire((sp, config) =>
{
    config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(hangfireConnectionString),
            new PostgreSqlStorageOptions
            {
                SchemaName = hangfireSchema,
                PrepareSchemaIfNecessary = prepareSchema,
                QueuePollInterval = TimeSpan.FromSeconds(15),
                InvisibilityTimeout = TimeSpan.FromMinutes(30),
                DistributedLockTimeout = TimeSpan.FromMinutes(1),
                UseNativeDatabaseTransactions = true,
            });
});

builder.Services.AddHangfireServer(options =>
{
    var configuredWorkers = builder.Configuration.GetValue<int?>("Hangfire:WorkerCount") ?? 0;
    options.WorkerCount = configuredWorkers > 0
        ? configuredWorkers
        : Math.Max(Environment.ProcessorCount * 2, 4);

    var queues = builder.Configuration.GetSection("Hangfire:Queues").Get<string[]>();
    options.Queues = queues is { Length: > 0 } ? queues : new[] { "critical", "default", "low" };

    options.ServerName = $"licensemanager-{Environment.MachineName}";
});

builder.Services.AddSingleton<HangfireDashboardAuthorizationFilter>();

// Recurring job classes (Hangfire activates these per execution via DI scope).
builder.Services.AddScoped<LicenseExpiryReminderJob>();
builder.Services.AddScoped<LicenseExpiryWarning30DaysJob>();
builder.Services.AddScoped<LicenseExpiryWarning7DaysJob>();
builder.Services.AddScoped<DailyLicenseValidationJob>();
builder.Services.AddScoped<DailyCleanupJob>();
builder.Services.AddScoped<AuditLogCleanupJob>();
builder.Services.AddScoped<NotificationQueueProcessorJob>();
builder.Services.AddScoped<FailedNotificationRetryJob>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("LicenseManagerCors", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? new[] { "http://localhost:3000", "http://localhost:3001", "http://localhost:3002" };

        policy.WithOrigins(origins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// =============================================================================
// JWT AUTHENTICATION
// =============================================================================
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured. Set it in appsettings or environment.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "LicenseManagerAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "LicenseManagerClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = builder.Environment.IsProduction();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(2),
        RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        NameClaimType = System.Security.Claims.ClaimTypes.Name,
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = ctx =>
        {
            if (ctx.Exception is SecurityTokenExpiredException)
                ctx.Response.Headers["Token-Expired"] = "true";
            return Task.CompletedTask;
        },
    };
});

// =============================================================================
// AUTHORIZATION (Role-based policies)
// =============================================================================
builder.Services.AddAuthorization(options => options.AddLicenseManagerPolicies());

var app = builder.Build();

// =============================================================================
// PIPELINE
// =============================================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "License Manager API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseSerilogRequestLogging();

if (app.Environment.IsProduction())
    app.UseHttpsRedirection();

app.UseCors("LicenseManagerCors");

app.UseAuthentication();
app.UseAuthorization();

// API request logging middleware (after auth so we know the user)
app.UseMiddleware<ApiLoggingMiddleware>();

// =============================================================================
// HANGFIRE DASHBOARD
// =============================================================================
var hangfireDashboardPath = builder.Configuration["Hangfire:DashboardPath"] ?? "/hangfire";
app.UseHangfireDashboard(hangfireDashboardPath, new DashboardOptions
{
    DashboardTitle = "License Manager Jobs",
    Authorization = new[] { app.Services.GetRequiredService<HangfireDashboardAuthorizationFilter>() },
    DisplayStorageConnectionString = false,
    IgnoreAntiforgeryToken = false,
});

// Register/refresh every recurring job after the host is built. AddOrUpdate is
// idempotent, so this is safe to run on every startup.
RecurringJobScheduler.RegisterAll(app.Services);

app.MapControllers();

app.MapGet("/", () => new
{
    service = "License Manager API",
    version = "1.0.0",
    status = "Running",
    timestamp = DateTime.UtcNow,
    documentation = "/swagger",
});

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    timestamp = DateTime.UtcNow,
}));

try
{
    Log.Information("Starting License Manager API on {Environment}", app.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
