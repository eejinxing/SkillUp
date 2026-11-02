using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend
{
    /// <summary>
    /// Learners of the courses this account looks after. Instructors get the student performance cards (?q=, ?course=,
    /// ?state=, ?sort=); program leaders get the learners table (?q=, ?course=, ?status=, ?more=1).
    /// ?learner=&amp;course= opens one learner's record in one course.
    /// </summary>
    public partial class LearnersPage : SkillPage
    {
        const string CollapsedKey = "SkillUp.LearnerCards.Collapsed";

        sealed class Record
        {
            public EnrollmentRow E;
            public CourseRow C;
            public UserRow U;
            public List<LessonRow> Lessons;
            public HashSet<int> Completed;
            public int Completion, QuizCount;
            public int? Average;
        }

        public override string Route => "learners";
        protected override string[] Roles => new[] { "Instructor", "Program Leader" };

        List<CourseRow> own;
        List<EnrollmentRow> scope;
        protected string LearnerName, ScopedTitle, RosterNote, RosterCount, Rail, Stats, Status = "all";
        protected CourseRow DetailCourse;
        protected EnrollmentRow DetailEnrolment;

        /// <summary>
        /// The learners search box finds a learner by user ID only: a user ID is unique, so it names exactly one learner. "14", "#14" and
        /// "ID 14" all find learner 14; an empty box shows everyone, and anything that is not an ID finds no one. (The course filter beside
        /// the box is separate and still narrows the list by course.)
        /// </summary>
        static bool IdMatches(string term, int userId)
        {
            term = (term ?? "").Trim();
            if (term.Length == 0) return true;
            if (term.StartsWith("#")) term = term.Substring(1);
            else if (term.StartsWith("id", StringComparison.OrdinalIgnoreCase)) term = term.Substring(2);
            int wanted;
            return int.TryParse(term.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out wanted) && wanted == userId;
        }

        /// <summary>
        /// The back link of a learner's detail page: the learners list it was opened from (with its search and filters), or else the page
        /// it was opened from, such as the course editor's Learners tab or the Overview.
        /// </summary>
        protected string BackUrl
        {
            get { string url, label; Back(out url, out label); return url; }
        }

        protected string BackLabel
        {
            get { string url, label; Back(out url, out label); return label; }
        }

        void Back(out string url, out string label)
        {
            var from = Request.UrlReferrer;
            if (from != null && string.Equals(from.Host, Request.Url.Host, StringComparison.OrdinalIgnoreCase)
                && string.Equals(System.IO.Path.GetFileNameWithoutExtension(from.AbsolutePath), "Learners", StringComparison.OrdinalIgnoreCase) && from.Query.IndexOf("learner=", StringComparison.Ordinal) < 0)
            {
                url = "Learners" + from.Query;
                label = "Learners";
                return;
            }
            BackTo("Learners", "Learners", out url, out label, "Learners", "Activity");
        }
        protected bool Expanded;
        List<Record> roster;

        protected void Page_Load(object sender, EventArgs e)
        {
            own = OwnCourses;
            var ids = new HashSet<int>(own.Select(c => c.Id));
            scope = Data.Enrollments.Where(x => ids.Contains(x.CourseId)).ToList();
            var learner = QueryInt("learner");
            if (learner != null) ShowLearner(learner.Value, QueryInt("course"));
            else if (Me.Role == "Instructor") ShowRoster();
            else ShowTable();
        }

        // ---- one learner ----------------------------------------------------------------------

        void ShowLearner(int userId, int? courseId)
        {
            DetailEnrolment = scope.FirstOrDefault(x => x.UserId == userId && (courseId == null || x.CourseId == courseId));
            if (DetailEnrolment == null)
            {
                Screens.ActiveViewIndex = 3;
                return;
            }
            Screens.ActiveViewIndex = 0;
            var cid = DetailEnrolment.CourseId;
            DetailCourse = Data.Course(cid);
            LearnerName = Data.User(userId)?.Name ?? "Former user";
            var lessons = Data.LessonsOf(cid);
            var completed = new HashSet<int>(Data.CompletedLessons(userId, cid));
            var attempts = Insights.Attempts(this).Where(a => a.UserId == userId && a.CourseId == cid).ToList();
            Metrics.DataSource = new[]
            {
                new { Label = "Completion", Value = Progress(userId, cid) + "%", Note = completed.Count + " activities completed" },
                new { Label = "Quiz average", Value = Charts.Avg(attempts.Where(a => a.Type == "quiz").Select(a => a.Score)) + "%", Note = "Across submitted quizzes" },
                new { Label = "Learning time", Value = lessons.Where(l => completed.Contains(l.Id)).Sum(l => l.Minutes) + "m", Note = "Completed activity time" },
                new { Label = "Attempts", Value = attempts.Count.ToString(CultureInfo.InvariantCulture), Note = "Labs and quizzes" }
            };
            Metrics.DataBind();
            LessonStates.DataSource = lessons.Select(l => new { l.Title, l.Type, State = completed.Contains(l.Id) ? "Completed" : "Incomplete" });
            LessonStates.DataBind();
            var topics = attempts.SelectMany(a => a.WrongTopics).GroupBy(t => t)
                .Select(g => new { Topic = g.Key.Length == 0 ? "General" : g.Key, Count = g.Count() }).OrderByDescending(t => t.Count).ToList();
            Topics.DataSource = topics;
            Topics.DataBind();
            NoTopics.Visible = topics.Count == 0;
            TypeCounts.DataSource = new[] { "article", "lab", "quiz" }.Select(t => new { Type = t, Done = lessons.Count(l => l.Type == t && completed.Contains(l.Id)), Total = lessons.Count(l => l.Type == t) });
            TypeCounts.DataBind();
            AttemptRows.DataSource = attempts.Select(a => new { a.Type, a.Score, Date = Ui.Date(a.At) });
            AttemptRows.DataBind();
            AttemptRows.Visible = attempts.Count > 0;
            NoAttempts.Visible = attempts.Count == 0;
        }

        // ---- instructor: student performance cards -----------------------------------------------

        void ShowRoster()
        {
            Screens.ActiveViewIndex = 1;
            Rail = string.Concat(new[]
            {
                new[] { "Dashboard", "Overview", "grid" }, new[] { "Courses", "My courses", "book" }, new[] { "CourseRequests", "My requests", "check" },
                new[] { "Learners", "Student performance", "users" }, new[] { "Analytics", "Course analytics", "chart" }
            }.Select(l => "<a href=\"" + l[0] + "\" aria-label=\"" + l[1] + "\" title=\"" + l[1] + "\"" + (l[0] == "Learners" ? " aria-current=\"page\"" : "") + ">" + Icons.App(l[2]) + "</a>"));

            var course = QueryInt("course");
            var term = (Request.QueryString["q"] ?? "").Trim();
            var state = Request.QueryString["state"] ?? "";
            var sort = Request.QueryString["sort"] ?? "name";
            if (!IsPostBack)
            {
                RosterSearch.Text = term;
                RosterCourse.Items.Add(new ListItem("All assigned courses", ""));
                foreach (var c in own) RosterCourse.Items.Add(new ListItem(c.Title, c.Id.ToString(CultureInfo.InvariantCulture)));
                Select(RosterCourse, course?.ToString(CultureInfo.InvariantCulture));
                Select(RosterState, state);
                Select(RosterSort, sort);
            }
            RosterNote = course == null ? "Progress and quiz results across your assigned courses." : "Course: " + (own.FirstOrDefault(c => c.Id == course)?.Title ?? "Unavailable");

            var quizzes = Insights.Attempts(this).Where(a => a.Type == "quiz").ToList();
            var records = scope.Select(x => new Record { E = x, C = Data.Course(x.CourseId), U = Data.User(x.UserId) }).Where(r => r.C != null && r.U != null).ToList();
            foreach (var r in records)
            {
                r.Lessons = Data.LessonsOf(r.C.Id);
                r.Completed = new HashSet<int>(Data.CompletedLessons(r.U.Id, r.C.Id));
                r.Completion = Progress(r.U.Id, r.C.Id);
                var scores = quizzes.Where(a => a.UserId == r.U.Id && a.CourseId == r.C.Id).Select(a => a.Score).ToList();
                r.QuizCount = scores.Count;
                r.Average = scores.Count == 0 ? (int?)null : Charts.Avg(scores);
            }
            var shown = records.Where(r => (course == null || r.C.Id == course)
                && IdMatches(term, r.U.Id)
                && (state == "" || state == "completed" && r.Completion == 100 || state == "progress" && r.Completion > 0 && r.Completion < 100
                    || state == "not-started" && r.Completion == 0 || state == "needs-support" && r.Average < 70));
            var byName = StringComparer.CurrentCultureIgnoreCase;
            roster = (sort == "completion" ? shown.OrderByDescending(r => r.Completion).ThenBy(r => r.U.Name, byName)
                : sort == "recent" ? shown.OrderByDescending(r => r.E.LastActive ?? r.E.EnrolledAt)
                : shown.OrderBy(r => r.U.Name, byName)).ToList();
            RosterCount = Ui.Plural(roster.Count, "enrolment") + " &middot; " + roster.Select(r => r.U.Id).Distinct().Count() + " learners";
        }

        static void Select(ListControl list, string value)
        {
            if (value != null && list.Items.FindByValue(value) != null) list.SelectedValue = value;
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            if (roster == null) return;
            // Bound after the click events, so the cards always show the saved state; their artwork is not kept in view state.
            var collapsed = Session[CollapsedKey] as HashSet<int> ?? new HashSet<int>();
            Cards.DataSource = roster.Select(r => new
            {
                EnrolmentId = r.E.Id, UserId = r.U.Id, CourseId = r.C.Id, Name = r.U.Name, CourseTitle = r.C.Title, Complete = r.Completion == 100,
                Collapsed = collapsed.Contains(r.E.Id),
                ToggleLabel = (collapsed.Contains(r.E.Id) ? "Expand" : "Collapse") + " details for " + r.U.Name + " in " + r.C.Title,
                Head = CardHead(r), Body = CardBody(r)
            }).ToList();
            Cards.DataBind();
            NoCards.Visible = roster.Count == 0;
        }

        protected string Chevron => Icons.Kit(CardIcons["chevron"]);

        static readonly Dictionary<string, string> CardIcons = new Dictionary<string, string>
        {
            ["mail"] = "<rect x=\"3\" y=\"5\" width=\"18\" height=\"14\" rx=\"2\"/><path d=\"m3 7 9 6 9-6\"/>",
            ["book"] = "<path d=\"M4 19.5A2.5 2.5 0 0 1 6.5 17H20V3H6.5A2.5 2.5 0 0 0 4 5.5z\"/><path d=\"M4 19.5A2.5 2.5 0 0 0 6.5 22H20v-5\"/>",
            ["chart"] = "<path d=\"M3 3v18h18\"/><path d=\"M18 17V9M13 17V5M8 17v-3\"/>",
            ["list"] = "<path d=\"m3 6 2 2 3-3M3 13l2 2 3-3\"/><path d=\"M12 7h9M12 14h9M12 20h9\"/>",
            ["target"] = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><circle cx=\"12\" cy=\"12\" r=\"5\"/><circle cx=\"12\" cy=\"12\" r=\"1\"/>",
            ["clock"] = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 7v5l3 2\"/>",
            ["award"] = "<circle cx=\"12\" cy=\"9\" r=\"6\"/><path d=\"m8.5 14-1.5 8 5-3 5 3-1.5-8\"/>",
            ["alert"] = "<path d=\"M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z\"/><path d=\"M12 9v4M12 17h.01\"/>",
            ["check"] = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"m8.5 12 2.5 2.5 4.5-5\"/>",
            ["pending"] = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M9.5 9.5a2.5 2.5 0 1 1 3.5 2.3c-.6.3-1 .8-1 1.5M12 16.5h.01\"/>",
            ["chevron"] = "<path d=\"m6 9 6 6 6-6\"/>"
        };

        static string Mark(string background, string colour, string paths) =>
            "<span class=\"sp-mark\" style=\"--mk:" + background + ";--mk-fg:" + colour + "\" aria-hidden=\"true\"><svg viewBox=\"0 0 16 16\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\">" + paths + "</svg></span>";

        static string CourseMark(CourseRow c)
        {
            switch ((c.Icon ?? "").ToUpperInvariant())
            {
                case "DB": return Mark("#13aa52", "#fff", "<path d=\"M8 2c2.5 2.6 3 5 3 6.5S9.6 12 8 14c-1.6-2-3-3.6-3-5.5S5.5 4.6 8 2Z\"/><path d=\"M8 5v9\"/>");
                case "C#": return Mark("#2563eb", "#fff", "<path d=\"M8 1.8 13.4 5v6L8 14.2 2.6 11V5Z\"/><path d=\"M9.6 6.4a2.2 2.2 0 1 0 0 3.2\"/>");
                case "API": return Mark("#f97316", "#fff", "<path d=\"M6 3.5c-1.5 0-1.5 1-1.5 2S4 7.5 3 8c1 .5 1.5 1 1.5 2.5s0 2 1.5 2M10 3.5c1.5 0 1.5 1 1.5 2S12 7.5 13 8c-1 .5-1.5 1-1.5 2.5s0 2-1.5 2\"/>");
                case "DS": return Mark("#0d9488", "#fff", "<path d=\"M3 13V9M6.5 13V5M10 13V7.5M13.5 13V3\"/>");
                default: return Mark("#e4e4e7", "#18181b", "<path d=\"M4 12 8 4l4 8M5.5 9.5h5\"/>");
            }
        }

        static string TypeMark(string type) =>
            type == "lab" ? Mark("#f59e0b", "#1c1206", "<path d=\"M6 2h4M6.5 2v4L3 13a1 1 0 0 0 .9 1.4h8.2A1 1 0 0 0 13 13L9.5 6V2\"/>")
            : type == "quiz" ? Mark("#22c55e", "#06210f", "<path d=\"m4.5 8.5 2.3 2.3L11.5 5\"/>")
            : Mark("#3b82f6", "#fff", "<path d=\"M4.5 2.5h5l2 2v9h-7Z\"/><path d=\"M6.5 8h3M6.5 10.5h3\"/>");

        static readonly string MailMark = Mark("#f4f4f5", "#18181b", "<rect x=\"2.5\" y=\"4\" width=\"11\" height=\"8.5\" rx=\"1.5\"/><path d=\"m3 5 5 4 5-4\"/>");

        /// <summary>"Just now", "5 min ago", "3 hours ago", "2 days ago".</summary>
        static string Relative(DateTime time)
        {
            var minutes = (int)Math.Max(0, (DateTime.Now - time).TotalMinutes);
            if (minutes < 1) return "Just now";
            if (minutes < 60) return minutes + " min ago";
            if (minutes < 1440) return Ui.Plural(minutes / 60, "hour") + " ago";
            return Ui.Plural(minutes / 1440, "day") + " ago";
        }

        static string CardHead(Record r)
        {
            var tile = string.IsNullOrEmpty(r.U.Avatar) ? Ui.H(Ui.Initials(r.U)) : "<img src=\"" + Ui.H(r.U.Avatar) + "\" alt=\"\">";
            return "<span class=\"sp-tile\" aria-hidden=\"true\">" + tile + "<span class=\"sp-tile-badge\">" + CourseMark(r.C) + "</span></span>"
                + "<div class=\"sp-head-copy\"><h2>" + Ui.H(r.U.Name) + "</h2><p>" + Ui.H(r.C.Title) + " &middot; " + Ui.H(Relative(r.E.LastActive ?? r.E.EnrolledAt)) + "</p></div>"
                + "<span class=\"sp-ring\" style=\"--p:" + r.Completion + "\" aria-hidden=\"true\"><b>" + r.Completion + "%</b></span>";
        }

        static string CardBody(Record r)
        {
            Func<string, string, string, string> row = (icon, label, value) => "<div class=\"sp-row\"><dt>" + Icons.Kit(CardIcons[icon]) + label + "</dt><dd>" + value + "</dd></div>";
            Func<string, string, string> chip = (text, cls) => "<span class=\"sp-chip " + cls + "\">" + text + "</span>";
            Func<bool, string, string> toggle = (on, label) => "<span class=\"sp-switch\" data-on=\"" + (on ? "true" : "false") + "\" role=\"img\" aria-label=\"" + label + ": " + (on ? "on" : "off") + "\">" + (on ? "On" : "Off") + "<i></i></span>";
            var lastActive = r.E.LastActive ?? r.E.EnrolledAt;
            var segments = new StringBuilder();
            for (int i = 0; i < 10; i++)
                segments.Append("<span>").Append(r.Completion > i * 10 ? "<span class=\"sp-segment-fill\" style=\"width:" + Math.Min(100, (r.Completion - i * 10) * 10) + "%\"></span>" : "").Append("</span>");
            var done = r.Lessons.Count(l => r.Completed.Contains(l.Id));
            var score = r.Average == null ? "sp-no-score" : r.Average < 70 ? "sp-low-score" : "sp-pass-score";
            var html = new StringBuilder("<div class=\"sp-block-body\"><dl class=\"sp-rows\">");
            html.Append(row("mail", "Email", chip(MailMark + Ui.H(r.U.Email), "")));
            html.Append(row("book", "Course", chip(CourseMark(r.C) + Ui.H(r.C.Title), "")));
            html.Append(row("chart", "Completion", "<div class=\"sp-progress-row\"><div class=\"sp-segments\" role=\"progressbar\" aria-label=\"" + Ui.H(r.U.Name + " completion in " + r.C.Title)
                + "\" aria-valuemin=\"0\" aria-valuemax=\"100\" aria-valuenow=\"" + r.Completion + "\">" + segments + "</div><b>" + r.Completion + "%</b></div>"));
            html.Append(row("list", "Activities", chip(done + "/" + r.Lessons.Count + " completed", "")));
            html.Append(row("target", "Quiz average", chip((r.Average == null ? "&mdash;" : r.Average + "%") + "<small>" + (r.QuizCount == 0 ? "No attempts" : Ui.Plural(r.QuizCount, "attempt")) + "</small>", score)));
            html.Append(row("clock", "Last active", chip("<time datetime=\"" + Workspace.Iso(lastActive) + "\" title=\"" + Workspace.Iso(lastActive) + "\">" + Ui.H(Relative(lastActive)) + "</time>", "")));
            html.Append(row("award", "Certificate", toggle(r.Completion == 100, "Certificate earned")));
            html.Append(row("alert", "Needs support", toggle(r.Average < 70, "Needs support")));
            html.Append("</dl><div class=\"sp-section\"><span class=\"sp-sub\">Activities</span><div class=\"sp-lesson-chips\">");
            foreach (var l in r.Lessons)
            {
                var ok = r.Completed.Contains(l.Id);
                html.Append("<span class=\"sp-lesson\" data-done=\"").Append(ok ? "true" : "false").Append("\" title=\"").Append(Ui.H(l.Title)).Append("\">")
                    .Append(TypeMark(l.Type)).Append(Ui.H(l.Title)).Append(Icons.Kit(CardIcons[ok ? "check" : "pending"])).Append("</span>");
            }
            var next = r.Lessons.FirstOrDefault(l => !r.Completed.Contains(l.Id));
            html.Append("</div></div><div class=\"sp-section\"><span class=\"sp-sub\">Next step</span><p>");
            html.Append(next == null ? "All activities are complete. The certificate is available to the learner."
                : "Continue with &ldquo;" + Ui.H(next.Title) + "&rdquo; (" + Ui.H(next.Type) + ", " + next.Minutes + " min)." + (r.Average < 70 ? " Quiz average is below the 70% pass mark, so a check-in may help." : ""));
            html.Append("</p></div></div>");
            return html.ToString();
        }

        protected void RosterFilter_Click(object sender, EventArgs e) =>
            Go("Learners?q=" + HttpUtility.UrlEncode(RosterSearch.Text.Trim()) + "&course=" + RosterCourse.SelectedValue + "&state=" + RosterState.SelectedValue + "&sort=" + RosterSort.SelectedValue);

        protected void Toggle_Command(object sender, CommandEventArgs e)
        {
            var id = int.Parse(e.CommandArgument.ToString(), CultureInfo.InvariantCulture);
            var collapsed = Session[CollapsedKey] as HashSet<int> ?? new HashSet<int>();
            if (!collapsed.Remove(id)) collapsed.Add(id);
            Session[CollapsedKey] = collapsed;
            Go(Request.RawUrl + "#sp-card-" + id);
        }

        protected void Remove_Command(object sender, CommandEventArgs e)
        {
            var ids = e.CommandArgument.ToString().Split(':');
            if (TryRun("enrollmentSave", new { courseId = ids[1], userId = ids[0], remove = true })) ReloadPage("Enrolment removed");
        }

        protected void EnrolToggle_Click(object sender, EventArgs e)
        {
            EnrolForm.Visible = true;
            EnrolCourse.DataSource = CourseOptions(own);
            EnrolCourse.DataBind();
            Select(EnrolCourse, QueryInt("course")?.ToString(CultureInfo.InvariantCulture));
            // The list is searchable (by name, username, email or ID), so each choice carries all of those.
            EnrolLearner.DataSource = Data.Users.Where(u => u.Role == "Learner" && u.Status == "Active").OrderBy(u => u.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(u => new { u.Id, Name = u.Name + "  ·  ID " + u.Id + "  ·  @" + u.Username + "  ·  " + u.Email }).ToList();
            EnrolLearner.DataBind();
        }

        protected void Enrol_Click(object sender, EventArgs e)
        {
            if (TryRun("enrollmentSave", new { courseId = EnrolCourse.SelectedValue, userId = EnrolLearner.SelectedValue })) ReloadPage("Learner enrolled");
        }

        // ---- program leader: learners table ----------------------------------------------------

        void ShowTable()
        {
            Screens.ActiveViewIndex = 2;
            var course = QueryInt("course");
            var term = (Request.QueryString["q"] ?? "").Trim();
            Status = new[] { "progress", "done", "idle" }.Contains(Request.QueryString["status"]) ? Request.QueryString["status"] : "all";
            Expanded = Request.QueryString["more"] == "1";
            if (course != null) ScopedTitle = own.FirstOrDefault(c => c.Id == course)?.Title ?? "Unavailable";
            var es = course == null ? scope : scope.Where(x => x.CourseId == course).ToList();

            var progress = es.Select(x => Progress(x.UserId, x.CourseId)).ToList();
            Func<string, int, string, string, bool, string> stat = (label, value, suffix, note, good) =>
                "<article class=\"kx-stat" + (good ? " is-good" : "") + "\"><header><span>" + label + "</span><span class=\"kx-dots\" aria-hidden=\"true\">&#8942;</span></header><strong><span data-count=\""
                + value + "\">" + value + "</span>" + suffix + "</strong><small>" + note + "</small></article>";
            Stats = stat("Learners", es.Select(x => x.UserId).Distinct().Count(), "", Ui.Plural(es.Count, "enrolment"), false)
                + stat("Average completion", Charts.Avg(progress), "%", "Across all enrolments", false)
                + stat("Completed", progress.Count(p => p == 100), "", "Finished every activity", true)
                + stat("In progress", progress.Count(p => p > 0 && p < 100), "", "Started, not finished", false);

            if (!IsPostBack)
            {
                search.Text = term;
                filter.Items.Add(new ListItem("All courses", ""));
                foreach (var c in own) filter.Items.Add(new ListItem(c.Title, c.Id.ToString(CultureInfo.InvariantCulture)));
                Select(filter, course?.ToString(CultureInfo.InvariantCulture));
            }
            filter.Visible = course == null;
            foreach (var pair in new[] { Tuple.Create(StatusAll, "all"), Tuple.Create(StatusProgress, "progress"), Tuple.Create(StatusDone, "done"), Tuple.Create(StatusIdle, "idle") })
                pair.Item1.Attributes["aria-pressed"] = pair.Item2 == Status ? "true" : "false";

            LeaderTable.Enrollments = es.Where(x =>
            {
                var user = Data.User(x.UserId);
                var text = (user?.Name ?? "") + " " + (user?.Username ?? "") + " " + x.UserId + " " + (Data.Course(x.CourseId)?.Title ?? "");
                var p = Progress(x.UserId, x.CourseId);
                var key = p == 100 ? "done" : p > 0 ? "progress" : "idle";
                return SearchMatch.Matches(term, text, user?.Email) && (Status == "all" || key == Status);
            }).ToList();
        }

        string TableUrl(string status, bool more)
        {
            var course = QueryInt("course");
            var term = (Request.QueryString["q"] ?? "").Trim();
            return "Learners?status=" + status + (course == null ? "" : "&course=" + course) + (term.Length == 0 ? "" : "&q=" + HttpUtility.UrlEncode(term)) + (more ? "&more=1" : "");
        }

        protected void Status_Command(object sender, CommandEventArgs e) => Go(TableUrl(e.CommandArgument.ToString(), false));

        protected void More_Click(object sender, EventArgs e) => Go(TableUrl(Status, true));

        protected void TableFilter_Click(object sender, EventArgs e)
        {
            var course = filter.Visible && filter.SelectedValue.Length > 0 ? filter.SelectedValue : QueryInt("course")?.ToString(CultureInfo.InvariantCulture);
            Go("Learners?status=" + Status + (course == null ? "" : "&course=" + course) + "&q=" + HttpUtility.UrlEncode(search.Text.Trim()));
        }
    }
}
