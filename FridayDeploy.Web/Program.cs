using System.Threading.RateLimiting;
using FridayDeploy.Web;
using FridayDeploy.Web.Authentication;
using FridayDeploy.Web.BackgroundServices;
using FridayDeploy.Web.Data;
using FridayDeploy.Web.Extensions;
using FridayDeploy.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.EnsureSqliteDirectoryExists();

builder.Services.AddControllersWithViews();
builder.Services.AddRequestDecompression();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<LogService>();
builder.Services.AddScoped<LogSearchService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ExplorerService>();
builder.Services.AddScoped<ApiKeyService>();
builder.Services.AddScoped<ExportService>();
builder.Services.AddScoped<SavedSearchService>();
builder.Services.AddScoped<BookmarkService>();
builder.Services.AddScoped<NoteService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddHostedService<LogRetentionService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FridayDeploy API",
        Version = "v1",
        Description = "Log ingestion, search, and management API for FridayDeploy. Ingestion endpoints authenticate with an API key (`X-Api-Key` header); everything else uses the browser session cookie."
    });

    options.AddSecurityDefinition(ApiKeyAuthenticationDefaults.Scheme, new OpenApiSecurityScheme
    {
        Name = ApiKeyAuthenticationDefaults.HeaderName,
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "API key issued from the FridayDeploy admin UI (Settings → API Keys)."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = ApiKeyAuthenticationDefaults.Scheme } },
            []
        }
    });

    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/Login";
        options.Cookie.Name = "FridayDeploy.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
    })
    .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationDefaults.Scheme, _ => { });
builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Ingestion: generous per-IP limit, since a single application can legitimately send many batches.
    options.AddPolicy(RateLimitPolicies.Ingestion, httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = 300, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

    // Login: tight per-IP limit to slow down credential-guessing.
    options.AddPolicy(RateLimitPolicies.Login, httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var app = builder.Build();

await app.InitializeDatabaseAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRequestDecompression();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "FridayDeploy API v1"));

app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "FridayDeploy", utc = DateTime.UtcNow })).AllowAnonymous();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

// Exposed so FridayDeploy.Tests can host the app via WebApplicationFactory<Program>.
public partial class Program;
