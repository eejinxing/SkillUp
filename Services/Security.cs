using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using SkillUp.Models;

namespace SkillUp.Services;

/// <summary>An error the app reports to the browser with an HTTP status: a notice on a page, or { "detail": message } from the API.</summary>
public sealed class ApiException : Exception
{
    public int Status { get; }

    public ApiException(int status, string message) : base(message) => Status = status;
}

/// <summary>Reads appsettings.json / user-secrets / environment variables (set once in Program.cs).</summary>
public static class Settings
{
    static IConfiguration config;

    public static void Use(IConfiguration configuration) => config = configuration;

    public static string Get(string key)
    {
        var value = config?[key];
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>Loads Database/demo-seed.json into an empty database on first start.</summary>
    public static bool SeedDemoData => config?.GetValue("SkillUp:SeedDemoData", false) ?? false;

    /// <summary>There is no email service yet, so the reset link is shown on screen instead of emailed.</summary>
    public static bool ShowResetLink => config?.GetValue("SkillUp:ShowResetLink", false) ?? false;
}

static class Passwords
{
    const int Iterations = 600000;

    /// <summary>Checked when the account does not exist, so a wrong user name takes as long as a wrong password.</summary>
    public static readonly string DummyHash = Hash("skillup-dummy-password");

    /// <summary>Stored hashes made with fewer iterations are upgraded the next time their owner signs in.</summary>
    public static bool NeedsRehash(string stored)
    {
        var parts = (stored ?? "").Split('$');
        return parts.Length != 4 || parts[0] != "PBKDF2" || !int.TryParse(parts[1], out var iterations) || iterations < Iterations;
    }

    /// <summary>Null when the password is acceptable, otherwise the message to show.</summary>
    public static string Problem(string password)
    {
        password ??= "";
        if (password.Length < 8 || password.Length > 128) return "Use 8 to 128 characters for the password.";
        if (!password.Any(char.IsUpper)) return "Add at least one capital letter.";
        if (!password.Any(char.IsDigit)) return "Add at least one number.";
        if (!password.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c))) return "Add at least one special character, such as ! @ # $ %.";
        return null;
    }

    /// <summary>Stored as PBKDF2$iterations$salt$hash (SHA-256, base64).</summary>
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        return "PBKDF2$" + Iterations + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(Derive(password, salt, Iterations));
    }

    public static bool Verify(string password, string stored)
    {
        var parts = (stored ?? "").Split('$');
        if (parts.Length != 4 || parts[0] != "PBKDF2" || !int.TryParse(parts[1], out var iterations)) return false;
        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }
        return CryptographicOperations.FixedTimeEquals(Derive(password ?? "", salt, iterations), expected);
    }

    static byte[] Derive(string password, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, 32);
}

/// <summary>
/// Slows down guessing: after too many failures a key (an account name, an address) is locked for a while. Kept in memory,
/// which fits a single server process; a restart clears it.
/// </summary>
static class AttemptGuard
{
    class Entry
    {
        public int Failures;
        public DateTime First = DateTime.UtcNow;
        public DateTime LockedUntil;
    }

    static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    static readonly Dictionary<string, Entry> entries = new();

    /// <summary>Throws 429 while any of the keys is locked.</summary>
    public static void Check(params string[] keys)
    {
        lock (entries)
        {
            foreach (var key in keys)
                if (entries.TryGetValue(key, out var e) && e.LockedUntil > DateTime.UtcNow)
                    throw new ApiException(429, "Too many attempts. Try again in " + Math.Max(1, (int)Math.Ceiling((e.LockedUntil - DateTime.UtcNow).TotalMinutes)) + " minutes.");
        }
    }

    /// <summary>Counts one failure; the key locks for 15 minutes once it reaches the limit.</summary>
    public static void Fail(string key, int limit)
    {
        lock (entries)
        {
            var now = DateTime.UtcNow;
            if (entries.Count > 5000)
                foreach (var stale in entries.Where(x => now - x.Value.First > Window && x.Value.LockedUntil < now).Select(x => x.Key).ToList()) entries.Remove(stale);
            if (!entries.TryGetValue(key, out var e) || (now - e.First > Window && e.LockedUntil < now)) entries[key] = e = new Entry();
            if (++e.Failures >= limit) e.LockedUntil = now + Window;
        }
    }

    public static void Clear(string key)
    {
        lock (entries) entries.Remove(key);
    }
}

static class Tokens
{
    public static string New() => Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));

    public static string Sha256(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value ?? "")));
}

/// <summary>
/// A random key that lets the database recognise this browser for "My enquiries". ENQUIRY_ACCESS stores only its
/// SHA-256 hash. HttpOnly, so page scripts cannot read it; nothing else about the visitor is kept in the browser.
/// </summary>
static class VisitorCookie
{
    const string Name = "SkillUp.Visitor";

    public static string Read(HttpContext context)
    {
        var value = context.Request.Cookies[Name];
        return string.IsNullOrEmpty(value) || value.Length > 64 ? null : value;
    }

    public static void Issue(HttpContext context, string key) =>
        context.Response.Cookies.Append(Name, key, new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = DateTimeOffset.Now.AddYears(1)
        });
}

/// <summary>
/// Sign-in cookie: ASP.NET Core cookie authentication (encrypted, HttpOnly, SameSite=Lax) holding the user id and a stamp of the
/// current password. The account is reloaded on every request, so a deactivated account loses access at once, and changing or
/// resetting the password ends every other session.
/// </summary>
static class AuthCookie
{
    public const string Scheme = "SkillUp.Auth";
    const string StampClaim = "stamp";

    static string Stamp(string passwordHash) => Tokens.Sha256(passwordHash)[..16];

    /// <summary>The signed-in, still active account whose password has not changed since the cookie was issued, or null.</summary>
    public static UserRow Resolve(HttpContext context, Snapshot data)
    {
        if (!TryRead(context, out var id, out var stamp)) return null;
        var user = data.User(id);
        return user != null && user.Status == "Active" && stamp == Stamp(user.Password) ? user : null;
    }

    /// <summary>Same check for callers that have no Snapshot loaded (call signalling).</summary>
    public static int? Resolve(HttpContext context, Db db)
    {
        if (!TryRead(context, out var id, out var stamp)) return null;
        var row = db.Query("SELECT password, account_status FROM app_user WHERE user_id = @p0",
            r => new { Password = r.Str("password"), Status = r.Str("account_status") }, id).FirstOrDefault();
        return row != null && row.Status == "Active" && stamp == Stamp(row.Password) ? id : null;
    }

    static bool TryRead(HttpContext context, out int id, out string stamp)
    {
        id = 0;
        stamp = context.User.FindFirstValue(StampClaim);
        return context.User.Identity?.IsAuthenticated == true && stamp != null
            && int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out id);
    }

    public static Task Issue(HttpContext context, UserRow user)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(StampClaim, Stamp(user.Password))], Scheme);
        return context.SignInAsync(Scheme, new ClaimsPrincipal(identity));
    }

    public static Task Clear(HttpContext context) => context.SignOutAsync(Scheme);
}
