using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace SkillUp.Controllers;

/// <summary>Signing in and out, creating an account, and getting back into one (forgot and reset password).</summary>
public sealed class AccountController : SkillController
{
    // ---- Login / Register --------------------------------------------------------------------
    // Both forms are pop-ups the layout adds to every visitor page. These two pages show the pop-up already open.

    [HttpGet("Login"), Page("login", Roles = null)]
    public IActionResult Login()
    {
        if (Me != null) return Redirect(SafeLocal(Ctx.Query("return"), "/Dashboard"));
        ViewData["Title"] = "Log in";
        return View();
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login(string user, string password, string returnUrl)
    {
        var result = await TryRun("login", new { email = (user ?? "").Trim(), password });
        if (result.Failed) return ReopenDialog("login", PageOnly(Request.Headers.Referer.ToString()).StartsWith("/Login", StringComparison.OrdinalIgnoreCase), returnUrl);
        return Redirect(SafeLocal(returnUrl, "/Dashboard"));
    }

    [HttpGet("Register"), Page("register")]
    public IActionResult Register()
    {
        if (Me != null) return Redirect("/Dashboard");
        ViewData["Title"] = "Create your account";
        return View();
    }

    [HttpPost("Register")]
    public async Task<IActionResult> Register(string name, string username, string email, string role, string password, string confirm, string agree, string fromPage)
    {
        if (agree != "true")
        {
            Flash("Tick the box to accept the terms and the privacy information.");
            return ReopenDialog("register", fromPage == "register");
        }
        var result = await TryRun("register", new { name = (name ?? "").Trim(), username = (username ?? "").Trim(), email = (email ?? "").Trim(), role = role == "Instructor" ? "Instructor" : "Learner", password, confirm });
        if (result.Failed) return ReopenDialog("register", fromPage == "register");
        var pending = (bool?)((JObject)result.Data)?["pending"] == true;
        Flash(pending ? "Account created. Your instructor application is pending administrator approval." : "Account created. Log in to start learning.");
        return fromPage == "register" ? Redirect("/Login") : Redirect(PageOnly(Request.Headers.Referer.ToString()) + "#login");
    }

    /// <summary>A refused sign-in or sign-up comes back to the page it was sent from, with its pop-up open.</summary>
    IActionResult ReopenDialog(string dialog, bool onOwnPage, string returnUrl = null)
    {
        if (!onOwnPage) return Redirect(PageOnly(Request.Headers.Referer.ToString()) + "#" + dialog);
        if (dialog == "register") return Redirect("/Register");
        return Redirect("/Login" + (string.IsNullOrEmpty(returnUrl) || returnUrl == "/Dashboard" ? "" : "?return=" + Uri.EscapeDataString(returnUrl)));
    }

    /// <summary>The page of this site a header points at, without any #fragment.</summary>
    string PageOnly(string url)
    {
        var local = SafeLocal(url, "/Home");
        var hash = local.IndexOf('#');
        return hash < 0 ? local : local[..hash];
    }

    [HttpPost("Logout"), Page("logout", Roles = "*")]
    public async Task<IActionResult> Logout()
    {
        await TryRun("logout");
        return Go("/Home", "You have signed out.");
    }

    // ---- Profile -----------------------------------------------------------------------------

    [HttpGet("Profile"), Page("profile", Roles = "*", Secondary = true)]
    public IActionResult Profile()
    {
        var model = new ProfileModel(Ctx);
        model.Load();
        ViewData["Title"] = model.PageTitle ?? "Profile";
        // A refused password change opens the password section again, so its message is in view.
        model.PasswordOpen = Ctx.Posted.ContainsKey("__password");
        return View(model);
    }

    [HttpPost("Profile/Save"), Page("profile", Roles = "*")]
    public async Task<IActionResult> ProfileSave(string name, string username, string email, string location, string bio)
    {
        // Without an avatar field the command keeps the current picture.
        var result = await TryRun("profile", new { name = (name ?? "").Trim(), username = (username ?? "").Trim(), email = (email ?? "").Trim(), location = (location ?? "").Trim(), bio, theme = Me.Theme, language = Me.Language });
        return result.Failed ? Redirect("/Profile") : Go("/Profile", "Profile saved");
    }

    object ProfilePayload(string avatar) => new { name = Me.Name, username = Me.Username, email = Me.Email, location = Me.Location, bio = Me.Bio, theme = Me.Theme, language = Me.Language, avatar };

    [HttpPost("Profile/Avatar"), Page("profile", Roles = "*"), RequestSizeLimit(4 * 1024 * 1024)]
    public async Task<IActionResult> ProfileAvatar(IFormFile file)
    {
        if (file == null || !ProfileModel.AvatarTypes.TryGetValue(Path.GetExtension(file.FileName), out var type)) return Go("/Profile", "Choose a PNG, JPG, WebP or GIF image.");
        if (file.Length > 2 * 1024 * 1024) return Go("/Profile", "Profile images must be smaller than 2 MB.");
        using var memory = new MemoryStream();
        await file.CopyToAsync(memory);
        var result = await TryRun("profile", ProfilePayload("data:" + type + ";base64," + Convert.ToBase64String(memory.ToArray())));
        return result.Failed ? Redirect("/Profile") : Go("/Profile", "Profile image updated");
    }

    [HttpPost("Profile/RemoveAvatar"), Page("profile", Roles = "*")]
    public async Task<IActionResult> ProfileRemoveAvatar()
    {
        var result = await TryRun("profile", ProfilePayload(""));
        return result.Failed ? Redirect("/Profile") : Go("/Profile", "Profile image removed");
    }

    [HttpPost("Profile/Password"), Page("profile", Roles = "*")]
    public async Task<IActionResult> ProfilePassword(string current, string password, string confirm)
    {
        // The command keeps this browser signed in with a fresh cookie and ends the account's other sessions.
        var result = await TryRun("password", new { current, password, confirm });
        if (result.Failed)
        {
            HttpContext.Session.SetString("SkillUp.Form", "{\"__password\":\"1\"}");
            return Redirect("/Profile#pf-password");
        }
        return Go("/Profile", "Password updated. Your other devices have been signed out.");
    }

    [HttpPost("Profile/Feedback"), Page("profile", Roles = "*")]
    public async Task<IActionResult> ProfileFeedback(string rating, string text)
    {
        var result = await TryRun("feedback", new { rating, text = (text ?? "").Trim() });
        return result.Failed ? Redirect("/Profile") : Go("/Profile", "Thanks for your feedback");
    }

    // ---- Forgot / reset password -------------------------------------------------------------

    /// <summary>Which step of the forgot-password page to draw: 0 the email box, 1 the on-screen link (demo), 2 "check your inbox".</summary>
    public sealed record ForgotModel(int Step, string ResetUrl);

    [HttpGet("ForgotPassword"), Page("forgot")]
    public IActionResult ForgotPassword()
    {
        ViewData["Title"] = "Reset your password";
        return View(new ForgotModel(0, null));
    }

    [HttpPost("ForgotPassword"), Page("forgot")]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        ViewData["Title"] = "Reset your password";
        var result = await TryRun("forgot", new { email = (email ?? "").Trim() });
        if (result.Failed)
        {
            Ctx.Notice = result.Error;
            return View(new ForgotModel(0, null));
        }
        var token = (string)((JObject)result.Data)?["token"];
        // No email service yet: in development (SkillUp:ShowResetLink) the link is shown on screen instead.
        return View(token == null ? new ForgotModel(2, null) : new ForgotModel(1, "/ResetPassword?token=" + Uri.EscapeDataString(token)));
    }

    [HttpGet("ResetPassword"), Page("reset")]
    public IActionResult ResetPassword(string token)
    {
        if (string.IsNullOrEmpty(token)) return Redirect("/ForgotPassword");
        ViewData["Title"] = "Choose a new password";
        return View();
    }

    [HttpPost("ResetPassword"), Page("reset")]
    public async Task<IActionResult> ResetPassword(string token, string password, string confirm)
    {
        var result = await TryRun("resetPassword", new { token, password, confirm });
        if (result.Failed) return Redirect("/ResetPassword?token=" + Uri.EscapeDataString(token ?? ""));
        return Go("/Login", "Password updated. Log in with your new password.");
    }
}
