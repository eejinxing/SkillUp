using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json.Linq;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend.Controls
{
    /// <summary>
    /// The program leader's overview, scoped to the leader's programs: headline metrics, daily learning activity against
    /// the previous period (1, 7, 14 or 24 days), one course's performance, overall completion, and an inbox of alerts
    /// whose read / archived state is saved in USER.preferences (leaderInbox).
    /// </summary>
    public partial class LeaderDashboard : UserControl
    {
        sealed class Note
        {
            public string Id, Kind, Verb, Object, To, Href, Tab;
            public UserRow Actor;
            public DateTime? At;
            public bool MessageRead;
        }

        static readonly CultureInfo Gb = CultureInfo.GetCultureInfo("en-GB");
        static string MonitorSvg(string paths) =>
            "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.5\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">" + paths + "</svg>";
        protected static readonly string Arrow = MonitorSvg("<path d=\"M7 17 17 7M8 7h9v9\"/>");
        protected static readonly string MonitorMore = MonitorSvg("<circle cx=\"12\" cy=\"5\" r=\"1\"/><circle cx=\"12\" cy=\"12\" r=\"1\"/><circle cx=\"12\" cy=\"19\" r=\"1\"/>");
        protected static readonly string FilterIcon = MonitorSvg("<path d=\"M4 5h16l-6 7v6l-4 2v-8Z\"/>");
        protected static readonly string ScrollIcon = MonitorSvg("<path d=\"m7 9 5 5 5-5\"/>");

        SkillPage Host => (SkillPage)Page;
        string WindowKey => "SkillUp.Leader.Window." + Host.Me.Id;
        string CourseKey => "SkillUp.Leader.Course." + Host.Me.Id;
        string TabKey => "SkillUp.Leader.NoteTab." + Host.Me.Id;

        protected int Days = 14, PendingCount, Shown;
        protected string Rail, Metrics, Change, Chart, CourseFields, Breakdown, CourseFoot, CourseRate, Health, Tab = "unread", TabLabel, EmptyNote;
        protected CourseRow Selected;
        List<Note> alerts = new List<Note>();
        HashSet<string> read = new HashSet<string>(), archived = new HashSet<string>();

        protected static string Native(string file, string cls) =>
            "<span class=\"ld-native " + cls + "\" aria-hidden=\"true\"><img src=\"assets/leader-dashboard/" + file + "\" alt=\"\"></span>";

        protected int TabCount(string tab) => alerts.Count(n => n.Tab == tab);

        protected void Page_Load(object sender, EventArgs e)
        {
            var me = Host.Me;
            var data = Host.Data;
            var now = DateTime.Now;
            var today = now.Date;
            Days = Session[WindowKey] as int? ?? 14;
            Rail = string.Concat(new[]
            {
                new[] { "Dashboard", "Overview", "grid" }, new[] { "Programs", "Programs", "book" }, new[] { "Approvals", "Course approvals", "check" },
                new[] { "Learners", "Learner progress", "users" }, new[] { "Messages", "Messages", "chat" }, new[] { "Analytics", "Program analytics", "chart" }
            }.Select(l => "<a href=\"" + l[0] + "\" aria-label=\"" + l[1] + "\" title=\"" + l[1] + "\"" + (l[0] == "Dashboard" ? " aria-current=\"page\"" : "") + ">" + Icons.App(l[2]) + "</a>"));

            var programs = data.Programs.Where(p => p.LeaderId == me.Id).ToList();
            var pids = new HashSet<int>(programs.Select(p => p.Id));
            var courses = data.Courses.Where(c => pids.Contains(c.ProgramId)).ToList();
            var cids = new HashSet<int>(courses.Select(c => c.Id));
            var enrolments = data.Enrollments.Where(x => cids.Contains(x.CourseId)).ToList();
            var attempts = Insights.Attempts(Host).Where(a => cids.Contains(a.CourseId) && a.At <= now).ToList();
            var progress = enrolments.ToDictionary(x => x.Id, x => Host.Progress(x.UserId, x.CourseId));

            // Learners active in the last 30 days (enrolment activity or an attempt), and in the 30 days before.
            var learners = new HashSet<int>(data.Users.Where(u => u.Role == "Learner" && u.Status == "Active").Select(u => u.Id));
            var activity = enrolments.Select(x => Tuple.Create(x.UserId, x.LastActive ?? x.EnrolledAt)).Concat(attempts.Select(a => Tuple.Create(a.UserId, a.At)))
                .Where(x => learners.Contains(x.Item1) && x.Item2 <= now).ToList();
            var recent = today.AddDays(-29);
            var active = activity.Where(x => x.Item2 >= recent).Select(x => x.Item1).Distinct().Count();
            var wasActive = activity.Where(x => x.Item2 >= recent.AddDays(-30) && x.Item2 < recent).Select(x => x.Item1).Distinct().Count();

            // Requests waiting for this leader, plus courses pending review without a request record.
            var pending = data.Requests.Where(r => r.ProgramId != null && pids.Contains(r.ProgramId.Value) && r.Status == "Pending review")
                .Select(r => new Note { Id = "approval-" + r.Id, Kind = "approval", At = r.CreatedAt, Actor = User(r.RequesterId), Verb = r.Kind == "Delete" ? "requested deletion of" : "submitted for review", Object = r.CourseTitle, Href = "Approvals?id=" + r.Id })
                .ToList();
            foreach (var c in courses.Where(c => c.Status == "Pending review"))
                if (!data.Requests.Any(r => r.Kind == "Create" && r.CourseId == c.Id && r.Status == "Pending review"))
                    pending.Add(new Note { Id = "approval-course-" + c.Id, Kind = "approval", Actor = User(c.InstructorId), Verb = "submitted for review", Object = c.Title, Href = "CourseReview?id=" + c.Id });
            PendingCount = pending.Count;

            var completed = enrolments.Count(x => progress[x.Id] == 100);
            var completion = Ui.Percent(completed, enrolments.Count);
            var metrics = new[]
            {
                new[] { "Managed Programs", programs.Count.ToString(CultureInfo.InvariantCulture), "Academic Year of " + now.Year, "Programs", "250-5042-imgVector.svg", "programs" },
                new[] { "Program courses", courses.Count.ToString(CultureInfo.InvariantCulture), pending.Count + " awaiting reviews", "Approvals", "250-5074-imgVector.svg", "courses" },
                new[] { "Active learners", active.ToString(CultureInfo.InvariantCulture), (active - wasActive >= 0 ? "+" : "") + (active - wasActive) + " vs prior 30 days", "Learners", "250-5106-imgVector.svg", "active" },
                new[] { "Completion rate", completion + "%", completed + " of " + enrolments.Count + " enrolments completed", "Analytics", "250-5139-imgVector.svg", "completion" }
            };
            Metrics = string.Concat(metrics.Select((m, i) => "<a class=\"ld-metric\" href=\"" + m[3] + "\">" + Native(m[4], "ld-surface") + "<h2>" + m[0] + "</h2><strong data-leader-metric=\"" + m[5] + "\">" + m[1]
                + "</strong>" + LeaderArt.Icon(i) + "<p>" + Ui.H(m[2]) + "</p></a>"));

            BuildChart(today, attempts, enrolments);
            BuildReach(courses, enrolments, progress, programs, active, completed, completion);
            BuildNotes(me, data, now, pids, courses, enrolments, pending, progress);

            if (!IsPostBack)
            {
                Periods.DataSource = new[]
                {
                    new { Value = 24, Label = "24D", Title = "Last 24 days", File = "250-5178-imgVector.svg" }, new { Value = 14, Label = "14D", Title = "Last 14 days", File = "250-5180-imgVector.svg" },
                    new { Value = 7, Label = "7D", Title = "Last 7 days", File = "250-5182-imgVector.svg" }, new { Value = 1, Label = "now", Title = "Today", File = "250-5184-imgVector.svg" }
                }.Select(p => new { p.Value, p.Label, p.Title, p.File, Pressed = p.Value == Days ? "true" : "false" });
                Periods.DataBind();
            }
        }

        UserRow User(int? id) => Host.Data.User(id) ?? new UserRow { FirstName = "Former", LastName = "user" };

        /// <summary>Learning sessions per day: graded attempts and new enrolments, against the same number of days before.</summary>
        void BuildChart(DateTime today, List<Insights.Attempt> attempts, List<EnrollmentRow> enrolments)
        {
            var events = attempts.Select(a => a.At.Date).Concat(enrolments.Select(x => x.EnrolledAt.Date)).ToList();
            var start = today.AddDays(-Days + 1);
            var series = Enumerable.Range(0, Days).Select(i => new
            {
                Date = start.AddDays(i), Value = events.Count(d => d == start.AddDays(i)), Prior = events.Count(d => d == start.AddDays(i - Days))
            }).ToList();
            int total = series.Sum(d => d.Value), prior = series.Sum(d => d.Prior);
            var max = Math.Max(4, (int)Math.Ceiling(series.Max(d => d.Value + d.Prior) / 4.0) * 4);
            Change = (total - prior >= 0 ? "+" : "") + (total - prior);
            var lines = new[] { "250-5199-imgVector.svg", "250-5197-imgVector.svg", "250-5195-imgVector.svg", "250-5193-imgVector.svg", "250-5191-imgVector.svg" };
            var html = new StringBuilder("<figure class=\"ld-activity-figure\"><div class=\"ld-chart-plot\"><div class=\"ld-y-axis\">");
            foreach (var i in new[] { 4, 3, 2, 1, 0 }) html.Append("<span>").Append(max * i / 4).Append("</span>");
            html.Append("</div><div class=\"ld-grid-lines\" aria-hidden=\"true\">");
            for (int i = 0; i < lines.Length; i++) html.Append("<span style=\"top:").Append(i * 25).Append("%\">").Append(Native(lines[i], "")).Append("</span>");
            html.Append("</div><div class=\"ld-daily-bars\" role=\"img\" aria-label=\"Daily learning sessions in selected and preceding periods\">");
            foreach (var d in series)
            {
                var sum = d.Value + d.Prior;
                html.Append("<div class=\"ld-day\" data-day=\"").Append(d.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append("\" data-count=\"").Append(d.Value).Append("\" data-prior=\"").Append(d.Prior)
                    .Append("\" title=\"").Append(d.Date.ToString("dd/MM/yyyy", Gb)).Append(": ").Append(d.Value).Append(" sessions; prior day: ").Append(d.Prior).Append("\"><span class=\"ld-day-stack\" style=\"height:")
                    .Append(Pct(sum, max)).Append("%\">");
                if (d.Prior > 0) html.Append("<span class=\"ld-day-prior\" style=\"height:").Append(Pct(d.Prior, sum)).Append("%\"></span>");
                if (d.Value > 0) html.Append("<span class=\"ld-day-current\" style=\"height:").Append(Pct(d.Value, sum)).Append("%\"></span>");
                html.Append("</span></div>");
            }
            html.Append("</div></div><div class=\"ld-x-axis\">");
            foreach (var i in new[] { 0, (Days - 1) / 2, Days - 1 }.Distinct()) html.Append("<span>").Append(series[i].Date.ToString("d MMM", Gb)).Append("</span>");
            html.Append("</div><figcaption>").Append(total).Append(" learning ").Append(total == 1 ? "session" : "sessions").Append(" and attempts recorded &middot; <a href=\"Analytics\">View report</a></figcaption></figure>");
            Chart = html.ToString();
        }

        static string Pct(int part, int whole) => (100.0 * part / whole).ToString("0.##", CultureInfo.InvariantCulture);

        void BuildReach(List<CourseRow> courses, List<EnrollmentRow> enrolments, Dictionary<int, int> progress, List<ProgramRow> programs, int active, int completed, int completion)
        {
            NoCourses.Visible = courses.Count == 0;
            Reach.Visible = courses.Count > 0;
            if (courses.Count == 0) return;
            var chosen = Session[CourseKey] as int?;
            Selected = courses.FirstOrDefault(c => c.Id == chosen) ?? courses[0];
            if (!IsPostBack)
            {
                PerformanceCourse.DataSource = SkillPage.CourseOptions(courses);
                PerformanceCourse.DataBind();
                PerformanceCourse.SelectedValue = Selected.Id.ToString(CultureInfo.InvariantCulture);
            }
            var es = enrolments.Where(x => x.CourseId == Selected.Id).ToList();
            var values = es.Select(x => progress[x.Id]).ToList();
            var done = values.Count(p => p == 100);
            var lessons = Host.Data.LessonsOf(Selected.Id).Count;
            Func<string, object, string, string> field = (label, value, cls) => "<div class=\"" + cls + "\"><dt>" + label + "</dt><dd>" + Ui.H(value) + "</dd></div>";
            CourseFields = field("Course name", Selected.Title, "is-wide") + field("Average progress", es.Count == 0 ? "—" : Charts.Avg(values) + "%", "is-progress")
                + field("Publishing status", Selected.Status ?? "Draft", "is-status") + field("Program", programs.FirstOrDefault(p => p.Id == Selected.ProgramId)?.Name ?? "No program", "")
                + field("Instructor", Host.Data.User(Selected.InstructorId)?.Name ?? "Unassigned", "") + field("Activities", Ui.Plural(lessons, "activity", "activities"), "")
                + field("Due date", Selected.Deadline == null ? "Not set" : Selected.Deadline.Value.ToString("d MMM yyyy", Gb), "");
            Breakdown = field("Enrolled learners", es.Count, "") + field("In progress", values.Count(p => p > 0 && p < 100), "") + field("Completed", done, "");
            CourseFoot = es.Count > 0 ? "Based on " + es.Count + " recorded enrolments" : "No enrolments in this course yet";
            CourseRate = es.Count > 0 ? Ui.Percent(done, es.Count) + "%" : "—";
            var circumference = 2 * Math.PI * 64;
            Health = "<dl class=\"ld-health-values\"><div><dt>Active learners</dt><dd>" + active + "</dd></div><div><dt>Total enrolments</dt><dd data-leader-reach=\"enrolments\">" + enrolments.Count
                + "</dd></div><div><dt>Completed enrolments</dt><dd>" + completed + " / " + enrolments.Count + "</dd></div></dl><div class=\"ld-orb-wrap\" role=\"img\" aria-label=\"Overall completion rate "
                + completion + " percent\"><svg viewBox=\"0 0 160 160\" aria-hidden=\"true\"><circle class=\"ld-orbit\" cx=\"80\" cy=\"80\" r=\"64\"/><circle class=\"ld-arc\" cx=\"80\" cy=\"80\" r=\"64\" stroke-dasharray=\""
                + (circumference * completion / 100).ToString("0.###", CultureInfo.InvariantCulture) + " " + circumference.ToString("0.###", CultureInfo.InvariantCulture) + "\"/></svg><div class=\"ld-orb\"><strong>"
                + completion + "%</strong></div></div><p class=\"ld-health-caption\">Overall completion rate</p>";
        }

        void BuildNotes(UserRow me, Model data, DateTime now, HashSet<int> pids, List<CourseRow> courses, List<EnrollmentRow> enrolments, List<Note> pending, Dictionary<int, int> progress)
        {
            try
            {
                var inbox = JObject.Parse(string.IsNullOrEmpty(me.Preferences) ? "{}" : me.Preferences)["leaderInbox"];
                read = new HashSet<string>((inbox?["read"] as JArray ?? new JArray()).Select(t => (string)t));
                archived = new HashSet<string>((inbox?["archived"] as JArray ?? new JArray()).Select(t => (string)t));
            }
            catch (Exception) { }

            alerts.AddRange(pending);
            foreach (var r in data.Requests.Where(r => r.ProgramId != null && pids.Contains(r.ProgramId.Value) && r.Status != "Pending review"))
            {
                var decision = r.Events.LastOrDefault(x => x.Action == r.Status);
                alerts.Add(new Note { Id = "status-" + r.Id + "-" + r.Status, Kind = "status", At = decision?.CreatedAt ?? r.CreatedAt, Actor = User(decision?.ActorId ?? r.RequesterId), Verb = "changed the status of", Object = r.CourseTitle, To = r.Status });
            }
            foreach (var x in enrolments)
            {
                var c = courses.FirstOrDefault(k => k.Id == x.CourseId);
                if (c == null) continue;
                if (progress[x.Id] == 100) alerts.Add(new Note { Id = "done-" + x.Id, Kind = "complete", At = x.LastActive ?? x.EnrolledAt, Actor = User(x.UserId), Verb = "completed", Object = c.Title, To = "Completed" });
                else alerts.Add(new Note { Id = "enrol-" + x.Id, Kind = "enrol", At = x.EnrolledAt, Actor = User(x.UserId), Verb = "enrolled in", Object = c.Title });
            }
            foreach (var m in data.Messages.Where(m => m.ReceiverId == me.Id))
                alerts.Add(new Note { Id = "msg-" + m.Id, Kind = "message", At = m.SentAt, Actor = User(m.SenderId), Verb = "sent you a message", Href = "Messages?id=" + m.SenderId, MessageRead = m.Read });
            var cids = new HashSet<int>(courses.Select(c => c.Id));
            foreach (var a in data.Announcements.Where(a => a.CourseId != null && cids.Contains(a.CourseId.Value)))
                alerts.Add(new Note { Id = "ann-" + a.Id, Kind = "announcement", At = a.CreatedAt, Actor = User(a.PostedBy), Verb = "posted an update in", Object = courses.FirstOrDefault(c => c.Id == a.CourseId)?.Title ?? "a course" });
            alerts = alerts.Where(n => n.At != null ? n.At <= now : n.Kind == "approval").OrderByDescending(n => n.At ?? DateTime.MinValue).ToList();
            foreach (var n in alerts) n.Tab = archived.Contains(n.Id) ? "archived" : read.Contains(n.Id) || n.MessageRead ? "read" : "unread";

            var tab = Session[TabKey] as string;
            Tab = tab == "read" || tab == "archived" ? tab : "unread";
            TabLabel = Tab == "unread" ? "Unread" : Tab == "read" ? "Read" : "Archived";
            foreach (var pair in new[] { Tuple.Create(TabUnread, "unread"), Tuple.Create(TabRead, "read"), Tuple.Create(TabArchived, "archived") })
                pair.Item1.Attributes["aria-selected"] = pair.Item2 == Tab ? "true" : "false";
            var shown = alerts.Where(n => n.Tab == Tab).ToList();
            Shown = shown.Count;
            EmptyNote = Tab == "unread" ? "<b>All caught up.</b><span>New activity will appear here.</span>"
                : Tab == "read" ? "<b>Nothing read yet.</b><span>Notifications you mark as read appear here.</span>"
                : "<b>No archived notifications.</b><span>Archive a read notification to keep it here.</span>";
            // On a postback the cards come back from view state, so a click acts on the alert it was drawn for.
            if (IsPostBack) return;
            Notes.DataSource = shown.Select(n => new
            {
                n.Id, n.Kind, n.Tab,
                Action = n.Tab == "unread" ? "read" : n.Tab == "read" ? "archive" : "restore",
                ActionLabel = n.Tab == "unread" ? "Mark as read" : n.Tab == "read" ? "Archive" : "Restore",
                Summary = "<span class=\"ld-note-dot" + (n.Tab == "unread" ? "" : " is-read") + "\" aria-hidden=\"true\"></span><span class=\"ld-monitor-note-copy\"><span class=\"ld-monitor-event\"><strong>"
                    + Ui.H(n.Actor.Name) + "</strong> " + Ui.H(n.Verb) + (n.Object == null ? "" : " " + Ui.H(n.Object)) + (n.Kind == "status" && n.To != null ? " &middot; " + Ui.H(n.To) : "") + "</span><time"
                    + (n.At == null ? "" : " datetime=\"" + Workspace.Iso(n.At.Value) + "\" title=\"" + Ui.Stamp(n.At.Value) + "\"") + ">" + Ago(n.At, now) + "</time></span>",
                Link = n.Href == null ? "" : "<a class=\"ld-note-primary\" href=\"" + n.Href + "\">" + (n.Kind == "approval" ? "Review request" : "Respond") + " " + Arrow + "</a>"
            }).ToList();
            Notes.DataBind();
        }

        static string Ago(DateTime? at, DateTime now)
        {
            if (at == null) return "Date unavailable";
            var minutes = (int)Math.Max(0, (now - at.Value).TotalMinutes);
            if (minutes < 1) return "Just now";
            if (minutes < 60) return minutes + " min ago";
            if (minutes < 1440) return Ui.Plural(minutes / 60, "hour") + " ago";
            return Ui.Plural(minutes / 1440, "day") + " ago";
        }

        protected void Period_Command(object sender, CommandEventArgs e)
        {
            var days = int.Parse(e.CommandArgument.ToString(), CultureInfo.InvariantCulture);
            Session[WindowKey] = new[] { 1, 7, 14, 24 }.Contains(days) ? days : 14;
            Host.ReloadPage();
        }

        protected void Course_Changed(object sender, EventArgs e)
        {
            Session[CourseKey] = int.Parse(PerformanceCourse.SelectedValue, CultureInfo.InvariantCulture);
            Host.ReloadPage();
        }

        protected void Tab_Command(object sender, CommandEventArgs e)
        {
            Session[TabKey] = e.CommandArgument.ToString();
            Host.ReloadPage();
        }

        /// <summary>Mark as read, Archive or Restore one alert, then save the inbox in the leader's preferences.</summary>
        protected void Note_Command(object sender, CommandEventArgs e)
        {
            var parts = e.CommandArgument.ToString().Split(new[] { '|' }, 2);
            if (parts[0] == "archive") archived.Add(parts[1]);
            else if (parts[0] == "restore") archived.Remove(parts[1]);
            read.Add(parts[1]);
            if (Host.TryRun("preferences", new { section = "leaderInbox", read = read.ToArray(), archived = archived.ToArray() })) Host.ReloadPage();
        }
    }
}
