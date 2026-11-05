using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json.Linq;
using Npgsql;
using SkillUp.Models;
using SkillUp.Services;

namespace SkillUp.Controllers;

/// <summary>
/// Base class of every page controller. Before each action it loads the database snapshot and the signed-in account, and enforces the
/// roles the action's [Page] attribute allows. Saves go through CommandRunner, so every validation and permission rule runs on the
/// server exactly as it did for the old JSON API.
/// </summary>
public abstract class SkillController : Controller
{
    const string FlashKey = "SkillUp.Flash", FormKey = "SkillUp.Form";

    protected SkillContext Ctx { get; private set; }
    protected Snapshot Data => Ctx.Data;
    protected UserRow Me => Ctx.Me;

    protected NpgsqlDataSource Source => HttpContext.RequestServices.GetRequiredService<NpgsqlDataSource>();

    /// <summary>True for the background refreshes js/site.js makes (X-SkillUp-Live): they must not use up a notice meant for the next page.</summary>
    bool IsLive => Request.Headers["X-SkillUp-Live"].Count > 0;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        Snapshot data;
        using (var db = new Db(Source)) data = Snapshot.Load(db);
        Ctx = new SkillContext(data, null, HttpContext);
        Ctx.Me = AuthCookie.Resolve(HttpContext, data);
        HttpContext.Items[SkillContext.Key] = Ctx;

        var page = context.ActionDescriptor.EndpointMetadata.OfType<PageAttribute>().FirstOrDefault();
        if (page != null) Ctx.Page = page;

        if (page?.Roles != null)
        {
            if (Me == null)
            {
                context.Result = Redirect("/Login?return=" + Uri.EscapeDataString(Request.Path + Request.QueryString));
                return;
            }
            var roles = page.RoleList;
            if (roles.Length > 0 && !roles.Contains(Me.Role))
            {
                // A restricted page never reads data that belongs to another role.
                context.Result = View("~/Views/Shared/Restricted.cshtml");
                return;
            }
        }

        if (Request.Method == "GET" && !IsLive)
        {
            Ctx.Notice = TakeSession(FlashKey);
            var form = TakeSession(FormKey);
            if (form != null) Ctx.Posted = JsonSerializer.Deserialize<Dictionary<string, string>>(form);
        }
        base.OnActionExecuting(context);
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);
        RememberPage(context);
    }

    // ---- saving ------------------------------------------------------------------------------

    /// <summary>
    /// Runs one command and refreshes Ctx so the rest of the request sees the change. Throws ApiException when the command refuses.
    /// </summary>
    protected async Task<object> Run(string action, object payload = null)
    {
        var body = payload as JObject ?? (payload == null ? new JObject() : JObject.FromObject(payload));
        var outcome = await CommandRunner.Run(HttpContext, Data, Me, action, body);
        Ctx.Data = outcome.Snapshot;
        Ctx.Me = outcome.Me;
        return outcome.Data;
    }

    /// <summary>
    /// Runs a command; when it is refused the message is kept as a notice and the typed form values are remembered, ready for
    /// the redirect back to the form. Returns null in that case (check Failed).
    /// </summary>
    protected async Task<CommandResult> TryRun(string action, object payload = null)
    {
        try
        {
            return new CommandResult(true, await Run(action, payload), null);
        }
        catch (ApiException e)
        {
            return Refused(e.Message);
        }
        catch (PostgresException e) when (Db.IsDuplicate(e))
        {
            return Refused("That record already exists.");
        }
    }

    CommandResult Refused(string message)
    {
        Flash(message);
        // Passwords are never kept.
        if (Request.HasFormContentType)
        {
            var typed = Request.Form.Where(x => !x.Key.StartsWith("__") && !x.Key.Contains("password", StringComparison.OrdinalIgnoreCase) && !x.Key.Contains("confirm", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(x => x.Key, x => x.Value.ToString());
            HttpContext.Session.SetString(FormKey, JsonSerializer.Serialize(typed));
        }
        return new CommandResult(false, null, message);
    }

    /// <summary>Keeps a message for the next page this browser opens.</summary>
    protected void Flash(string message) => HttpContext.Session.SetString(FlashKey, message);

    string TakeSession(string key)
    {
        var value = HttpContext.Session.GetString(key);
        if (value != null) HttpContext.Session.Remove(key);
        return value;
    }

    /// <summary>Post/redirect/get: send the browser back to the page it posted from so a refresh never repeats the save.</summary>
    protected IActionResult Back(string message = null, string fallback = "/")
    {
        if (message != null) Flash(message);
        return Redirect(SafeLocal(Request.Headers.Referer.ToString(), fallback));
    }

    /// <summary>Redirect to a page of this site, optionally with a notice.</summary>
    protected IActionResult Go(string url, string message = null)
    {
        if (message != null) Flash(message);
        return Redirect(url);
    }

    /// <summary>Only addresses on this site: an absolute address from a header or a "return" field is cut down to its path.</summary>
    protected string SafeLocal(string url, string fallback = "/")
    {
        if (string.IsNullOrWhiteSpace(url)) return fallback;
        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
            return string.Equals(absolute.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase) ? absolute.PathAndQuery : fallback;
        return url.StartsWith('/') && !url.StartsWith("//") && !url.StartsWith("/\\") ? url : fallback;
    }

    /// <summary>
    /// Sends rows as a CSV download (UTF-8 with a byte order mark, so Excel reads accents). Values starting with = + @ or - get a
    /// leading apostrophe so a spreadsheet never runs them as formulas.
    /// </summary>
    protected FileContentResult Csv(string fileName, IEnumerable<IEnumerable<object>> rows)
    {
        var csv = string.Join("\r\n", rows.Select(row => string.Join(",", row.Select(value =>
        {
            var text = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "";
            if (text.Length > 0 && "=+@-".Contains(text[0])) text = "'" + text;
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }))));
        var utf8 = new UTF8Encoding(true);
        return File([.. utf8.GetPreamble(), .. utf8.GetBytes(csv)], "text/csv", fileName);
    }

    // ---- where the visitor came from ---------------------------------------------------------
    // Back links return to the page that was clicked, not always to a fixed list. Each page opened by address (a GET, not a save)
    // is remembered in the session: the page now open ("Current") and the one before it with a different path ("Previous").

        void RememberPage(ActionExecutedContext context)
    {
        if (Me == null || Request.Method != "GET" || IsLive || context.Result is not ViewResult) return;
        var path = Request.Path.Value ?? "";
        var current = Ctx.ReadVisit(SkillContext.NavCurrent);
        // Moving between pages of the same group (the course editor and its question editor) keeps the page the group was opened from.
        var group = Ctx.Page.Group ?? Ctx.Page.Route;
        if (current != null && !string.Equals(current.Path, path, StringComparison.OrdinalIgnoreCase) && current.UserId == Me.Id && current.Group != group)
            HttpContext.Session.SetString(SkillContext.NavPrevious, JsonSerializer.Serialize(current));
        HttpContext.Session.SetString(SkillContext.NavCurrent, JsonSerializer.Serialize(new NavVisit(path, path + Request.QueryString, ViewData["Title"] as string ?? "", Me.Id, group)));
    }
}

/// <summary>The outcome of SkillController.TryRun.</summary>
public sealed record CommandResult(bool Ok, object Data, string Error)
{
    public bool Failed => !Ok;
}
