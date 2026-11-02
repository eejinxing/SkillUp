using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Security;

namespace SKILL_UP.Backend
{
    /// <summary>An error the API reports to the browser as { "detail": message } with an HTTP status.</summary>
    public sealed class ApiException : Exception
    {
        public int Status { get; }

        public ApiException(int status, string message) : base(message)
        {
            Status = status;
        }
    }

    /// <summary>appSettings in Web.config. Web.Release.config turns the demo switches off.</summary>
    static class Settings
    {
        static bool Flag(string key, bool fallback)
        {
            bool value;
            return bool.TryParse(ConfigurationManager.AppSettings[key], out value) ? value : fallback;
        }

        /// <summary>Allows the "Sign in with a demo account" buttons (no password). Never enable in production.</summary>
        public static bool DemoLogin => Flag("SkillUp:DemoLogin", false);

        /// <summary>Loads Database/demo-seed.json into an empty database on first start.</summary>
        public static bool SeedDemoData => Flag("SkillUp:SeedDemoData", false);

        /// <summary>There is no email service yet, so the reset link is shown on screen instead of emailed.</summary>
        public static bool ShowResetLink => Flag("SkillUp:ShowResetLink", false);
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
            int iterations;
            return parts.Length != 4 || parts[0] != "PBKDF2" || !int.TryParse(parts[1], out iterations) || iterations < Iterations;
        }

