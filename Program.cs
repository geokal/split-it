using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using QuizManager.Components;
using QuizManager.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. CONFIGURATION
// if (builder.Environment.IsDevelopment())
// {
//     builder.Configuration.AddUserSecrets<Program>();
// }

// 2. DATABASE
builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DbConnectionString"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    );
});

// 3. AUTHENTICATION (Server Side Only)
builder.Services.AddAuth0WebAppAuthentication(options =>
{
    options.Domain = builder.Configuration["Auth0:Domain"];
    options.ClientId = builder.Configuration["Auth0:ClientId"];
    // Callback path is handled automatically by Auth0 middleware
    // After callback, it will redirect to the redirectUri set in LoginAuthenticationPropertiesBuilder
});

// Configure Cookie Authentication to use /login instead of /Account/Login
builder.Services.Configure<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>(
    Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
    options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/";
    }
);

// 4. HTTP CLIENTS
builder.Services.AddHttpClient(); // General HttpClient factory for services like FrontPageService
builder.Services.AddHttpClient<GoogleScholarService>();
builder.Services.AddHttpClient<ICordisService, CordisService>();

// --- THE FIX IS HERE ---
builder.Services.AddHttpClient(
    "Auth0Api",
    client =>
    {
        // IMPORTANT: Note the '/' at the very end of this URL.
        // Without it, HttpClient strips the "v2" segment causing 404 errors.
        client.BaseAddress = new Uri("https://dev-75kcw8hj0pzojdod.us.auth0.com/api/v2/");
    }
);

// 5. APP SERVICES
builder.Services.AddRazorPages();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Configure authorization to allow anonymous access to root route
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = null; // Don't require authorization by default
});

builder.Services.AddScoped<QuizManager.Services.InternshipEmailService>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var emailSettings = config.GetSection("EmailSettings");
    return new QuizManager.Services.InternshipEmailService(
        emailSettings["SmtpUsername"],
        emailSettings["SmtpPassword"],
        emailSettings["SupportEmail"],
        emailSettings["NoReplyEmail"]
    );
});

// Also register the global namespace version (used by MainLayout and other components)
builder.Services.AddScoped<InternshipEmailService>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var emailSettings = config.GetSection("EmailSettings");
    return new InternshipEmailService(
        emailSettings["SmtpUsername"],
        emailSettings["SmtpPassword"],
        emailSettings["SupportEmail"],
        emailSettings["NoReplyEmail"]
    );
});

builder.Services.AddScoped<QuizManager.Data.IEmailService, QuizManager.Data.EmailService>();
builder.Services.AddScoped<FileUploadService>();
builder.Services.AddScoped<IAuth0Service, Auth0Service>();
builder.Services.AddMemoryCache();

// Register Dashboard Services
builder.Services.AddScoped<
    QuizManager.Services.UserContext.IUserContextService,
    QuizManager.Services.UserContext.UserContextService
>();
builder.Services.AddScoped<
    QuizManager.Services.UserContext.IUserRoleService,
    QuizManager.Services.UserContext.UserRoleService
>();
builder.Services.AddScoped<
    QuizManager.Services.Authentication.ICacheService,
    QuizManager.Services.Authentication.CacheService
>();
builder.Services.AddScoped<
    QuizManager.Services.Authentication.IRoleValidator,
    QuizManager.Services.Authentication.RoleValidator
>();
builder.Services.AddScoped<
    QuizManager.Services.Authentication.IAuditLogRepository,
    QuizManager.Services.Authentication.AuditLogRepository
>();
builder.Services.AddScoped<
    QuizManager.Services.StudentDashboard.IStudentDashboardService,
    QuizManager.Services.StudentDashboard.StudentDashboardService
>();
builder.Services.AddScoped<
    QuizManager.Services.CompanyDashboard.ICompanyDashboardService,
    QuizManager.Services.CompanyDashboard.CompanyDashboardService
>();
builder.Services.AddScoped<
    QuizManager.Services.ProfessorDashboard.IProfessorDashboardService,
    QuizManager.Services.ProfessorDashboard.ProfessorDashboardService
>();
builder.Services.AddScoped<
    QuizManager.Services.ResearchGroupDashboard.IResearchGroupDashboardService,
    QuizManager.Services.ResearchGroupDashboard.ResearchGroupDashboardService
>();
builder.Services.AddScoped<
    QuizManager.Services.FrontPage.IFrontPageService,
    QuizManager.Services.FrontPage.FrontPageService
>();

var app = builder.Build();

// Auto-apply migrations only in Production
if (!app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var db = factory.CreateDbContext();
        db.Database.Migrate();
    }
}

// 6. MIDDLEWARE PIPELINE
// Add Auth0 token refresh middleware before authentication
app.UseMiddleware<QuizManager.Middleware.AuthTokenRefreshMiddleware>();
app.UseForwardedHeaders(
    new ForwardedHeadersOptions
    {
        ForwardedHeaders =
            ForwardedHeaders.XForwardedFor
            | ForwardedHeaders.XForwardedProto
            | ForwardedHeaders.XForwardedHost,
    }
);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorPages();
app.MapRazorComponents<QuizManager.Components.App>().AddInteractiveServerRenderMode();

app.Run();
