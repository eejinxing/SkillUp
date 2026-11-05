using Microsoft.AspNetCore.Mvc;
using Npgsql;
using SkillUp.Services;

var builder = WebApplication.CreateBuilder(args);
Settings.Use(builder.Configuration);

// ---- database ---------------------------------------------------------------------------------
// Connection string "SkillUp" (appsettings.json). The password is not stored in the project: put it in pgpass.conf, in the
// PGPASSWORD environment variable, or in user-secrets (dotnet user-secrets set "ConnectionStrings:SkillUp" "...").
var connectionString = builder.Configuration.GetConnectionString("SkillUp")
    ?? throw new InvalidOperationException("Set ConnectionStrings:SkillUp in appsettings.json.");
var dataSource = NpgsqlDataSource.Create(connectionString);
builder.Services.AddSingleton(dataSource);

// ---- web --------------------------------------------------------------------------------------
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "SkillUp.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromHours(2);
});
builder.Services.AddAuthentication(AuthCookie.Scheme).AddCookie(AuthCookie.Scheme, options =>
{
    options.Cookie.Name = AuthCookie.Scheme;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromHours(12);
    options.SlidingExpiration = false;
});
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "SkillUp.Antiforgery";
    options.Cookie.SameSite = SameSiteMode.Lax;
});

var app = builder.Build();

DatabaseSetup.Run(dataSource, connectionString, app.Environment.ContentRootPath);

if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Error");

app.Use(async (context, next) =>
{
    // Browsers must not guess file types, show the site inside another site's frame, or send full page addresses to other sites.
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "SAMEORIGIN";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "geolocation=(), payment=(), usb=()";
    await next();
});

app.UseStaticFiles(new StaticFileOptions
{
    // Font files never change under the same name: browsers keep them for a week instead of asking again on every page.
    OnPrepareResponse = context =>
    {
        var path = context.Context.Request.Path;
        if (path.StartsWithSegments("/assets/visitor") || path.StartsWithSegments("/assets/registered"))
            context.Context.Response.Headers.CacheControl = "public,max-age=604800";
    }
});
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.MapControllers();

app.Run();