        /// <summary>Null when the password is acceptable, otherwise the message to show.</summary>
        public static string Problem(string password)
        {
            password = password ?? "";
            if (password.Length < 8 || password.Length > 128) return "Use 8 to 128 characters for the password.";
            if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit)) return "The password needs at least one letter and one number.";
            return null;
        }

        /// <summary>Stored as PBKDF2$iterations$salt$hash (SHA-256, base64).</summary>
        public static string Hash(string password)
        {
            var salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            return "PBKDF2$" + Iterations + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(Derive(password, salt, Iterations));
        }

        public static bool Verify(string password, string stored)
        {
            var parts = (stored ?? "").Split('$');
            int iterations;
            if (parts.Length != 4 || parts[0] != "PBKDF2" || !int.TryParse(parts[1], out iterations)) return false;
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
            var actual = Derive(password ?? "", salt, iterations);
            int difference = actual.Length ^ expected.Length;
            for (int i = 0; i < Math.Min(actual.Length, expected.Length); i++) difference |= actual[i] ^ expected[i];
            return difference == 0;
        }

        static byte[] Derive(string password, byte[] salt, int iterations)
        {
            using (var kdf = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256)) return kdf.GetBytes(32);
        }
    }

    /// <summary>
    /// Slows down guessing: after too many failures a key (an account name, an address) is locked for a while. Kept in memory,
    /// which fits the single IIS Express process; a restart clears it.
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
        static readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();

        /// <summary>Throws 429 while any of the keys is locked.</summary>
        public static void Check(params string[] keys)
        {
            lock (entries)
            {
                foreach (var key in keys)
                {
                    Entry e;
                    if (entries.TryGetValue(key, out e) && e.LockedUntil > DateTime.UtcNow)
                        throw new ApiException(429, "Too many attempts. Try again in " + Math.Max(1, (int)Math.Ceiling((e.LockedUntil - DateTime.UtcNow).TotalMinutes)) + " minutes.");
                }
            }
        }

        /// <summary>Counts one failure; the key locks for 15 minutes once it reaches the limit.</summary>
        public static void Fail(string key, int limit)
        {
            lock (entries)
            {
                var now = DateTime.UtcNow;
                if (entries.Count > 5000) foreach (var stale in entries.Where(x => now - x.Value.First > Window && x.Value.LockedUntil < now).Select(x => x.Key).ToList()) entries.Remove(stale);
                Entry e;
                if (!entries.TryGetValue(key, out e) || now - e.First > Window && e.LockedUntil < now) entries[key] = e = new Entry();
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
        public static string New()
        {
            var bytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        public static string Sha256(string value)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""))).Replace("-", "").ToLowerInvariant();
        }
    }

    /// <summary>
    /// Sign-in cookie: an encrypted, HttpOnly forms-authentication ticket that holds the user ID.
    /// The account is reloaded on every request, so a deactivated account loses access at once.
    /// </summary>
    /// <summary>
    /// A random key that lets the database recognise this browser for "My enquiries". ENQUIRY_ACCESS stores only its
    /// SHA-256 hash. HttpOnly, so page scripts cannot read it; nothing else about the visitor is kept in the browser.
    /// </summary>
    static class VisitorCookie
    {
        const string Name = "SkillUp.Visitor";

        public static string Read(HttpContext context)
        {
            var value = context.Request.Cookies[Name]?.Value;
            return string.IsNullOrEmpty(value) || value.Length > 64 ? null : value;
        }

        public static void Issue(HttpContext context, string key)
        {
            context.Response.Cookies.Add(new HttpCookie(Name, key)
            {
                HttpOnly = true,
                Secure = context.Request.IsSecureConnection,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = DateTime.Now.AddYears(1)
            });
        }
    }

    static class AuthCookie
    {
        const string Name = "SkillUp.Auth";

        static FormsAuthenticationTicket Ticket(HttpContext context)
        {
            var cookie = context.Request.Cookies[Name];
            if (string.IsNullOrEmpty(cookie?.Value)) return null;
            try
            {
                var ticket = FormsAuthentication.Decrypt(cookie.Value);
                int id;
                return ticket == null || ticket.Expired || !int.TryParse(ticket.Name, out id) ? null : ticket;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>The user ID in the cookie. It does not check the account: use Resolve for that.</summary>
        public static int? Read(HttpContext context)
        {
            var ticket = Ticket(context);
            return ticket == null ? (int?)null : int.Parse(ticket.Name);
        }

        /// <summary>Ties a sign-in to the current password, so changing or resetting it ends every other session.</summary>
        static string Stamp(UserRow user) => Tokens.Sha256(user.Password).Substring(0, 16);

        /// <summary>The signed-in, still active account whose password has not changed since the cookie was issued, or null.</summary>
        public static UserRow Resolve(HttpContext context, Model model)
        {
            var ticket = Ticket(context);
            if (ticket == null) return null;
            var user = model.User(int.Parse(ticket.Name));
            if (user != null && user.Status == "Active" && ticket.UserData == Stamp(user)) return user;
            Clear(context);
            return null;
        }

        /// <summary>Same check for callers that have no Model loaded.</summary>
        public static int? Resolve(HttpContext context, Db db)
        {
            var ticket = Ticket(context);
            if (ticket == null) return null;
            var id = int.Parse(ticket.Name);
            var row = db.Query("SELECT password, account_status FROM [USER] WHERE user_id = @p0", r => new { Password = r.Str("password"), Status = r.Str("account_status") }, id).FirstOrDefault();
            if (row != null && row.Status == "Active" && ticket.UserData == Tokens.Sha256(row.Password).Substring(0, 16)) return id;
            return null;
        }

        public static void Issue(HttpContext context, UserRow user)
        {
            var ticket = new FormsAuthenticationTicket(1, user.Id.ToString(), DateTime.Now, DateTime.Now.AddHours(12), false, Stamp(user));
            context.Response.Cookies.Add(new HttpCookie(Name, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true,
                Secure = context.Request.IsSecureConnection,
                SameSite = SameSiteMode.Lax,
                Path = "/"
            });
        }

        public static void Clear(HttpContext context)
        {
            context.Response.Cookies.Add(new HttpCookie(Name, "")
            {
                HttpOnly = true,
                Secure = context.Request.IsSecureConnection,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = DateTime.UtcNow.AddDays(-1)
            });
        }
    }
}
