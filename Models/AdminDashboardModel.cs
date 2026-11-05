using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SkillUp.Services;

namespace SkillUp.Models
{
    /// <summary>
    /// The administrator's overview: recorded platform activity per day, the platform summary, active learners over the
    /// chosen window (7, 30 or 90 days), average completion per course, items needing attention, and every recorded
    /// action across all roles (?q=, ?role=, ?source=), which Export CSV downloads.
    /// </summary>
    public sealed class AdminDashboardModel : PageModel
    {
        public AdminDashboardModel(SkillContext ctx) : base(ctx) { }
        /// <summary>The activity log's current search and filters (?q=&amp;role=&amp;source=), for the filter boxes.</summary>
        public string Term = "", RoleFilter = "", SourceFilter = "";
        public List<Activity> Filtered => filtered;

        public sealed class Activity
        {
            public string Id, Name, Role, Action, Detail, Source;
            public int? UserId;
            public DateTime At;
        }

        static readonly Dictionary<string, string> Phrasing = new Dictionary<string, string>
        {
            ["Community message sent"] = "Sent a message in",
            ["Direct message sent"] = "Sent a direct message",
            ["Quiz submitted"] = "Submitted a quiz for",
            ["Lab submitted"] = "Submitted a lab for",
            ["Enrolled in course"] = "Enrolled in",
            ["Learning activity recorded"] = "Continued learning in",
            ["Discussion posted"] = "Started a discussion in",
            ["Discussion reply posted"] = "Replied to a discussion in",
            ["Course review posted"] = "Reviewed",
            ["Announcement published"] = "Published an announcement in"
        };
        static string TimelineIcon(string paths) =>
            "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">" + paths + "</svg>";
        static readonly Dictionary<string, string> SourceIcons = new Dictionary<string, string>
        {
            ["Audit"] = TimelineIcon("<path d=\"M12 3 5 6v5c0 4.4 3 8.3 7 10 4-1.7 7-5.6 7-10V6Z\"/><path d=\"m9 12 2 2 4-4\"/>"),
            ["Learning"] = TimelineIcon("<path d=\"M4 5.5c2.7-1 5.3-1 8 .5 2.7-1.5 5.3-1.5 8-.5v13c-2.7-1-5.3-1-8 .5-2.7-1.5-5.3-1.5-8-.5Z\"/><path d=\"M12 6v13\"/>"),
            ["Community"] = TimelineIcon("<path d=\"M4 5h16v11H9l-5 4Z\"/>"),
            ["Messages"] = TimelineIcon("<rect x=\"3.5\" y=\"5.5\" width=\"17\" height=\"13\" rx=\"2\"/><path d=\"m4 7 8 6 8-6\"/>")
        };
        static readonly string[] AvatarColours = { "#d3a28d", "#adc1c0", "#c9bfa5", "#aaaecb" };
        static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");
        static readonly CultureInfo Gb = CultureInfo.GetCultureInfo("en-GB");

        PageModel Host => this;
        string WindowKey => "SkillUp.Admin.Window." + Host.Me.Id;

        public int Days = 30, Active, Completed, Enrolments, PendingUsers, PendingCourses, PendingDeletions;
        public string Pulse, Summary, Growth, ActiveChart, CompletionChart, LogRows, LogCount;
        /// <summary>"Needs attention": which courses wait for a Program Leader, and which deletions are pending.</summary>
        public string PendingCourseList, PendingDeletionList;
        List<Activity> logs = new List<Activity>(), filtered = new List<Activity>();

