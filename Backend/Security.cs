using System;
using System.Configuration;
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
        const int Iterations = 100000;

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

        public static int? Read(HttpContext context)
        {
            var cookie = context.Request.Cookies[Name];
            if (string.IsNullOrEmpty(cookie?.Value)) return null;
            try
            {
                var ticket = FormsAuthentication.Decrypt(cookie.Value);
                int id;
                if (ticket == null || ticket.Expired || !int.TryParse(ticket.Name, out id)) return null;
                return id;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void Issue(HttpContext context, int userId)
        {
            var ticket = new FormsAuthenticationTicket(1, userId.ToString(), DateTime.Now, DateTime.Now.AddHours(12), false, "skillup");
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
