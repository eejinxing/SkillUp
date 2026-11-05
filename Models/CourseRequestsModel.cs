using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using SkillUp.Services;

namespace SkillUp.Models
{
    /// <summary>
    /// The instructor's course requests: a sortable, filterable table (?q=, ?kind=, ?status=, ?sort=, ?page=, ?size=)
    /// and the selected request's details (?id=, ?tab=overview|activity). ?export=id downloads one request as CSV.
    /// </summary>
    public sealed class CourseRequestsModel : PageModel
    {
        public CourseRequestsModel(SkillContext ctx) : base(ctx) { }

        public sealed class Req
        {
            public RequestRow Row;
            public CourseRow Course;
            public UserRow Leader;
            public string Program, Reviewer, NextStep;
            public int Priority, Activities;
            public double Hours;
            public string Tone => Row.Status.ToLowerInvariant().Replace(' ', '-');
        }

        static readonly string[] Terminal = { "Approved", "Rejected", "Changes requested", "Withdrawn" };
        static readonly Dictionary<string, string> NextFor = new Dictionary<string, string>
        {
            ["Pending review"] = "Await leader review", ["Approved"] = "Publish to learners", ["Changes requested"] = "Update course content",
            ["Rejected"] = "Revise course outline", ["Withdrawn"] = "Archive draft"
        };
        static readonly string[] PriorityName = { "", "Low", "Medium", "High", "Urgent" };
        static readonly Dictionary<string, string> Ic = new Dictionary<string, string>
        {
            ["send"] = "<path d=\"m22 2-7 20-4-9-9-4Z\"/><path d=\"M22 2 11 13\"/>",
            ["eye"] = "<path d=\"M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/>",
            ["badge"] = "<path d=\"M3.85 8.62a4 4 0 0 1 4.78-4.77 4 4 0 0 1 6.74 0 4 4 0 0 1 4.78 4.78 4 4 0 0 1 0 6.74 4 4 0 0 1-4.77 4.78 4 4 0 0 1-6.75 0 4 4 0 0 1-4.78-4.77 4 4 0 0 1 0-6.76Z\"/><path d=\"m9 12 2 2 4-4\"/>",
            ["reject"] = "<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"m15 9-6 6M9 9l6 6\"/>",
            ["edit"] = "<path d=\"M17 3a2.85 2.85 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5Z\"/>",
            ["undo"] = "<path d=\"M9 14 4 9l5-5\"/><path d=\"M4 9h10.5a5.5 5.5 0 0 1 0 11H11\"/>",
            ["chart"] = "<path d=\"M3 3v18h18\"/><path d=\"M18 17V9M13 17V5M8 17v-3\"/>",
            ["external"] = "<path d=\"M15 3h6v6M10 14 21 3M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6\"/>",
            ["download"] = "<path d=\"M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4M7 10l5 5 5-5M12 15V3\"/>",
            ["message"] = "<path d=\"M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z\"/>",
            ["chevron"] = "<path d=\"m9 18 6-6-6-6\"/>",
            ["arrow"] = "<path d=\"M7 17 17 7M7 7h10v10\"/>",
            ["up"] = "<path d=\"m6 15 6-6 6 6\"/>",
            ["down"] = "<path d=\"m6 9 6 6 6-6\"/>",
            ["clock"] = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 7v5l3 2\"/>",
            ["info"] = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 8h.01M11 12h1v5h1\"/>"
        };
        static readonly Dictionary<string, string> StageIcon = new Dictionary<string, string>
        {
            ["Approved"] = Ic["badge"], ["Rejected"] = Ic["reject"], ["Changes requested"] = Ic["edit"], ["Withdrawn"] = Ic["undo"]
        };
        static readonly string[] Hues = { "#7c5cff", "#2f9e6e", "#e07b39", "#d6457a", "#3b82f6", "#c9a227", "#14a3b8", "#9b59d0" };


