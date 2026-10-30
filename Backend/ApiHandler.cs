using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SKILL_UP.Backend
{
    /// <summary>Registered in App_Start/RouteConfig.cs for every URL under /api/.</summary>
    public sealed class ApiRouteHandler : IRouteHandler
    {
        public IHttpHandler GetHttpHandler(RequestContext requestContext) => new ApiHandler();
    }

    /// <summary>
    /// The HTTP boundary used by Frontend/js/api.js in "http" mode:
    ///   GET  /api/workspace          -> role-scoped workspace JSON
    ///   POST /api/commands/{action}  -> { data, state }
    /// Errors return the HTTP status with { detail }.
    /// </summary>
    public sealed class ApiHandler : IHttpHandler
    {
        /// <summary>The site runs as one IIS Express process, so commands run one at a time and never race.</summary>
        static readonly object CommandGate = new object();

        public bool IsReusable => false;

        public void ProcessRequest(HttpContext context)
        {
            var response = context.Response;
            response.ContentType = "application/json";
            response.ContentEncoding = Encoding.UTF8;
            response.Cache.SetCacheability(HttpCacheability.NoCache);
            response.Cache.SetNoStore();
            response.TrySkipIisCustomErrors = true;
            try
            {
                DatabaseSetup.EnsureReady(context);
                var path = ((context.Request.RequestContext.RouteData.Values["path"] as string) ?? "").Trim('/');
                var method = context.Request.HttpMethod;
                if (path == "workspace" && method == "GET")
                {
                    using (var db = new Db())
                    {
                        var model = Model.Load(db);
                        Write(context, 200, Workspace.Build(model, CurrentUser(context, model)));
                    }
                }
                else if (path.StartsWith("commands/", StringComparison.Ordinal) && method == "POST")
                {
                    // A custom header cannot be sent by a cross-site form or image, so it blocks CSRF.
                    if (context.Request.Headers["X-SkillUp-Request"] != "1") throw new ApiException(403, "This request is missing the Skill UP request header.");
                    Write(context, 200, RunCommand(context, path.Substring("commands/".Length)));
                }
                else if (path.StartsWith("call/", StringComparison.Ordinal) && method == "POST")
                {
                    if (context.Request.Headers["X-SkillUp-Request"] != "1") throw new ApiException(403, "This request is missing the Skill UP request header.");
                    Write(context, 200, CallSignals.Handle(context, path.Substring("call/".Length)));
                }
                else throw new ApiException(404, "Not found.");
            }
            catch (ApiException e)
            {
                Write(context, e.Status, new JObject { ["detail"] = e.Message });
            }
            catch (SqlException e) when (e.Number == 2627 || e.Number == 2601)
            {
                Write(context, 409, new JObject { ["detail"] = "That record already exists." });
            }
            catch (Exception e)
            {
                Trace.TraceError("Skill UP API error: " + e);
                Write(context, 500, new JObject { ["detail"] = "The server could not complete the request. Please try again." });
            }
        }

        static JObject RunCommand(HttpContext context, string action)
        {
            JObject payload;
            using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
            {
                var body = reader.ReadToEnd();
                try { payload = string.IsNullOrWhiteSpace(body) ? new JObject() : JObject.Parse(body); }
                catch (JsonReaderException) { throw new ApiException(400, "The request body is not valid JSON."); }
            }
            lock (CommandGate)
            {
                using (var db = new Db())
                {
                    db.Begin();
                    var model = Model.Load(db);
                    var commands = new Commands(db, model, CurrentUser(context, model)) { VisitorKey = VisitorCookie.Read(context) };
                    var data = commands.Run(action, payload);
                    db.Commit();

                    if (commands.NewVisitorKey != null) VisitorCookie.Issue(context, commands.NewVisitorKey);
                    if (commands.SignIn != null) AuthCookie.Issue(context, commands.SignIn.Value);
                    if (commands.SignOut) AuthCookie.Clear(context);
                    var reloaded = Model.Load(db);
                    var deferred = data as Deferred;
                    if (deferred != null) data = deferred(reloaded);
                    var me = commands.Me == null ? null : reloaded.User(commands.Me.Id);
                    return new JObject
                    {
                        ["data"] = data == null ? JValue.CreateNull() : JToken.FromObject(data),
                        ["state"] = Workspace.Build(reloaded, me)
                    };
                }
            }
        }

        /// <summary>The signed-in, still active account, or null for a visitor.</summary>
        static UserRow CurrentUser(HttpContext context, Model model)
        {
            var id = AuthCookie.Read(context);
            var user = model.User(id);
            if (user != null && user.Status == "Active") return user;
            if (id != null) AuthCookie.Clear(context);
            return null;
        }

        static void Write(HttpContext context, int status, JObject body)
        {
            context.Response.StatusCode = status;
            context.Response.Write(body.ToString(Formatting.None));
        }
    }
}
