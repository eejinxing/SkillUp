using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using SkillUp.Services;

namespace SkillUp.Controllers;

/// <summary>
/// The small JSON endpoints js/site.js calls in the background (quiz answers, chat, the lesson companion, call signalling).
/// Every save still goes through CommandRunner, so the same rules apply as for the pages' own forms.
/// </summary>
[ApiController, IgnoreAntiforgeryToken]
public sealed class ApiController : Controller
{
    /// <summary>Runs one command. The custom header keeps other sites from posting here with the visitor's cookie.</summary>
    [HttpPost("api/commands/{name}")]
    public async Task<IActionResult> Command(string name)
    {
        try
        {
            if (!Request.Headers.ContainsKey("X-SkillUp-Request")) throw new ApiException(400, "That request was not recognised.");
            var body = await ReadBody();
            var source = HttpContext.RequestServices.GetRequiredService<NpgsqlDataSource>();
            Snapshot data;
            using (var db = new Db(source)) data = Snapshot.Load(db);
            var me = AuthCookie.Resolve(HttpContext, data);
            var outcome = await CommandRunner.Run(HttpContext, data, me, name, body);
            return Content(JsonConvert.SerializeObject(new { data = outcome.Data }), "application/json");
        }
        catch (ApiException e) { return Failure(e.Status, e.Message); }
        catch (PostgresException e) when (Db.IsDuplicate(e)) { return Failure(409, "That record already exists."); }
    }

    /// <summary>Call signalling (send / poll).</summary>
    [HttpPost("api/call/{op}"), HttpGet("api/call/{op}")]
    public async Task<IActionResult> Call(string op)
    {
        try
        {
            if (!Request.Headers.ContainsKey("X-SkillUp-Request") && Request.Method == "POST") throw new ApiException(400, "That request was not recognised.");
            var result = await CallSignals.Handle(HttpContext, op);
            return Content(result.ToString(Formatting.None), "application/json");
        }
        catch (ApiException e) { return Failure(e.Status, e.Message); }
    }

    async Task<JObject> ReadBody()
    {
        using var reader = new StreamReader(Request.Body);
        var text = await reader.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(text)) return new JObject();
        try { return JObject.Parse(text); }
        catch (JsonReaderException) { throw new ApiException(400, "The request body is not valid JSON."); }
    }

    IActionResult Failure(int status, string message) =>
        new ContentResult { StatusCode = status, ContentType = "application/json", Content = JsonConvert.SerializeObject(new { detail = message }) };
}