        List<Req> mine = new List<Req>();
        public List<Req> Filtered = new List<Req>();
        public Req Active;
        public int PageNumber = 1, From, To;
        public string Tab = "overview", Summary, Headers, Rows, PageLinks, Nav, DetailHead, Overview, Activity, DetailNote;
        public string term, kind, status, sort;
        public int size = 20;
        public bool ShowNewForm;
        public IEnumerable<object> NewCourseOptions = new List<object>();

        public static string Draw(string paths) =>
            "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">" + paths + "</svg>";

        public void Load()
        {
            mine = Data.Requests.Where(r => r.RequesterId == Me.Id).Select(Enrich).OrderByDescending(r => r.Row.CreatedAt).ToList();
            ShowNewForm = Request.QueryString["new"] == "1";
            if (ShowNewForm) NewCourseOptions = CourseOptions(OwnCourses);
            term = (Request.QueryString["q"] ?? "").Trim();
            kind = Request.QueryString["kind"] == "Create" || Request.QueryString["kind"] == "Delete" ? Request.QueryString["kind"] : "";
            status = Terminal.Contains(Request.QueryString["status"]) || Request.QueryString["status"] == "Pending review" ? Request.QueryString["status"] : "";
            sort = SortKeys.Contains(Request.QueryString["sort"]) ? Request.QueryString["sort"] : "newest";
            size = new[] { 10, 50 }.Contains(QueryInt("size") ?? 20) ? QueryInt("size").Value : 20;
            Tab = Request.QueryString["tab"] == "activity" ? "activity" : "overview";

            Filtered = Sorted(mine.Where(r => (kind == "" || r.Row.Kind == kind) && (status == "" || r.Row.Status == status)
                && SearchMatch.Matches(term, r.Row.CourseTitle + " " + r.Row.Id + " " + r.Program + " " + r.Reviewer, r.Row.Reason + " " + r.NextStep))).ToList();
            var id = QueryInt("id");
            Active = Filtered.FirstOrDefault(r => r.Row.Id == id) ?? Filtered.FirstOrDefault();
            var pages = Math.Max(1, (Filtered.Count + size - 1) / size);
            var at = Active == null ? -1 : Filtered.IndexOf(Active);
            PageNumber = Math.Min(Math.Max(1, QueryInt("page") ?? (at < 0 ? 1 : at / size + 1)), pages);
            var pageRows = Filtered.Skip((PageNumber - 1) * size).Take(size).ToList();
            From = Filtered.Count == 0 ? 0 : (PageNumber - 1) * size + 1;
            To = Math.Min(Filtered.Count, PageNumber * size);

            Summary = string.Concat(new[] { new[] { "Pending review", "pending review" }, new[] { "Changes requested", "need changes" }, new[] { "Approved", "approved" }, new[] { "Rejected", "rejected" } }
                .Select((s, i) =>
                {
                    var n = mine.Count(r => r.Row.Status == s[0]);
                    return "<span data-tone=\"" + s[0].ToLowerInvariant().Replace(' ', '-') + "\">" + (i == 0 ? "<i></i>" : "") + "<b data-count=\"" + n + "\">" + n + "</b> " + s[1] + "</span>";
                })) + "<span><b data-count=\"" + mine.Count + "\">" + mine.Count + "</b> " + (mine.Count == 1 ? "submission" : "submissions") + "</span>";
            Headers = HeaderCell("Priority", "priority", "rq-priority-cell") + HeaderCell("Course / request", "name", "") + HeaderCell("Program", "program", "") + HeaderCell("Type", null, "")
                + HeaderCell("Activities", null, "rq-num-cell") + HeaderCell("Est. time", null, "rq-num-cell") + HeaderCell("Reviewer", null, "") + HeaderCell("Status", "status", "")
                + HeaderCell("Submitted", "date", "") + HeaderCell("Next step", null, "");
            Rows = pageRows.Count == 0
                ? "<tr><td colspan=\"11\"><div class=\"rq-empty\"><h2>No matching requests</h2><p>Your submitted course requests will appear here. Try another filter or create a request.</p></div></td></tr>"
                : string.Concat(pageRows.Select((r, i) => Row(r, i)));
            PageLinks = Pager(pages);

            if (Active == null)
            {
                Nav = "";
                return;
            }
            Func<Req, string, string, string, string> step = (other, label, direction, paths) => other == null
                ? "<span class=\"rq-nav-btn\" aria-disabled=\"true\">" + Draw(paths) + "</span>"
                : "<a class=\"rq-nav-btn\" href=\"" + System.Net.WebUtility.HtmlEncode(Url(id: other.Row.Id, tab: Tab)) + "\" data-nav=\"" + direction + "\" aria-label=\"" + label + "\">" + Draw(paths) + "</a>";
            Nav = "<span class=\"rq-nav-pos\">" + (at + 1) + " / " + Filtered.Count + "</span>" + step(at > 0 ? Filtered[at - 1] : null, "Previous request", "-1", "<path d=\"m15 6-6 6 6 6\"/>")
                + step(at + 1 < Filtered.Count ? Filtered[at + 1] : null, "Next request", "1", "<path d=\"m9 6 6 6-6 6\"/>");
            DetailHead = "<div class=\"rq-request-meta\"><h2>" + Ui.H(Active.Row.CourseTitle) + "</h2><p data-status=\"" + Active.Tone + "\">" + Ui.H(Active.Row.Status) + "</p><span class=\"rq-kind\">"
                + (Active.Row.Kind == "Delete" ? "Course deletion" : "Course creation / update") + "</span><small>Submitted " + Ui.Stamp(Active.Row.CreatedAt) + "</small></div>";
            Overview = OverviewPanel(Active);
            Activity = ActivityPanel(Active);
            DetailNote = Active.Course != null ? "Changes stay in draft until a decision is recorded." : "Course history is retained after deletion.";
        }