        public void Load()
        {
            var data = Host.Data;
            var now = DateTime.Now;
            var today = now.Date;
            Days = new[] { 7, 30, 90 }.Contains(QueryInt("days") ?? 0) ? QueryInt("days").Value : 30;
            var attempts = Insights.Attempts(Ctx);
            logs = Recorded(data, attempts, now, Host.Me.Id);

            // Active learners: learners with an enrolment activity or attempt in the window, against the window before.
            var start = today.AddDays(-Days + 1);
            var learners = new HashSet<int>(data.Users.Where(u => u.Role == "Learner" && u.Status == "Active").Select(u => u.Id));
            var events = data.Enrollments.Select(x => Tuple.Create(x.UserId, x.LastActive ?? x.EnrolledAt)).Concat(attempts.Select(a => Tuple.Create(a.UserId, a.At)))
                .Where(x => learners.Contains(x.Item1)).ToList();
            Active = events.Where(x => x.Item2 >= start && x.Item2 <= now).Select(x => x.Item1).Distinct().Count();
            var prior = events.Where(x => x.Item2 >= start.AddDays(-Days) && x.Item2 < start).Select(x => x.Item1).Distinct().Count();
            Growth = (Active - prior > 0 ? "+" : "") + (Active - prior) + " vs. previous " + Days + " days";
            var daily = Enumerable.Range(0, Days).Select(i => start.AddDays(i))
                .Select(d => new { Date = d, Value = events.Where(x => x.Item2 >= d && x.Item2 < d.AddDays(1) && x.Item2 <= now).Select(x => x.Item1).Distinct().Count() }).ToList();
            var peak = Math.Max(1, daily.Max(d => d.Value));
            Func<int, double> px = i => daily.Count > 1 ? i * 320.0 / (daily.Count - 1) : 160;
            Func<int, double> py = v => 108 - v * 85.0 / peak;
            ActiveChart = "<figure class=\"ad-activity-chart\"><svg viewBox=\"0 0 320 120\" preserveAspectRatio=\"none\" role=\"img\" aria-label=\"Recorded active learners by day over the last " + Days
                + " days\"><line x1=\"0\" y1=\"110\" x2=\"320\" y2=\"110\" stroke=\"#ffffff18\"></line><polyline points=\"" + string.Join(" ", daily.Select((d, i) => F(px(i)) + "," + F(py(d.Value))))
                + "\" fill=\"none\" stroke=\"#f4f2ff\" stroke-width=\"2.5\" vector-effect=\"non-scaling-stroke\"></polyline>"
                + string.Concat(daily.Select((d, i) => "<circle cx=\"" + F(px(i)) + "\" cy=\"" + F(py(d.Value)) + "\" r=\"2.3\" fill=\"#f4f2ff\"><title>" + d.Date.ToString("dd/MM/yyyy", Gb) + ": " + d.Value + " active learners</title></circle>"))
                + "</svg><figcaption>Based on saved learning activity</figcaption></figure>";

            var rows = data.Courses.Select(c =>
            {
                var es = data.Enrollments.Where(x => x.CourseId == c.Id).ToList();
                return new { Course = c, Completion = es.Count == 0 ? (int?)null : Charts.Avg(es.Select(x => Host.Progress(x.UserId, x.CourseId))) };
            }).ToList();
            CompletionChart = "<figure class=\"ad-completion-chart\"><div class=\"ad-bars\" role=\"img\" aria-label=\"Average completion by course, from 0 to 100 percent\">"
                + (rows.Count == 0 ? "<p>No courses yet</p>" : string.Concat(rows.Select(r => "<span class=\"ad-bar-slot\" title=\"" + Ui.H(r.Course.Title) + ": " + (r.Completion == null ? "No enrolments" : r.Completion + "%")
                    + "\" data-admin-bar=\"" + r.Course.Id + "\" data-value=\"" + (r.Completion ?? 0) + "\"><span style=\"height:" + Math.Max(1, r.Completion ?? 0) + "%\" class=\"" + (r.Completion > 0 ? "" : "ad-bar-zero") + "\"></span></span>")))
                + "</div><figcaption>Average progress by course &middot; 0&ndash;100%</figcaption></figure>";
            Enrolments = data.Enrollments.Count;
            Completed = data.Enrollments.Count(x => data.Course(x.CourseId) != null && Host.Progress(x.UserId, x.CourseId) == 100);
            PendingUsers = data.Users.Count(u => u.Status == "Pending");
            PendingCourses = data.Courses.Count(c => c.Status == "Pending review");
            PendingDeletions = data.Requests.Count(r => r.Kind == "Delete" && r.Status == "Pending review");
            // The admin cannot open the Program Leader's approval pages, so the card lists what is waiting and who it waits for.
            var waitingCourses = data.Courses.Where(c => c.Status == "Pending review").Select(c => new
            {
                Course = c,
                Since = data.Requests.Where(r => r.CourseId == c.Id && r.Kind == "Create" && r.Status == "Pending review").Select(r => (DateTime?)r.CreatedAt).FirstOrDefault()
            }).OrderBy(x => x.Since ?? DateTime.MaxValue).ToList();
            PendingCourseList = AttentionList(waitingCourses.Select(x => AttentionItem(x.Course.Title, x.Course.ProgramId,
                "by " + (data.User(x.Course.InstructorId)?.Name ?? "no instructor"), x.Since, null)), "No courses are waiting for review.");
            var deletions = data.Requests.Where(r => r.Kind == "Delete" && r.Status == "Pending review").OrderBy(r => r.CreatedAt).ToList();
            PendingDeletionList = AttentionList(deletions.Select(r => AttentionItem(data.Course(r.CourseId)?.Title ?? r.CourseTitle, data.Course(r.CourseId)?.ProgramId ?? r.ProgramId,
                "requested by " + (data.User(r.RequesterId)?.Name ?? "a former user"), r.CreatedAt, r.Reason)), "No deletion requests are pending.");

            var quizzes = data.Courses.Count(c => (data.QuizFor(c.Id)?.Questions.Count ?? 0) > 0);
            var assessments = data.Lessons.Count(l => l.Type == "lab" || l.Type == "quiz");
            Summary = "<div class=\"ad-material ad-summary-material\" aria-hidden=\"true\"></div><h2>Platform summary</h2><a class=\"ad-users ad-summary-total\" href=\"Users\"><span>Total users</span><strong>"
                + data.Users.Count.ToString("N0", En) + "</strong><b aria-hidden=\"true\"><svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" width=\"16\" height=\"16\"><path d=\"M7 17 17 7M9 7h8v8\"/></svg></b></a>"
                + "<div class=\"ad-dot-matrix\" aria-hidden=\"true\">" + string.Concat(Enumerable.Range(0, 42).Select(i => "<i class=\"" + (i % 7 < 3 ? "lit" : "") + "\"></i>")) + "</div><dl class=\"ad-summary-legend\">"
                + string.Concat(new[] { new[] { "Total users", data.Users.Count.ToString(CultureInfo.InvariantCulture), "Users" }, new[] { "Total courses", data.Courses.Count.ToString(CultureInfo.InvariantCulture), "Catalog" },
                    new[] { "Total quizzes", quizzes.ToString(CultureInfo.InvariantCulture), "#admin-activity" }, new[] { "Total assessments", assessments.ToString(CultureInfo.InvariantCulture), "#admin-activity" } }
                    .Select(l => "<div><dt><a href=\"" + l[2] + "\">" + l[0] + "</a></dt><dd data-admin-summary=\"" + l[0] + "\">" + l[1] + "</dd></div>")) + "</dl>";

            Pulse = BuildPulse(data, today, now);
            ShowLogs();
        }

