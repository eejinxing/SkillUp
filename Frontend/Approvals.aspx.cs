using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend
{
    /// <summary>
    /// Program leader: the Create / Delete requests of the leader's programs as a board or a table, filtered by search,
    /// type, program and submission day (kept for the session). ?id= opens one application with its decision form;
    /// &amp;decision=approve|reject preselects the decision.
    /// </summary>
    public partial class ApprovalsPage : SkillPage
    {
        /// <summary>Board filters, kept in the session like the old per-tab filters.</summary>
        [Serializable]
        protected sealed class BoardFilters
        {
            public string Q = "", Kind = "", Program = "", Date = "", View = "board", Anchor = "";
        }

        static readonly string[][] Lanes = { new[] { "Pending review", "New", "new" }, new[] { "Rejected", "Rejected", "rejected" }, new[] { "Approved", "Approved", "approved" } };
        static readonly string[] Tones = { "#6d5dfc", "#14b8a6", "#f59e0b", "#ec4899", "#3b82f6", "#84cc16" };
        static readonly CultureInfo Gb = CultureInfo.GetCultureInfo("en-GB");
        static readonly TimeZoneInfo Myt = MalaysiaTime();
        static readonly string IdIcon = Icons.Kit("<path d=\"M4 9h16M4 15h16M10 3 8 21M16 3l-2 18\"/>");
        static readonly string CalendarIcon = Icons.Kit("<rect x=\"4\" y=\"5\" width=\"16\" height=\"15\" rx=\"2\"/><path d=\"M4 10h16M9 3v4M15 3v4\"/>");

        public override string Route => "approvals";
        protected override string[] Roles => new[] { "Program Leader" };

        List<RequestRow> all;
        protected BoardFilters Filters = new BoardFilters();
        protected List<RequestRow> Shown = new List<RequestRow>();
        protected RequestRow Current;
        protected string Rail, Month, Results, Content, Timeline;

        string FiltersKey => "SkillUp.Approvals.Filters." + Me.Id;

        static TimeZoneInfo MalaysiaTime()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time"); }
            catch (TimeZoneNotFoundException) { return TimeZoneInfo.CreateCustomTimeZone("MYT", TimeSpan.FromHours(8), "MYT", "MYT"); }
        }

        /// <summary>Database times are the server PC's local time; requests are shown in Malaysia time (MYT).</summary>
        static DateTime InMyt(DateTime local) => TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(local, DateTimeKind.Local), Myt);
        static string DayKey(DateTime local) => InMyt(local).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        static string ShortDate(DateTime? local) => local == null ? "Not recorded" : InMyt(local.Value).ToString("d MMM yyyy", Gb);
        static string ShortTime(DateTime local) => InMyt(local).ToString("HH:mm", Gb);

        protected void Page_Load(object sender, EventArgs e)
        {
            var owned = new HashSet<int>(Data.Programs.Where(p => p.LeaderId == Me.Id).Select(p => p.Id));
            all = Data.Requests.Where(r => r.ProgramId != null && owned.Contains(r.ProgramId.Value)).OrderByDescending(r => r.CreatedAt).ToList();
            Rail = "<nav class=\"la-rail\" aria-label=\"Program Leader navigation\">" + string.Concat(new[]
            {
                new[] { "Dashboard", "Overview", "grid" }, new[] { "Programs", "Programs", "book" }, new[] { "Approvals", "Course approvals", "check" },
                new[] { "Assignments", "Instructors", "users" }, new[] { "Analytics", "Program analytics", "chart" }
            }.Select(l => "<a href=\"" + l[0] + "\" aria-label=\"" + l[1] + "\" title=\"" + l[1] + "\"" + (l[0] == "Approvals" ? " aria-current=\"page\"" : "") + ">" + Icons.App(l[2]) + "</a>")) + "</nav>";
            if (Request.QueryString["id"] != null) ShowApplication(QueryInt("id"));
            else ShowBoard();
        }

        // ---- board ------------------------------------------------------------------------

        void ShowBoard()
        {
            Screens.ActiveViewIndex = 0;
            Filters = Session[FiltersKey] as BoardFilters ?? new BoardFilters();
            if (Filters.Anchor.Length == 0) Filters.Anchor = DayKey(DateTime.Now);
            if (!IsPostBack)
            {
                SearchBox.Text = Filters.Q;
                KindFilter.SelectedValue = Filters.Kind;
                ProgramFilter.Items.Add(new ListItem("All programs", ""));
                foreach (var p in Data.Programs.Where(p => p.LeaderId == Me.Id)) ProgramFilter.Items.Add(new ListItem(p.Name, p.Id.ToString(CultureInfo.InvariantCulture)));
                if (ProgramFilter.Items.FindByValue(Filters.Program) != null) ProgramFilter.SelectedValue = Filters.Program;
                var anchor = DateTime.ParseExact(Filters.Anchor, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                Days.DataSource = Enumerable.Range(0, 12).Select(i => anchor.AddDays(i - 5)).Select(d => new
                {
                    Iso = d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Weekday = d.ToString("ddd", Gb), Day = d.Day,
                    Pressed = Filters.Date == d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ? "true" : "false"
                });
                Days.DataBind();
            }
            Month = DateTime.ParseExact(Filters.Anchor, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString("MMMM yyyy", Gb);
            BoardView.Attributes["aria-pressed"] = Filters.View == "board" ? "true" : "false";
            TableView.Attributes["aria-pressed"] = Filters.View == "table" ? "true" : "false";
            AllDates.Attributes["aria-pressed"] = Filters.Date.Length == 0 ? "true" : "false";

            Shown = all.Where(r => (Filters.Kind.Length == 0 || r.Kind == Filters.Kind)
                && (Filters.Program.Length == 0 || r.ProgramId?.ToString(CultureInfo.InvariantCulture) == Filters.Program)
                && (Filters.Date.Length == 0 || DayKey(r.CreatedAt) == Filters.Date)
                && (r.CourseTitle + " " + r.Reason + " " + r.Id + " " + Data.User(r.RequesterId)?.Name + " " + ProgramName(r)).IndexOf(Filters.Q.Trim(), StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            Results = Filters.View == "table" ? Table(Shown) : Board(Shown);
        }

        string ProgramName(RequestRow r) => r.ProgramId == null ? null : Data.Program(r.ProgramId.Value)?.Name;

        string Badge(RequestRow r)
        {
            var name = ProgramName(r) ?? "Former program";
            var label = name == "Data & Development" ? "D&D"
                : string.Concat(name.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Take(3).Select(w => w[0])).ToUpperInvariant();
            var tone = Tones[(r.ProgramId?.ToString(CultureInfo.InvariantCulture) ?? "").Sum(c => (int)c) % Tones.Length];
            return "<span class=\"la-program\" style=\"--tone:" + tone + "\" title=\"" + Ui.H(name) + "\" aria-label=\"Program: " + Ui.H(name) + "\"><span class=\"la-badge-native\" aria-hidden=\"true\">"
                + "<img src=\"assets/leader-approvals/imgVector.svg\" alt=\"\"></span><b>" + Ui.H(label) + "</b></span>";
        }

        string Card(RequestRow r)
        {
            var who = Data.User(r.RequesterId)?.Name ?? "Former user";
            var pending = r.Status == "Pending review";
            var url = "Approvals?id=" + r.Id;
            var html = new StringBuilder("<article class=\"la-request\" data-request-id=\"" + r.Id + "\"><header>" + Badge(r));
            if (!pending) html.Append("<span class=\"la-state la-state-").Append(r.Status == "Approved" ? "approved" : r.Status == "Rejected" ? "rejected" : "other").Append("\">&#9679; ").Append(Ui.H(r.Status)).Append("</span>");
            html.Append("</header><a class=\"la-card-title\" href=\"").Append(url).Append("\"><h3>").Append(r.Kind == "Delete" ? "Delete course" : "Create / update course").Append("</h3><strong>")
                .Append(Ui.H(r.CourseTitle)).Append("</strong></a><p class=\"la-reference\" title=\"").Append(r.Id).Append("\">").Append(IdIcon).Append(r.Id).Append("</p><p class=\"la-when\">").Append(CalendarIcon)
                .Append("<time datetime=\"").Append(Workspace.Iso(r.CreatedAt)).Append("\">").Append(ShortDate(r.CreatedAt)).Append("</time><span aria-hidden=\"true\">&middot;</span>").Append(ShortTime(r.CreatedAt))
                .Append(" MYT</p><div class=\"la-tags\"><span>").Append(r.Kind == "Delete" ? "Deletion" : "Course creation").Append("</span><span title=\"").Append(Ui.H(who)).Append("\">").Append(Ui.H(who))
                .Append("</span></div><div class=\"la-card-actions\">");
            if (pending)
                html.Append("<a class=\"la-approve\" href=\"").Append(url).Append("&amp;decision=approve\" aria-label=\"Approve ").Append(Ui.H(r.CourseTitle)).Append("\">Approve</a><a class=\"la-reject\" href=\"")
                    .Append(url).Append("&amp;decision=reject\" aria-label=\"Reject ").Append(Ui.H(r.CourseTitle)).Append("\">Reject</a>");
            else html.Append("<a href=\"").Append(url).Append("\">View application &rarr;</a>");
            return html.Append("</div></article>").ToString();
        }

        string Board(List<RequestRow> list)
        {
            var html = new StringBuilder("<div class=\"la-columns\">");
            foreach (var lane in Lanes)
            {
                var items = list.Where(r => r.Status == lane[0]).ToList();
                html.Append("<section class=\"la-column la-").Append(lane[2]).Append("\" data-status=\"").Append(lane[0]).Append("\"><h2><i class=\"la-dot\" aria-hidden=\"true\"></i>").Append(lane[1])
                    .Append(" <span>").Append(items.Count).Append("</span></h2>").Append(items.Count == 0 ? "<p class=\"la-empty\">No applications here.</p>" : string.Concat(items.Select(Card))).Append("</section>");
            }
            html.Append("</div>");
            var others = list.Where(r => !Lanes.Any(l => l[0] == r.Status)).ToList();
            if (others.Count > 0)
                html.Append("<details class=\"la-other\"><summary>Other outcomes &middot; ").Append(others.Count).Append(" <small>Changes requested and withdrawn</small></summary><div>")
                    .Append(string.Concat(others.Select(Card))).Append("</div></details>");
            return html.ToString();
        }

        string Table(List<RequestRow> list)
        {
            var rows = string.Concat(list.Select(r => "<tr data-request-id=\"" + r.Id + "\"><td><strong>" + Ui.H(r.CourseTitle) + "</strong><small>" + (r.Kind == "Delete" ? "Delete" : "Create / update")
                + "</small></td><td>" + Ui.H(ProgramName(r)) + "</td><td>" + Ui.H(Data.User(r.RequesterId)?.Name ?? "Former user") + "</td><td>" + ShortDate(r.CreatedAt) + "</td><td>" + Ui.H(r.Status)
                + "</td><td><a href=\"Approvals?id=" + r.Id + "\">View application &rarr;</a></td></tr>"));
            return "<div class=\"la-table\"><table><thead><tr><th>Course / request</th><th>Program</th><th>Instructor</th><th>Submitted</th><th>Status</th><th>Action</th></tr></thead><tbody>"
                + (rows.Length > 0 ? rows : "<tr><td colspan=\"6\">No matching applications.</td></tr>") + "</tbody></table></div>";
        }

        /// <summary>Copies the search box and drop-downs into the filters, so every board button also applies what was typed.</summary>
        void Capture()
        {
            Filters.Q = SearchBox.Text.Trim();
            Filters.Kind = KindFilter.SelectedValue;
            Filters.Program = ProgramFilter.SelectedValue;
        }

        void Apply()
        {
            Session[FiltersKey] = Filters;
            Go("Approvals");
        }

        protected void Filter_Changed(object sender, EventArgs e)
        {
            Capture();
            Apply();
        }

        protected void View_Command(object sender, CommandEventArgs e)
        {
            Capture();
            Filters.View = e.CommandArgument.ToString() == "table" ? "table" : "board";
            Apply();
        }

        protected void AllDates_Click(object sender, EventArgs e)
        {
            Capture();
            Filters.Date = "";
            Apply();
        }

        protected void Day_Command(object sender, CommandEventArgs e)
        {
            Capture();
            Filters.Date = e.CommandArgument.ToString();
            Apply();
        }

        protected void Shift_Command(object sender, CommandEventArgs e)
        {
            Capture();
            var anchor = DateTime.ParseExact(Filters.Anchor, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            Filters.Anchor = anchor.AddDays(int.Parse(e.CommandArgument.ToString(), CultureInfo.InvariantCulture)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            Apply();
        }

        // ---- one application -------------------------------------------------------------------

        void ShowApplication(int? id)
        {
            Current = all.FirstOrDefault(x => x.Id == id);
            if (Current == null)
            {
                Screens.ActiveViewIndex = 2;
                return;
            }
            Screens.ActiveViewIndex = 1;
            var r = Current;
            var c = Data.Course(r.CourseId);
            Func<int?, string> who = uid => Ui.H(Data.User(uid)?.Name ?? "Former user");
            var learners = Data.Enrollments.Count(x => x.CourseId == r.CourseId);
            var pending = r.Status == "Pending review";

            var html = new StringBuilder("<section class=\"la-detail-panel\"><header>" + Badge(r) + "<h2>" + Ui.H(r.CourseTitle) + "</h2></header><dl>");
            html.Append("<div><dt>Request type</dt><dd>").Append(r.Kind == "Delete" ? "Delete course" : "Create / update course").Append("</dd></div>");
            html.Append("<div><dt>Submitted by</dt><dd>").Append(who(r.RequesterId)).Append("</dd></div>");
            html.Append("<div><dt>Submitted</dt><dd>").Append(ShortDate(r.CreatedAt)).Append(" &middot; ").Append(ShortTime(r.CreatedAt)).Append(" MYT</dd></div>");
            html.Append("<div><dt>Program</dt><dd>").Append(Ui.H(ProgramName(r) ?? "Former program")).Append("</dd></div>");
            html.Append("<div><dt>Request ID</dt><dd>").Append(r.Id).Append("</dd></div></dl>");
            html.Append("<h3>Instructor's request</h3><p class=\"la-copy\">").Append(Ui.H(string.IsNullOrEmpty(r.Reason) ? "No additional reason recorded." : r.Reason)).Append("</p>");
            if (r.Kind == "Delete")
            {
                html.Append("<p class=\"la-impact\">").Append(c != null ? "Approving permanently removes this course and its materials from the demo workspace. The request history is retained."
                    : "The course has been deleted. Its request history is retained.").Append("</p><p>").Append(learners).Append(" enrolled learners</p>");
                if (learners > 0) html.Append("<p class=\"la-warning\">Deletion cannot be approved while learners are enrolled. Reject this request and ask the instructor to hide the course instead.</p>");
            }
            html.Append("</section>");
            if (c == null) html.Append("<section class=\"la-detail-panel\"><p>Course content is no longer available. The recorded application and decisions remain available.</p></section>");
            else
            {
                var lessons = Data.LessonsOf(c.Id);
                var questions = Data.QuizFor(c.Id)?.Questions ?? new List<QuestionRow>();
                html.Append("<section class=\"la-detail-panel\"><h2>Current course content</h2><p class=\"la-copy\">").Append(Ui.H(c.Description)).Append("</p><h3>Learning outcomes</h3><p class=\"la-copy\">")
                    .Append(Ui.H(c.Outcomes)).Append("</p><dl><div><dt>Instructor</dt><dd>").Append(who(c.InstructorId)).Append("</dd></div><div><dt>Difficulty</dt><dd>").Append(Ui.H(c.Difficulty))
                    .Append("</dd></div><div><dt>Deadline</dt><dd>").Append(ShortDate(c.Deadline)).Append("</dd></div><div><dt>Course status</dt><dd>").Append(Ui.H(c.Status)).Append("</dd></div></dl>");
                html.Append("<h3>Learning activities &middot; ").Append(lessons.Count).Append("</h3>");
                foreach (var l in lessons)
                {
                    html.Append("<details class=\"la-activity\"><summary>").Append(Ui.H(l.Title)).Append(" <span>").Append(Ui.H(l.Type)).Append(" &middot; ").Append(l.Minutes).Append(" min</span></summary><p class=\"la-copy\">")
                        .Append(Ui.H(l.Body)).Append("</p>");
                    if (l.Type == "lab") html.Append("<p>Expected answer: ").Append(Ui.H(l.Answer)).Append("</p>");
                    html.Append("</details>");
                }
                if (lessons.Count == 0) html.Append("<p>No learning activities.</p>");
                html.Append("<h3>Assessment &middot; ").Append(questions.Count).Append(" questions</h3>");
                foreach (var q in questions)
                {
                    html.Append("<details class=\"la-activity\"><summary>").Append(Ui.H(q.Text)).Append("</summary><ol>");
                    for (int i = 0; i < q.Options.Count; i++) html.Append("<li>").Append(Ui.H(q.Options[i].Text)).Append(q.Options[i].Correct ? " &#10003; Correct answer" : "").Append("</li>");
                    html.Append("</ol></details>");
                }
                if (questions.Count == 0) html.Append("<p>No quiz questions.</p>");
                html.Append("<h3>Course files</h3>");
                var files = Data.Resources.Where(f => f.CourseId == c.Id).ToList();
                foreach (var f in files)
                {
                    long bytes;
                    long.TryParse(f.Size, NumberStyles.Integer, CultureInfo.InvariantCulture, out bytes);
                    html.Append("<div class=\"la-file\"><span>").Append(Ui.H(f.Name)).Append(" &middot; ").Append(Math.Round(bytes / 1024.0, MidpointRounding.AwayFromZero)).Append(" KB</span><a class=\"button\" href=\"")
                        .Append(Ui.H(f.DataUrl)).Append("\" download=\"").Append(Ui.H(f.Name)).Append("\">Download</a></div>");
                }
                if (files.Count == 0) html.Append("<p>No files uploaded.</p>");
                html.Append("</section>");
            }
            Content = html.ToString();
            Timeline = string.Concat(r.Events.Select(x => "<li><strong>" + Ui.H(x.Action) + "</strong><span>" + who(x.ActorId) + " &middot; " + ShortDate(x.CreatedAt) + " " + ShortTime(x.CreatedAt)
                + "</span><p class=\"la-copy\">" + Ui.H(string.IsNullOrEmpty(x.Comment) ? x.Action : x.Comment) + "</p></li>"));

            DecisionForm.Visible = pending && c != null;
            ReadOnly.Visible = !DecisionForm.Visible;
            StartReview.Visible = !r.Events.Any(x => x.Action == "Review started");
            if (!IsPostBack && DecisionForm.Visible)
            {
                foreach (var choice in r.Kind == "Delete" ? new[] { "Approved", "Rejected" } : new[] { "Approved", "Rejected", "Changes requested" }) Decision.Items.Add(choice);
                Decision.SelectedValue = Request.QueryString["decision"] == "approve" ? "Approved" : "Rejected";
            }
        }

        protected void StartReview_Click(object sender, EventArgs e)
        {
            if (TryRun("courseRequestSeen", new { id = Current.Id.ToString(CultureInfo.InvariantCulture) })) ReloadPage("Review started");
        }

        protected void SubmitDecision_Click(object sender, EventArgs e)
        {
            if (!IsValid) return;
            var done = Current.Kind == "Delete"
                ? TryRun("courseRequestReview", new { id = Current.Id.ToString(CultureInfo.InvariantCulture), decision = Decision.SelectedValue, comment = Feedback.Text })
                : TryRun("review", new { courseId = Current.CourseId.ToString(CultureInfo.InvariantCulture), requestId = Current.Id.ToString(CultureInfo.InvariantCulture), decision = Decision.SelectedValue, comment = Feedback.Text });
            if (done) Go("Approvals?id=" + Current.Id, "Decision submitted");
        }
    }
}