        Req Enrich(RequestRow r)
        {
            var course = Data.Course(r.CourseId);
            var program = r.ProgramId == null ? null : Data.Program(r.ProgramId.Value);
            var leader = Data.User(program?.LeaderId);
            var lessons = course == null ? new List<LessonRow>() : Data.LessonsOf(course.Id);
            string next;
            return new Req
            {
                Row = r, Course = course, Leader = leader, Program = program?.Name ?? "Former program", Reviewer = leader?.Name ?? "Program Leader",
                Priority = r.Status == "Pending review" ? 3 : 2, Activities = lessons.Count,
                Hours = Math.Round(lessons.Sum(l => l.Minutes) / 6.0, MidpointRounding.AwayFromZero) / 10,
                NextStep = NextFor.TryGetValue(r.Status, out next) ? next : "Review request"
            };
        }

        /// <summary>
        /// The sort values: a column's own name sorts in its usual direction (Course, Program and Status A to Z, Priority high to low,
        /// Submitted newest first as "newest") and "-rev" reverses it. Clicking the same column heading again switches between the two.
        /// </summary>
        static readonly string[] SortKeys = { "newest", "oldest", "name", "name-rev", "priority", "priority-rev", "program", "program-rev", "status", "status-rev" };

        IEnumerable<Req> Sorted(IEnumerable<Req> rows)
        {
            var text = StringComparer.CurrentCultureIgnoreCase;
            var reversed = sort.EndsWith("-rev", StringComparison.Ordinal);
            IEnumerable<Req> ordered;
            switch (reversed ? sort.Substring(0, sort.Length - 4) : sort)
            {
                case "priority": ordered = rows.OrderByDescending(r => r.Priority).ThenByDescending(r => r.Row.CreatedAt); break;
                case "program": ordered = rows.OrderBy(r => r.Program, text).ThenByDescending(r => r.Row.CreatedAt); break;
                case "status": ordered = rows.OrderBy(r => r.Row.Status, text).ThenByDescending(r => r.Row.CreatedAt); break;
                case "name": ordered = rows.OrderBy(r => r.Row.CourseTitle, text).ThenByDescending(r => r.Row.CreatedAt); break;
                case "oldest": ordered = rows.OrderBy(r => r.Row.CreatedAt); break;
                default: ordered = rows.OrderByDescending(r => r.Row.CreatedAt); break;
            }
            return reversed ? ordered.Reverse() : ordered;
        }