        static string F(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        /// <summary>Recorded activity across every role: audit entries, learning, community posts and the admin's own messages.</summary>
        static List<Activity> Recorded(Snapshot data, List<Insights.Attempt> attempts, DateTime now, int adminId)
        {
            var result = new List<Activity>();
            Func<int?, string> course = id => id == null ? null : data.Course(id.Value)?.Title;
            Action<string, int?, string, string, DateTime, string> add = (key, userId, action, detail, at, source) =>
            {
                if (at > now) return;
                var u = data.User(userId);
                result.Add(new Activity { Id = key, UserId = userId, Name = u?.Name ?? (userId != null ? "Deleted user" : "System"), Role = u?.Role ?? "System", Action = action, Detail = detail ?? "", At = at, Source = source });
            };
            foreach (var a in data.Audit) add("audit:" + a.Id, a.UserId, a.Action, a.Detail, a.CreatedAt, "Audit");
            foreach (var x in data.Enrollments)
            {
                add("enrol:" + x.Id, x.UserId, "Enrolled in course", course(x.CourseId) ?? "Removed course", x.EnrolledAt, "Learning");
                if (x.LastActive != null && x.LastActive != x.EnrolledAt) add("active:" + x.Id, x.UserId, "Learning activity recorded", course(x.CourseId) ?? "Removed course", x.LastActive.Value, "Learning");
            }
            foreach (var a in attempts) add("attempt:" + a.Type + a.UserId + a.At.Ticks, a.UserId, a.Type == "lab" ? "Lab submitted" : "Quiz submitted", (course(a.CourseId) ?? "Removed course") + " · " + a.Score + "%", a.At, "Learning");
            foreach (var p in data.Posts.Where(p => p.CourseId == null && p.LabId == null && p.ParentId == null && p.Status == "Visible"))
                add("globalMessages:" + p.Id, p.UserId, "Community message sent", "Community chat", p.CreatedAt, "Community");
            foreach (var p in data.Posts.Where(p => p.CourseId != null && p.ParentId == null))
            {
                add("posts:" + p.Id, p.UserId, "Discussion posted", course(p.CourseId) ?? "", p.CreatedAt, "Community");
                foreach (var r in data.Posts.Where(r => r.ParentId == p.Id)) add("posts:reply:" + r.Id, r.UserId, "Discussion reply posted", course(p.CourseId) ?? "", r.CreatedAt, "Community");
            }
            foreach (var f in data.Feedback.Where(f => f.CourseId != null))
            {
                add("reviews:" + f.Id, f.UserId, "Course review posted", course(f.CourseId) ?? "", f.CreatedAt, "Community");
                foreach (var r in data.FeedbackReplies.Where(r => r.ParentId == f.Id)) add("reviews:reply:" + r.Id, r.UserId, "Discussion reply posted", course(f.CourseId) ?? "", r.CreatedAt, "Community");
            }
            foreach (var a in data.Announcements) add("announcements:" + a.Id, a.PostedBy, "Announcement published", course(a.CourseId) ?? "", a.CreatedAt, "Community");
            foreach (var m in data.Messages.Where(m => m.SenderId == adminId || m.ReceiverId == adminId))
                add("message:" + m.Id, m.SenderId, "Direct message sent", "To " + (data.User(m.ReceiverId)?.Name ?? "removed account"), m.SentAt, "Messages");
            return result.OrderByDescending(r => r.At).ThenBy(r => r.Id, StringComparer.Ordinal).ToList();
        }

        /// <summary>Recorded actions per day over the window, against the window before.</summary>
        string BuildPulse(Snapshot data, DateTime today, DateTime now)
        {
            var start = today.AddDays(-Days + 1);
            var all = logs.Where(x => x.At <= now).ToList();
            var inRange = all.Where(x => x.At >= start).ToList();
            var previous = all.Count(x => x.At >= start.AddDays(-Days) && x.At < start);
            var series = Enumerable.Range(0, Days).Select(i => start.AddDays(i)).Select(d => new { Date = d, Value = inRange.Count(x => x.At.Date == d) }).ToList();
            var busiest = series.Aggregate((a, b) => b.Value > a.Value ? b : a);
            var people = inRange.Where(x => x.UserId != null).Select(x => x.UserId).Distinct().Count();
            var delta = inRange.Count - previous;

            const int W = 1000, H = 200;
            var max = Math.Max(4, series.Max(s => s.Value));
            var step = (int)Math.Ceiling(max / 4.0);
            var top = step * 4;
            Func<int, double> left = i => series.Count > 1 ? i * (double)W / (series.Count - 1) : W / 2.0;
            Func<int, double> height = v => H - v * (H - 12.0) / top;
            var points = string.Join(" ", series.Select((s, i) => left(i).ToString("0.0", CultureInfo.InvariantCulture) + "," + height(s.Value).ToString("0.0", CultureInfo.InvariantCulture)));
            Func<DateTime, string> shortDate = d => d.ToString("MMM d", En);
            var html = new StringBuilder("<header class=\"ad-pulse-head\"><div><h2 id=\"ad-pulse-title\">Platform activity</h2><p>Recorded actions and learning sessions across all roles &middot; last ")
                .Append(Days).Append(" days</p></div><dl class=\"ad-pulse-stats\"><div><dt>Recorded</dt><dd>").Append(inRange.Count.ToString("N0", En)).Append("</dd><small>").Append(delta > 0 ? "+" : "")
                .Append(delta.ToString("N0", En)).Append(" vs. previous ").Append(Days).Append(" days</small></div><div><dt>People active</dt><dd>").Append(people).Append("</dd><small>of ").Append(data.Users.Count)
                .Append(" accounts</small></div><div><dt>Busiest day</dt><dd>").Append(busiest.Value > 0 ? shortDate(busiest.Date) : "&mdash;").Append("</dd><small>").Append(busiest.Value).Append(" recorded</small></div></dl></header>");
            html.Append("<figure class=\"ad-pulse-chart\"><div class=\"ad-pulse-plot\"><div class=\"ad-pulse-y\" aria-hidden=\"true\">");
            foreach (var g in new[] { 4, 2, 0 }) html.Append("<span style=\"top:").Append((height(g * step) / H * 100).ToString("0.00", CultureInfo.InvariantCulture)).Append("%\">").Append(g * step).Append("</span>");
            html.Append("</div><svg viewBox=\"0 0 ").Append(W).Append(' ').Append(H).Append("\" preserveAspectRatio=\"none\" role=\"img\" aria-label=\"Recorded platform activity per day over the last ").Append(Days)
                .Append(" days, peaking at ").Append(busiest.Value).Append(" on ").Append(shortDate(busiest.Date)).Append("\"><defs><linearGradient id=\"ad-pulse-fill\" x1=\"0\" x2=\"0\" y1=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#ffffff\" stop-opacity=\".42\"/><stop offset=\"1\" stop-color=\"#ffffff\" stop-opacity=\"0\"/></linearGradient></defs><g class=\"ad-pulse-grid\">");
            for (int g = 0; g <= 4; g++) html.Append("<line x1=\"0\" x2=\"").Append(W).Append("\" y1=\"").Append(height(g * step).ToString("0.0", CultureInfo.InvariantCulture)).Append("\" y2=\"").Append(height(g * step).ToString("0.0", CultureInfo.InvariantCulture)).Append("\"/>");
            html.Append("</g><polygon points=\"0,").Append(H).Append(' ').Append(points).Append(' ').Append(W).Append(',').Append(H).Append("\" fill=\"url(#ad-pulse-fill)\"/><polyline points=\"").Append(points)
                .Append("\" fill=\"none\" stroke=\"#ffffff\" stroke-width=\"2\" vector-effect=\"non-scaling-stroke\" stroke-linejoin=\"round\"/><g class=\"ad-pulse-hits\">");
            for (int i = 0; i < series.Count; i++)
                html.Append("<rect x=\"").Append((left(i) - W / (double)series.Count / 2).ToString("0.0", CultureInfo.InvariantCulture)).Append("\" y=\"0\" width=\"").Append((W / (double)series.Count).ToString("0.0", CultureInfo.InvariantCulture))
                    .Append("\" height=\"").Append(H).Append("\"><title>").Append(series[i].Date.ToString("ddd, MMM d", En)).Append(": ").Append(series[i].Value).Append(" recorded</title></rect>");
            html.Append("</g></svg></div><div class=\"ad-pulse-x\" aria-hidden=\"true\">");
            foreach (var i in new[] { 0, .25, .5, .75, 1 }.Select(f => (int)Math.Round(f * (series.Count - 1), MidpointRounding.AwayFromZero)).Distinct())
                html.Append("<span style=\"left:").Append((left(i) / W * 100).ToString("0.00", CultureInfo.InvariantCulture)).Append("%\">").Append(shortDate(series[i].Date)).Append("</span>");
            return html.Append("</div></figure>").ToString();
        }

        void ShowLogs()
        {
            var q = Request.QueryString;
            var term = (q["q"] ?? "").Trim();
            var role = q["role"] ?? "";
            var source = q["source"] ?? "";
            Term = term;
            RoleFilter = role;
            SourceFilter = source;
            filtered = logs.Where(r => (role == "" || r.Role == role) && (source == "" || r.Source == source)
                && SearchMatch.Matches(term, string.Join(" ", r.Name, r.Role, r.Action), string.Join(" ", r.Detail, r.Source))).ToList();
            LogCount = term.Length + role.Length + source.Length == 0 ? logs.Count + " activities" : filtered.Count + " of " + logs.Count + " activities";
            if (filtered.Count == 0)
            {
                LogRows = "<li class=\"ad-empty\">No matching activity</li>";
                return;
            }
            var now = DateTime.Now;
            var html = new StringBuilder();
            foreach (var r in filtered)
            {
                var u = Host.Data.User(r.UserId);
                var initials = Ui.H(string.Concat(r.Name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => w[0])));
                var colour = AvatarColours[(r.UserId?.ToString(CultureInfo.InvariantCulture) ?? "").Sum(c => (int)c) % AvatarColours.Length];
                var linked = Host.Data.Courses.FirstOrDefault(c => r.Detail == c.Title || r.Detail.StartsWith(c.Title + " · ", StringComparison.Ordinal));
                var target = r.Detail.Length == 0 ? ""
                    : linked != null ? "<a class=\"ad-tl-detail\" href=\"Course?id=" + linked.Id + "\">" + Ui.H(r.Detail) + "</a>"
                    : r.Detail == "Community chat" ? "<a class=\"ad-tl-detail\" href=\"Messages\">" + Ui.H(r.Detail) + "</a>"
                    : "<span class=\"ad-tl-detail\">" + Ui.H(r.Detail) + "</span>";
                string phrase, icon;
                if (!Phrasing.TryGetValue(r.Action ?? "", out phrase)) phrase = r.Action;
                if (!SourceIcons.TryGetValue(r.Source, out icon)) icon = SourceIcons["Audit"];
                html.Append("<li class=\"ad-tl-item ad-feed-item\" data-source=\"").Append(r.Source).Append("\" title=\"").Append(Ui.H(r.Name + " · " + r.Role + " · " + r.At.ToString("d MMM yyyy, HH:mm", Gb)))
                    .Append("\"><div class=\"ad-feed-identity\"><span class=\"ad-tl-avatar\" style=\"--activity-avatar:").Append(colour).Append("\" role=\"img\" aria-label=\"").Append(Ui.H(r.Name)).Append("\">")
                    .Append(string.IsNullOrEmpty(u?.Avatar) ? initials : "<img src=\"" + Ui.H(u.Avatar) + "\" alt=\"\">").Append("</span><span class=\"ad-tl-icon\" aria-hidden=\"true\">").Append(icon)
                    .Append("</span></div><div class=\"ad-tl-body\"><p class=\"ad-tl-line\"><span class=\"sr-only\">").Append(Ui.H(r.Name)).Append(" (").Append(Ui.H(r.Role)).Append("): </span><span class=\"ad-tl-action\">")
                    .Append(Ui.H(phrase)).Append("</span>").Append(target).Append("<time class=\"sr-only\" datetime=\"").Append(Workspace.Iso(r.At)).Append("\"> &middot; ").Append(Ago(r.At, now))
                    .Append("</time></p><span class=\"ad-tl-tag\">").Append(r.Source).Append("</span></div></li>");
            }
            LogRows = html.ToString();
        }

