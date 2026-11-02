using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;
using Newtonsoft.Json.Linq;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend
{
    /// <summary>Page frame drawn by SkillUp.Master.</summary>
    public enum PageLayout
    {
        /// <summary>Signed-in workspace: .shell wrapper, capsule navigation, main.main[data-view].</summary>
        Workspace,
        /// <summary>Visitor page: capsule navigation, main.public-main and the site footer.</summary>
        Public,
        /// <summary>The landing page: everything inside .site-home.home-refined, with its own footer.</summary>
        Home
    }

    /// <summary>One question to the learning companion and its reply.</summary>
    public sealed class AiPair
    {
        public string Prompt { get; set; }
        public string Response { get; set; }
        public string Date { get; set; }
        public DateTime PromptAt { get; set; }
        public DateTime ResponseAt { get; set; }
    }

    /// <summary>
    /// Base class of every Skill UP page. It loads the database snapshot (Backend.Model) and the signed-in account,
    /// enforces the roles a page allows, and saves changes through Backend.Commands, so every validation and
    /// permission rule runs on the server exactly as it did for the old JSON API.
    /// </summary>
    public abstract class SkillPage : Page
    {
        /// <summary>The site runs as one IIS Express process, so saves run one at a time and never race.</summary>
        static readonly object CommandGate = new object();

        /// <summary>
        /// Text fields accept any characters, such as code samples with &lt;div&gt;. Every page encodes what it prints,
        /// so ASP.NET request validation, which rejects such posts with an error page, is switched off.
        /// </summary>
        protected SkillPage()
        {
            ValidateRequestMode = ValidateRequestMode.Disabled;
        }

        public Model Data { get; private set; }
        public UserRow Me { get; private set; }
        /// <summary>True when the signed-in role may not open this page; the master shows "Access restricted" instead.</summary>
        public bool Restricted { get; private set; }

        /// <summary>The page's name in the navigation, for example "dashboard". It highlights the nav link and becomes main[data-view].</summary>
        public abstract string Route { get; }
        /// <summary>Roles that may open the page. Null: anyone, including visitors. Empty: any signed-in account.</summary>
        protected virtual string[] Roles => null;
        public virtual PageLayout Layout => Me == null ? PageLayout.Public : PageLayout.Workspace;
        /// <summary>Adds figma-secondary to main, which the secondary page styles target.</summary>
        public virtual bool Secondary => false;
        /// <summary>Extra classes for body, used by page-specific styles.</summary>
        public virtual string BodyClass => "";

        /// <summary>The enquiry form near the end of the landing page (Controls/EnquiryForm.ascx).</summary>
        public const string HomeContact = "Home#home-enquiry";
        /// <summary>The "What You'll Learn" section of the landing page, where visitors browse courses instead of the catalogue.</summary>
        public const string HomeCourses = "Home#discover";
        /// <summary>Where Help links go: visitors to the landing page's enquiry form, signed-in accounts to the Help page.</summary>
        public string HelpUrl => Me == null ? HomeContact : "Help";
        /// <summary>
        /// Signing in through the Log in pop-up on this page comes back to it (a course or an activity) instead of going to
        /// the dashboard.
        /// </summary>
        public virtual bool StayAfterSignIn => false;

        protected override void OnPreInit(EventArgs e)
        {
            base.OnPreInit(e);
            DatabaseSetup.EnsureReady(Context);
            using (var db = new Db()) Data = Model.Load(db);
            Me = Active(Data);
            if (Roles != null && Me == null) Response.Redirect("Login?return=" + HttpUtility.UrlEncode(Request.RawUrl), true);
            Restricted = Roles != null && Roles.Length > 0 && !Roles.Contains(Me.Role);
        }

        /// <summary>
        /// Ties the page's view state to the signed-in account, so another site cannot replay a postback as this user
        /// (the sign-in cookie is also SameSite=Lax, so cross-site posts arrive signed out).
        /// </summary>
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            if (Me != null) ViewStateUserKey = "SkillUp.User." + Me.Id;
        }

        /// <summary>A restricted page skips its Page_Load, so it never reads data that belongs to another role.</summary>
        protected override void OnLoad(EventArgs e)
        {
            if (!Restricted) base.OnLoad(e);
        }

        // ---- where the visitor came from ----------------------------------------------------
        // Back links return to the page that was clicked, not always to a fixed list. Each page opened by address (a GET, not a save)
        // is remembered in the session: the page now open ("Current") and the one before it with a different file name ("Previous").

        const string NavCurrent = "SkillUp.Nav.Current", NavPrevious = "SkillUp.Nav.Previous";

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            if (Me == null || Request.HttpMethod != "GET" || Request.Headers["X-SkillUp-Live"] != null || ScriptManager.GetCurrent(this)?.IsInAsyncPostBack == true) return;
            var path = Request.Url.AbsolutePath;
            var current = Session[NavCurrent] as string[];
            // Moving between pages of the same group (the course editor and its question editor) keeps the page the group was opened from.
            if (current != null && !string.Equals(current[0], path, StringComparison.OrdinalIgnoreCase) && current[3] == Me.Id.ToString(CultureInfo.InvariantCulture) && current[4] != NavGroup)
                Session[NavPrevious] = current;
            Session[NavCurrent] = new[] { path, Request.RawUrl, Title ?? "", Me.Id.ToString(CultureInfo.InvariantCulture), NavGroup };
        }

        /// <summary>Pages with the same group are steps of one task, so a back link skips over them. By default each page is its own group.</summary>
        protected virtual string NavGroup => Route;

        /// <summary>
        /// The address and name of the page this one was opened from, for a "back" link. Pages whose file name is in skip are passed
        /// over (a page that is a step inside this one), and when nothing suitable was remembered the fallback is used.
        /// </summary>
        protected void BackTo(string fallbackUrl, string fallbackLabel, out string url, out string label, params string[] skip)
        {
            url = fallbackUrl;
            label = fallbackLabel;
            var previous = Session[NavPrevious] as string[];
            if (previous == null || Me == null || previous[3] != Me.Id.ToString(CultureInfo.InvariantCulture)) return;
            var file = System.IO.Path.GetFileNameWithoutExtension(previous[0]);
            if (string.Equals(file, System.IO.Path.GetFileNameWithoutExtension(Request.Url.AbsolutePath), StringComparison.OrdinalIgnoreCase)) return;
            if (skip.Any(s => string.Equals(s, file, StringComparison.OrdinalIgnoreCase))) return;
            if (new[] { "Login", "Register", "ForgotPassword", "ResetPassword", "Home", "Default" }.Contains(file, StringComparer.OrdinalIgnoreCase)) return;
            // The page's own address, relative to this folder, so it works wherever the site is hosted.
            url = file + (previous[1].IndexOf('?') >= 0 ? previous[1].Substring(previous[1].IndexOf('?')) : "");
            var name = previous[2].Replace("Skill UP · ", "").Trim();
            label = name.Length == 0 ? fallbackLabel : name;
        }

        /// <summary>The signed-in, still active account, or null for a visitor.</summary>
        UserRow Active(Model model)
        {
            return AuthCookie.Resolve(Context, model);
        }

        // ---- saving ------------------------------------------------------------------------

        /// <summary>
        /// Runs one Backend command in a transaction, issues any sign-in or visitor cookie it asks for, and reloads
        /// Data and Me so the rest of the request sees the change. Throws ApiException when the command refuses.
        /// </summary>
        public object Run(string action, object payload = null)
        {
            var body = payload as JObject ?? (payload == null ? new JObject() : JObject.FromObject(payload));
            // Gemini answers in seconds, so ask it before taking the gate that serialises every command.
            var aiReply = action == "ai" ? Companion.Reply(Data, Me, (string)body["text"], Companion.LessonOf(body)) : null;
            lock (CommandGate)
            {
                using (var db = new Db())
                {
                    db.Begin();
                    var commands = new Commands(db, Model.Load(db), Active(Data)) { VisitorKey = VisitorCookie.Read(Context), AiReply = aiReply, ClientIp = Request.UserHostAddress };
                    var data = commands.Run(action, body);
                    db.Commit();
                    if (commands.NewVisitorKey != null) VisitorCookie.Issue(Context, commands.NewVisitorKey);
                    if (commands.SignOut) AuthCookie.Clear(Context);
                    Data = Model.Load(db);
                    if (commands.SignIn != null) AuthCookie.Issue(Context, Data.User(commands.SignIn.Value));
                    Me = commands.SignOut ? null : Data.User(commands.SignIn ?? commands.Me?.Id);
                    var deferred = data as Deferred;
                    return deferred != null ? deferred(Data) : data;
                }
            }
        }

        /// <summary>
        /// Runs a command and shows its message on the page when it is refused. Returns false in that case.
        /// showError puts the message somewhere else, such as inside the Log in pop-up.
        /// </summary>
        public bool TryRun(string action, object payload, out object data, Action<string> showError = null)
        {
            data = null;
            var show = showError ?? ShowError;
            try
            {
                data = Run(action, payload);
                return true;
            }
            catch (ApiException e)
            {
                show(e.Message);
            }
            catch (SqlException e) when (e.Number == 2627 || e.Number == 2601)
            {
                show("That record already exists.");
            }
            return false;
        }

        public bool TryRun(string action, object payload = null)
        {
            object ignored;
            return TryRun(action, payload, out ignored);
        }

        /// <summary>Shows a message on the page being rendered now.</summary>
        public virtual void ShowError(string message) => ((SkillUpMaster)Master).Notice = message;

        /// <summary>Keeps a message for the next page this browser opens, for use before a redirect.</summary>
        public void Flash(string message) => Session["SkillUp.Flash"] = message;

        /// <summary>Post/redirect/get: reload the current address so a refresh never repeats the save.</summary>
        public void ReloadPage(string message = null)
        {
            if (message != null) Flash(message);
            Response.Redirect(Request.RawUrl, true);
        }

        public void Go(string url, string message = null)
        {
            if (message != null) Flash(message);
            Response.Redirect(url, true);
        }

        /// <summary>
        /// Sends rows as a CSV download (UTF-8 with a byte order mark, so Excel reads accents) and ends the response.
        /// Values starting with = + @ or - get a leading apostrophe so a spreadsheet never runs them as formulas.
        /// </summary>
        public void SendCsv(string fileName, IEnumerable<IEnumerable<object>> rows)
        {
            var csv = string.Join("\r\n", rows.Select(row => string.Join(",", row.Select(value =>
            {
                var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
                if (text.Length > 0 && "=+@-".IndexOf(text[0]) >= 0) text = "'" + text;
                return "\"" + text.Replace("\"", "\"\"") + "\"";
            }))));
            Response.Clear();
            Response.ContentType = "text/csv";
            Response.AddHeader("Content-Disposition", "attachment; filename=\"" + fileName + "\"");
            Response.ContentEncoding = new System.Text.UTF8Encoding(true);
            Response.BinaryWrite(new System.Text.UTF8Encoding(true).GetPreamble());
            Response.Write(csv);
            Response.End();
        }

        // ---- reading -----------------------------------------------------------------------

        protected int? QueryInt(string key)
        {
            int value;
            return int.TryParse(Request.QueryString[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : (int?)null;
        }

        public bool IsStaff => Workspace.IsStaff(Me);

        HashSet<int> managed;
        /// <summary>Courses the signed-in account may edit (assigned instructor, program leader, or any for Admin).</summary>
        public HashSet<int> Managed => managed ?? (managed = Workspace.ManagedCourses(Data, Me));

        /// <summary>Courses this staff account looks after (Admin: all), in course order.</summary>
        public List<CourseRow> OwnCourses => Data.Courses.Where(c => Managed.Contains(c.Id)).ToList();

        /// <summary>Courses whose materials this account may open: enrolled ones for a learner, managed ones for staff.</summary>
        public List<CourseRow> AccessibleCourses => Me?.Role == "Learner"
            ? Data.Courses.Where(c => MyEnrollments.Any(e => e.CourseId == c.Id)).ToList()
            : OwnCourses;

        /// <summary>Id/Title pairs for a course drop-down list (data binding needs properties; the row classes use fields).</summary>
        public static IEnumerable<object> CourseOptions(IEnumerable<CourseRow> courses) => courses.Select(c => new { c.Id, c.Title }).ToList();

        /// <summary>The signed-in learner's enrolments.</summary>
        public List<EnrollmentRow> MyEnrollments => Me == null ? new List<EnrollmentRow>() : Data.Enrollments.Where(e => e.UserId == Me.Id).ToList();

        /// <summary>Courses this viewer may see: published ones, plus those they manage or are enrolled in.</summary>
        public IEnumerable<CourseRow> VisibleCourses
        {
            get
            {
                var enrolled = new HashSet<int>(MyEnrollments.Select(e => e.CourseId));
                return Data.Courses.Where(c => c.Status == "Published" || Managed.Contains(c.Id) || enrolled.Contains(c.Id));
            }
        }

        List<int> visibleOrder;

        /// <summary>The course's uploaded artwork, or one of the four stock covers chosen by its place in the course list.</summary>
        public string CourseArt(CourseRow course)
        {
            if (!string.IsNullOrEmpty(course.Thumbnail)) return course.Thumbnail;
            var order = visibleOrder ?? (visibleOrder = VisibleCourses.Select(c => c.Id).ToList());
            return "assets/my-courses/computer-essentials-wide-0" + (Math.Max(0, order.IndexOf(course.Id)) % 4 + 1) + ".webp";
        }

        /// <summary>Percentage of a course's activities this learner has completed.</summary>
        public int Progress(int userId, int courseId)
        {
            var total = Data.LessonsOf(courseId).Count;
            return total == 0 ? 0 : (int)Math.Round(100.0 * Data.CompletedLessons(userId, courseId).Count / total);
        }

        /// <summary>
        /// A message time for the companion chat: the time today, the day and time before that. js/site.js rewrites it in the viewer's own
        /// clock and style; this text is what shows without it.
        /// </summary>
        public static string ChatTime(object when)
        {
            var t = (DateTime)when;
            var gb = System.Globalization.CultureInfo.GetCultureInfo("en-GB");
            return "<time class=\"ai-time\" datetime=\"" + t.ToString("s", System.Globalization.CultureInfo.InvariantCulture) + "\" title=\"" + t.ToString("d MMM yyyy, HH:mm:ss", gb) + "\">"
                + (t.Date == DateTime.Today ? t.ToString("HH:mm", gb) : t.ToString("d MMM, HH:mm", gb)) + "</time>";
        }

        /// <summary>
        /// The account's conversation with the learning companion, oldest first: the one for an activity (lessonId), or the general
        /// one on the AI Assistant page. AI_CHAT_MESSAGE rows come in pairs: the prompt, then the reply.
        /// </summary>
        public List<AiPair> AiPairs(int? lessonId = null)
        {
            var pairs = new List<AiPair>();
            AiMessageRow prompt = null;
            foreach (var row in Data.AiMessages.Where(x => x.UserId == Me.Id && x.LessonId == lessonId))
            {
                if (row.Sender == "User") { prompt = row; continue; }
                if (prompt == null) continue;
                pairs.Add(new AiPair { Prompt = prompt.Content, Response = row.Content, Date = Ui.Date(prompt.CreatedAt), PromptAt = prompt.CreatedAt, ResponseAt = row.CreatedAt });
                prompt = null;
            }
            return pairs;
        }

        /// <summary>Rows of a learners table (course editor, student performance): person, course, completion and last activity.</summary>
        public List<object> LearnerLines(IEnumerable<EnrollmentRow> enrollments) => enrollments.Select(e =>
        {
            var u = Data.User(e.UserId);
            var p = Progress(e.UserId, e.CourseId);
            return (object)new
            {
                e.UserId, e.CourseId, Avatar = Ui.Avatar(u), Name = u?.Name ?? "Unknown learner", Email = u?.Email ?? "", Course = Data.Course(e.CourseId)?.Title ?? "",
                Progress = p, StatusKey = p == 100 ? "done" : p > 0 ? "progress" : "idle", Status = p == 100 ? "Completed" : p > 0 ? "In progress" : "Not started",
                LastActive = Ui.Date(e.LastActive ?? e.EnrolledAt)
            };
        }).ToList();
    }
}