        /// <summary>This page's address with the current filters, changing only what is passed.</summary>
        public string Url(int? id = null, int? page = null, string sortBy = null, string tab = null)
        {
            var query = new List<string>();
            Action<string, string> add = (key, value) => { if (!string.IsNullOrEmpty(value)) query.Add(key + "=" + System.Net.WebUtility.UrlEncode(value)); };
            add("id", id?.ToString(CultureInfo.InvariantCulture));
            add("q", term);
            add("kind", kind);
            add("status", status);
            var s = sortBy ?? sort;
            add("sort", s == "newest" ? null : s);
            add("size", size == 20 ? null : size.ToString(CultureInfo.InvariantCulture));
            add("page", page?.ToString(CultureInfo.InvariantCulture));
            add("tab", tab == "activity" ? tab : null);
            return "/CourseRequests" + (query.Count == 0 ? "" : "?" + string.Join("&", query));
        }

        string HeaderCell(string label, string key, string cls)
        {
            if (key == null) return "<th scope=\"col\" class=\"" + cls + "\">" + label + "</th>";
            var reversed = sort.EndsWith("-rev", StringComparison.Ordinal);
            var on = (reversed ? sort.Substring(0, sort.Length - 4) : sort) == key || key == "date" && (sort == "newest" || sort == "oldest");
            // Priority runs high to low until reversed; the other columns run A to Z (oldest to newest for dates) until reversed.
            var descending = key == "date" ? sort == "newest" : key == "priority" ? !reversed : reversed;
            var direction = on ? (descending ? "descending" : "ascending") : "none";
            // Clicking the sorted column again flips it; clicking another column starts it in its usual direction.
            var target = key == "date" ? (sort == "newest" ? "oldest" : "newest") : on ? (reversed ? key : key + "-rev") : key;
            return "<th scope=\"col\" class=\"" + cls + "\" data-sort-key=\"" + key + "\"" + (on ? " data-sorted" : "") + " aria-sort=\"" + direction + "\"><a class=\"rq-sort-head\" href=\""
                + System.Net.WebUtility.HtmlEncode(Url(sortBy: target)) + "\">" + label + Draw("<path class=\"rq-arrow-up\" d=\"m8 9 4-4 4 4\"/><path class=\"rq-arrow-down\" d=\"M8 15l4 4 4-4\"/>") + "</a></th>";
        }

        static string Hue(string name)
        {
            uint n = 3;
            foreach (var c in name ?? "") n = unchecked(n * 31 + c);
            return Hues[n % Hues.Length];
        }

        static string Face(string name) => Ui.H(string.Concat((name ?? "?").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => w[0])));

        static string PriorityMark(int level) => level >= 4
            ? "<span class=\"rq-priority rq-urgent\" title=\"Urgent priority\">" + Draw("<path d=\"M12 7v6M12 17h.01\"/>") + "<span class=\"sr-only\">Urgent</span></span>"
            : "<span class=\"rq-priority\" data-level=\"" + level + "\" title=\"" + PriorityName[level] + " priority\"><i></i><i></i><i></i><i></i><span class=\"sr-only\">" + PriorityName[level] + "</span></span>";