        static string Ago(DateTime at, DateTime now)
        {
            var minutes = (int)Math.Max(0, Math.Round((now - at).TotalMinutes, MidpointRounding.AwayFromZero));
            if (minutes < 1) return "just now";
            if (minutes < 60) return minutes + (minutes == 1 ? " min ago" : " mins ago");
            var hours = (int)Math.Round(minutes / 60.0, MidpointRounding.AwayFromZero);
            if (hours < 24) return hours + (hours == 1 ? " hour ago" : " hours ago");
            var days = (int)Math.Round(hours / 24.0, MidpointRounding.AwayFromZero);
            if (days < 7) return days + (days == 1 ? " day ago" : " days ago");
            return at.ToString("d MMM", Gb);
        }

        static string AttentionList(IEnumerable<string> items, string empty)
        {
            var list = items.ToList();
            return list.Count == 0 ? "<p class=\"ad-attention-empty\">" + Ui.H(empty) + "</p>" : "<ul class=\"ad-attention-list\">" + string.Concat(list) + "</ul>";
        }

        /// <summary>One waiting course: title, who submitted it, which Program Leader must act, and for how long it has waited.</summary>
        string AttentionItem(string title, int? programId, string by, DateTime? since, string reason)
        {
            var program = programId == null ? null : Host.Data.Program(programId.Value);
            var leader = Host.Data.User(program?.LeaderId);
            var days = since == null ? (int?)null : Math.Max(0, (int)(DateTime.Now - since.Value).TotalDays);
            var waiting = days == null ? "" : days == 0 ? "today" : days == 1 ? "1 day" : days + " days";
            return "<li><strong>" + Ui.H(title) + "</strong><small>" + Ui.H(by) + " &middot; waiting for " + Ui.H(leader?.Name ?? "a Program Leader")
                + (program == null ? "" : " (" + Ui.H(program.Name) + ")") + "</small>"
                + (waiting.Length == 0 ? "" : "<em class=\"" + (days >= 7 ? "is-late" : "") + "\">" + waiting + "</em>")
                + (string.IsNullOrWhiteSpace(reason) ? "" : "<q>" + Ui.H(reason) + "</q>") + "</li>";
        }

    }
}