        string Row(Req r, int i)
        {
            var selected = r == Active;
            return "<tr class=\"rq-request" + (selected ? " rq-selected" : "") + "\" data-request-id=\"" + r.Row.Id + "\" aria-selected=\"" + (selected ? "true" : "false") + "\" style=\"--i:" + Math.Min(i, 24) + "\">"
                + "<td class=\"rq-check-cell\"><input class=\"rq-row-check\" type=\"checkbox\" name=\"rq-select\" value=\"" + r.Row.Id + "\" aria-label=\"Select " + Ui.H(r.Row.CourseTitle) + "\"></td>"
                + "<td class=\"rq-priority-cell\">" + PriorityMark(r.Priority) + "</td>"
                + "<td class=\"rq-name-cell\"><a href=\"" + System.Net.WebUtility.HtmlEncode(Url(id: r.Row.Id, page: PageNumber, tab: Tab)) + "\"" + (selected ? " aria-current=\"true\"" : "") + ">" + Ui.H(r.Row.CourseTitle) + "</a></td>"
                + "<td class=\"rq-program-cell\"><span class=\"rq-program-icon\" style=\"--rq-hue:" + Hue(r.Program) + "\" aria-hidden=\"true\">" + Ui.H(r.Program.Substring(0, 1)) + "</span>" + Ui.H(r.Program) + "</td>"
                + "<td><span class=\"rq-type-tag\" data-kind=\"" + Ui.H(r.Row.Kind) + "\">" + (r.Row.Kind == "Delete" ? "Deletion" : "Create / update") + "</span></td>"
                + "<td class=\"rq-num-cell\">" + r.Activities + "</td><td class=\"rq-num-cell\">" + r.Hours.ToString("0.0", CultureInfo.InvariantCulture) + " h</td>"
                + "<td class=\"rq-reviewer-cell\"><span class=\"rq-reviewer-face\" aria-hidden=\"true\">" + Face(r.Reviewer) + "</span>" + Ui.H(r.Reviewer) + "</td>"
                + "<td><span class=\"rq-state\" data-status=\"" + r.Tone + "\">" + Ui.H(r.Row.Status) + "</span></td>"
                + "<td class=\"rq-date-cell\"><time datetime=\"" + Workspace.Iso(r.Row.CreatedAt) + "\" title=\"" + Ui.Stamp(r.Row.CreatedAt) + "\">" + r.Row.CreatedAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + "</time></td>"
                + "<td class=\"rq-next-cell\" title=\"" + Ui.H(r.NextStep) + "\">" + Ui.H(r.NextStep) + "</td></tr>";
        }

        string Pager(int pages)
        {
            var numbers = new List<int?>();
            for (int n = 1; n <= pages; n++)
                if (n == 1 || n == pages || Math.Abs(n - PageNumber) <= 1) numbers.Add(n);
                else if (numbers.Count == 0 || numbers[numbers.Count - 1] != null) numbers.Add(null);
            Func<int, string, string, string> stepLink = (target, label, paths) => target < 1 || target > pages
                ? "<span class=\"rq-page-step\" aria-disabled=\"true\" aria-label=\"" + label + "\">" + Draw(paths) + "</span>"
                : "<a class=\"rq-page-step\" href=\"" + System.Net.WebUtility.HtmlEncode(Url(id: Active?.Row.Id, page: target, tab: Tab)) + "\" aria-label=\"" + label + "\">" + Draw(paths) + "</a>";
            return stepLink(PageNumber - 1, "Previous page", "<path d=\"m15 6-6 6 6 6\"/>")
                + "<div class=\"rq-page-numbers\"><span class=\"rq-pager-glider\" aria-hidden=\"true\"></span>"
                + string.Concat(numbers.Select(n => n == null ? "<span class=\"rq-page-gap\">&hellip;</span>"
                    : "<a class=\"rq-page-num\" href=\"" + System.Net.WebUtility.HtmlEncode(Url(id: Active?.Row.Id, page: n, tab: Tab)) + "\"" + (n == PageNumber ? " aria-current=\"page\"" : "") + ">" + n + "</a>"))
                + "</div>" + stepLink(PageNumber + 1, "Next page", "<path d=\"m9 6 6 6-6 6\"/>");
        }

        static double DaysInReview(Req r)
        {
            var decision = Terminal.Contains(r.Row.Status) ? r.Row.Events.FirstOrDefault(x => x.Action == r.Row.Status)?.CreatedAt ?? r.Row.CreatedAt : DateTime.Now;
            return Math.Max(0, (decision - r.Row.CreatedAt).TotalDays);
        }

        string OverviewPanel(Req r)
        {
            Func<string, double, double, string, bool, string> metric = (label, value, mean, unit, lowerIsBetter) =>
            {
                var diff = value - mean;
                var up = diff >= 0;
                var good = lowerIsBetter ? !up : up;
                Func<double, string> fmt = n => (Math.Round(n * 10, MidpointRounding.AwayFromZero) / 10).ToString(CultureInfo.InvariantCulture) + unit;
                return "<div class=\"rq-metric\"><span>" + label + "</span><strong>" + fmt(value) + "</strong><small data-trend=\"" + (Math.Abs(diff) < .05 ? "flat" : good ? "good" : "bad")
                    + "\"><i>" + Draw(Ic[up ? "up" : "down"]) + "</i>" + fmt(mean) + " avg</small></div>";
            };
            var pool = mine.Count > 0 ? mine : new List<Req> { r };
            var html = new StringBuilder("<section class=\"rq-group\"><h3 class=\"rq-group-title\">Key metrics</h3><div class=\"rq-group-body rq-metrics\">");
            html.Append(metric("Activities", r.Activities, pool.Average(x => x.Activities), "", false));
            html.Append(metric("Est. time", r.Hours, pool.Average(x => x.Hours), " h", false));
            html.Append(metric("Days in review", DaysInReview(r), pool.Average(DaysInReview), " d", true));
            html.Append(metric("Review events", r.Row.Events.Count, pool.Average(x => x.Row.Events.Count), "", false));
            html.Append("</div></section>");

            Func<string, string, string, string, string> action = (icon, label, href, cls) =>
                "<a class=\"rq-action " + cls + "\" href=\"" + System.Net.WebUtility.HtmlEncode(href) + "\"><span class=\"rq-action-icon\">" + Draw(Ic[icon]) + "</span><span class=\"rq-action-label\">" + label
                + "</span><span class=\"rq-action-chevron\">" + Draw(Ic["chevron"]) + "</span></a>";
            html.Append("<section class=\"rq-group\"><h3 class=\"rq-group-title\">Quick actions</h3><div class=\"rq-group-body rq-actions\">");
            if (r.Course != null && r.Course.InstructorId == Me.Id) html.Append(action("chart", "Open course", "CourseEditor?id=" + r.Course.Id, "rq-open-course"));
            // The public course page only opens for a published course; for any other status "Open course" above is the way in.
            if (r.Course != null && r.Course.Status == "Published") html.Append(action("external", "View course page", "Course?id=" + r.Course.Id, ""));
            html.Append(action("clock", "Show activity history", Url(id: r.Row.Id, page: PageNumber, tab: "activity"), ""));
            html.Append(action("download", "Export this request", "CourseRequests?export=" + r.Row.Id, ""));
            html.Append(action("message", "Message " + Ui.H(r.Reviewer.Split(' ')[0]), r.Leader == null ? "Messages" : "Messages?id=" + r.Leader.Id, ""));
            html.Append("</div></section>");

            html.Append("<section class=\"rq-group\"><h3 class=\"rq-group-title\">Metadata</h3><dl class=\"rq-group-body rq-meta-list\">");
            html.Append("<div><dt>Request ID</dt><dd>REQ-").Append(r.Row.Id).Append("</dd></div>");
            html.Append("<div><dt>Program</dt><dd><span class=\"rq-program-icon\" style=\"--rq-hue:").Append(Hue(r.Program)).Append("\" aria-hidden=\"true\">").Append(Ui.H(r.Program.Substring(0, 1))).Append("</span>").Append(Ui.H(r.Program)).Append("</dd></div>");
            html.Append("<div><dt>Type</dt><dd>").Append(r.Row.Kind == "Delete" ? "Deletion" : "Create / update").Append("</dd></div>");
            html.Append("<div><dt>Priority</dt><dd>").Append(PriorityMark(r.Priority)).Append(" ").Append(PriorityName[r.Priority]).Append("</dd></div>");
            html.Append("<div><dt>Reviewer</dt><dd><span class=\"rq-reviewer-face\" aria-hidden=\"true\">").Append(Face(r.Reviewer)).Append("</span>").Append(Ui.H(r.Reviewer)).Append("</dd></div>");
            html.Append("<div><dt>Next step</dt><dd>").Append(Ui.H(r.NextStep)).Append("</dd></div>");
            html.Append("<div><dt>Submitted</dt><dd>").Append(Ui.Stamp(r.Row.CreatedAt)).Append("</dd></div></dl></section>");
            return html.ToString();
        }

        string ActivityPanel(Req r)
        {
            var messages = new Dictionary<string, string>
            {
                ["Pending review"] = "This request is pending review. A Program Leader will review the submitted course changes.",
                ["Approved"] = "This request was approved. The recorded decision and feedback are shown below.",
                ["Rejected"] = "This request was rejected. Review the feedback before preparing another submission.",
                ["Changes requested"] = "Changes were requested. Review the feedback and update your course.",
                ["Withdrawn"] = "This request was withdrawn. Its activity is retained for your records."
            };
            string message;
            var html = new StringBuilder("<div class=\"rq-status-notice\" data-status=\"" + r.Tone + "\"><span aria-hidden=\"true\">"
                + Draw(Ic[r.Row.Status == "Approved" ? "badge" : r.Row.Status == "Pending review" ? "clock" : "info"]) + "</span><p>"
                + Ui.H(messages.TryGetValue(r.Row.Status, out message) ? message : r.Row.Status) + "</p></div>");

            // Review progress: submitted, seen by the leader, then the decision.
            var submitted = r.Row.Events.FirstOrDefault(x => x.Action == "Submitted");
            var seen = r.Row.Events.FirstOrDefault(x => x.Action == "Review started" || x.Action == "Seen");
            var terminal = Terminal.Contains(r.Row.Status);
            var decision = terminal ? r.Row.Events.FirstOrDefault(x => x.Action == r.Row.Status) : null;
            var steps = new[]
            {
                new { Key = "submitted", Label = "Submitted for review", When = (DateTime?)(submitted?.CreatedAt ?? r.Row.CreatedAt), State = "done", Icon = Ic["send"] },
                new { Key = "seen", Label = "Seen", When = seen?.CreatedAt, State = seen != null ? "done" : terminal ? "unrecorded" : "current", Icon = Ic["eye"] },
                new { Key = "approved", Label = terminal ? r.Row.Status : "Approved", When = decision?.CreatedAt, State = terminal ? "done" : seen != null ? "current" : "waiting", Icon = StageIcon[terminal ? r.Row.Status : "Approved"] }
            };
            html.Append("<section class=\"rq-review-progress\" aria-label=\"Review progress\"><h3>Review progress</h3><ol>");
            foreach (var s in steps)
            {
                html.Append("<li data-stage=\"").Append(s.Key).Append("\" data-state=\"").Append(s.State).Append("\"");
                if (terminal && s.Key == "approved") html.Append(" data-outcome=\"").Append(r.Tone).Append("\"");
                if (s.State == "current") html.Append(" aria-current=\"step\"");
                html.Append("><span class=\"rq-stage-dot\" aria-hidden=\"true\">").Append(Draw(s.Icon)).Append("</span><strong>").Append(Ui.H(s.Label)).Append("</strong><small>")
                    .Append(s.When != null ? Ui.Day(s.When) : s.State == "unrecorded" ? "Not recorded" : s.Key == "seen" ? "Awaiting review" : "Awaiting decision").Append("</small></li>");
            }
            html.Append("</ol></section>");

            html.Append("<details class=\"rq-tracker\" open><summary>Activity history<span>").Append(r.Row.Events.Count).Append("</span></summary><ol>");
            foreach (var x in r.Row.Events)
            {
                var actor = Data.User(x.ActorId);
                var title = x.Action == "Submitted" ? "Request submitted for review" : x.Action == "Review started" ? "Seen" : x.Action == "Rejected" ? "Request rejected"
                    : x.Action == "Changes requested" ? "Course changes requested" : x.Action == "Withdrawn" ? "Request withdrawn" : x.Action;
                string icon;
                if (x.Action == "Submitted") icon = Ic["send"];
                else if (x.Action == "Review started") icon = Ic["eye"];
                else if (!StageIcon.TryGetValue(x.Action, out icon)) icon = Ic["info"];
                var face = string.IsNullOrEmpty(actor?.Avatar) ? Face(actor?.Name) : "<img src=\"" + Ui.H(actor.Avatar) + "\" alt=\"\">";
                html.Append("<li data-event-action=\"").Append(Ui.H(x.Action)).Append("\"><span class=\"rq-event-icon\" data-action-kind=\"").Append(Ui.H(x.Action)).Append("\">").Append(Draw(icon))
                    .Append("</span><div class=\"rq-event-heading\"><strong>").Append(Ui.H(title)).Append("</strong><time datetime=\"").Append(Workspace.Iso(x.CreatedAt)).Append("\">").Append(Ui.Stamp(x.CreatedAt))
                    .Append("</time></div><div class=\"rq-event-card\"><dl><div><dt>").Append(x.Action == "Submitted" ? "Submitted by" : "Updated by").Append("</dt><dd class=\"rq-event-author\"><span class=\"rq-person-avatar\" aria-hidden=\"true\">")
                    .Append(face).Append("</span><span>").Append(Ui.H(actor?.Name ?? "Former user")).Append("</span></dd></div><div><dt>Course</dt><dd><span class=\"rq-data-tag\">").Append(Ui.H(r.Row.CourseTitle))
                    .Append("</span><span class=\"rq-data-tag\">").Append(Ui.H(r.Program)).Append("</span></dd></div><div><dt>").Append(x.Action == "Submitted" ? "Context" : "Feedback")
                    .Append("</dt><dd><p class=\"rq-event-note\">").Append(Ui.H(string.IsNullOrEmpty(x.Comment) ? x.Action : x.Comment)).Append("</p></dd></div></dl>");
                // The course editor shows the submitted details at any status (read-only while the review is pending); the public page
                // only opens for a published course.
                if (x.Action == "Submitted" && r.Course != null)
                    html.Append("<footer><a href=\"").Append(r.Course.InstructorId == Me.Id ? "/CourseEditor?id=" : "/Course?id=").Append(r.Course.Id).Append("\">View course details ").Append(Draw(Ic["arrow"])).Append("</a></footer>");
                html.Append("</div></li>");
            }
            html.Append("</ol>");
            if (r.Row.Status == "Pending review") html.Append("<p class=\"rq-awaiting\">Awaiting Program Leader decision</p>");
            html.Append("</details>");
            return html.ToString();
        }
        /// <summary>The CSV download: the ticked requests, or every request that matches the filters when none is ticked.</summary>
        public IEnumerable<IEnumerable<object>> ExportRows(ICollection<string> ticked)
        {
            var chosen = Filtered.Where(r => ticked.Contains(r.Row.Id.ToString(CultureInfo.InvariantCulture))).ToList();
            return new[] { new object[] { "Course", "Type", "Program", "Status", "Submitted (UTC)", "Reason" } }
                .Concat((chosen.Count > 0 ? chosen : Filtered).Select(r => new object[] { r.Row.CourseTitle, r.Row.Kind, r.Program, r.Row.Status, Workspace.Iso(r.Row.CreatedAt), r.Row.Reason }));
        }

    }
}
