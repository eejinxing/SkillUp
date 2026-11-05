using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SkillUp.Services;

namespace SkillUp.Models
{
    /// <summary>
    /// Admin: every account as cards or a list, filtered by search, role and status (?q=, ?role=, ?status=, ?sort=,
    /// ?view=grid|list, ?limit=), with a role network and recent workspace activity. ?actions=user opens that account's actions.
    /// </summary>
    public sealed class UsersModel : PageModel
    {
        public UsersModel(SkillContext ctx) : base(ctx) { }
        public IEnumerable<dynamic> RoleFacets, StatusFacets;
        public bool HasFilters, IsEmpty, HasMore;

        const int PageSize = 60;
        static readonly string[] RoleNames = { "Learner", "Instructor", "Program Leader", "Admin" };
        static readonly string[] Statuses = { "Active", "Pending", "Inactive" };
        static readonly Dictionary<string, string> RoleKey = new Dictionary<string, string> { ["Learner"] = "learner", ["Instructor"] = "instructor", ["Program Leader"] = "leader", ["Admin"] = "admin" };
        static readonly Dictionary<string, string> Ic = new Dictionary<string, string>
        {
            ["learner"] = "<path d=\"M22 10 12 5 2 10l10 5 10-5Z\"/><path d=\"M6 12v5c3 2 9 2 12 0v-5\"/>",
            ["instructor"] = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"12\" rx=\"2\"/><path d=\"M8 20h8M12 16v4\"/>",
            ["leader"] = "<path d=\"M12 3 4 7v5c0 5 3.5 8 8 9 4.5-1 8-4 8-9V7z\"/>",
            ["admin"] = "<circle cx=\"12\" cy=\"12\" r=\"3\"/><path d=\"M19.4 15a1.7 1.7 0 0 0 .3 1.8l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.7 1.7 0 0 0-1.8-.3 1.7 1.7 0 0 0-1 1.5V21a2 2 0 1 1-4 0v-.1a1.7 1.7 0 0 0-1.1-1.5 1.7 1.7 0 0 0-1.8.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.7 1.7 0 0 0 .3-1.8 1.7 1.7 0 0 0-1.5-1H3a2 2 0 1 1 0-4h.1a1.7 1.7 0 0 0 1.5-1.1 1.7 1.7 0 0 0-.3-1.8l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.7 1.7 0 0 0 1.8.3H9a1.7 1.7 0 0 0 1-1.5V3a2 2 0 1 1 4 0v.1a1.7 1.7 0 0 0 1 1.5 1.7 1.7 0 0 0 1.8-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.7 1.7 0 0 0-.3 1.8V9a1.7 1.7 0 0 0 1.5 1H21a2 2 0 1 1 0 4h-.1a1.7 1.7 0 0 0-1.5 1Z\"/>",
            ["grid"] = "<rect x=\"4\" y=\"4\" width=\"7\" height=\"7\" rx=\"1.5\"/><rect x=\"13\" y=\"4\" width=\"7\" height=\"7\" rx=\"1.5\"/><rect x=\"4\" y=\"13\" width=\"7\" height=\"7\" rx=\"1.5\"/><rect x=\"13\" y=\"13\" width=\"7\" height=\"7\" rx=\"1.5\"/>",
            ["list"] = "<path d=\"M9 6h11M9 12h11M9 18h11\"/><circle cx=\"4.5\" cy=\"6\" r=\"1\"/><circle cx=\"4.5\" cy=\"12\" r=\"1\"/><circle cx=\"4.5\" cy=\"18\" r=\"1\"/>",
            ["search"] = "<circle cx=\"11\" cy=\"11\" r=\"6\"/><path d=\"m20 20-4.2-4.2\"/>",
            ["sort"] = "<path d=\"M7 4v16M4 17l3 3 3-3M17 20V4M14 7l3-3 3 3\"/>",
            ["chevron"] = "<path d=\"m6 9 6 6 6-6\"/>",
            ["check"] = "<path d=\"m5 12 4 4L19 6\"/>",
            ["more"] = "<circle cx=\"5\" cy=\"12\" r=\"1.2\"/><circle cx=\"12\" cy=\"12\" r=\"1.2\"/><circle cx=\"19\" cy=\"12\" r=\"1.2\"/>",
            ["back"] = "<path d=\"m15 6-6 6 6 6\"/>",
            ["arrow"] = "<path d=\"M7 17 17 7M7 7h10v10\"/>"
        };
        static readonly int[][] Slots =
        {
            new[] { 9, 27 }, new[] { 31, 48 }, new[] { 56, 22 }, new[] { 83, 48 }, new[] { 14, 79 }, new[] { 55, 83 }, new[] { 86, 80 }, new[] { 38, 16 }, new[] { 71, 16 },
            new[] { 39, 78 }, new[] { 69, 68 }, new[] { 13, 51 }, new[] { 65, 43 }, new[] { 29, 82 }, new[] { 88, 22 }, new[] { 48, 60 }, new[] { 23, 16 }, new[] { 77, 84 }
        };


        public string View = "grid", Network, Mix, Cards;
        public int Shown, Total;
        public UserRow Target;
        string term, role, status, sort;
        int limit;

        public static string Svg(string name) =>
            "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">" + Ic[name] + "</svg>";

        static string Initials(string name) => string.Concat((name ?? "?").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => w[0])).ToUpperInvariant();

        public void Load()
        {
            var q = Request.QueryString;
            term = (q["q"] ?? "").Trim();
            role = RoleNames.Contains(q["role"]) ? q["role"] : "";
            status = Statuses.Contains(q["status"]) ? q["status"] : "";
            sort = new[] { "name-desc", "role", "status" }.Contains(q["sort"]) ? q["sort"] : "name";
            View = q["view"] == "list" ? "list" : "grid";
            limit = Math.Max(PageSize, Math.Min(100000, QueryInt("limit") ?? PageSize));
            RoleFacets = RoleNames.Select(r => new { Role = r, Key = RoleKey[r], Icon = Svg(RoleKey[r]), Count = Data.Users.Count(u => u.Role == r), Pressed = r == role ? "true" : "false" }).ToList();
            StatusFacets = Statuses.Select(s => new { Status = s, Key = s.ToLowerInvariant(), Count = Data.Users.Count(u => u.Status == s), Pressed = s == status ? "true" : "false" }).ToList();
            HasFilters = term.Length > 0 || role.Length > 0 || status.Length > 0;

            var users = Data.Users.Where(u => (role == "" || u.Role == role) && (status == "" || u.Status == status)
                && SearchMatch.Matches(term, string.Join(" ", u.Name, u.Username, u.Id.ToString(CultureInfo.InvariantCulture)), u.Email));
            var byName = StringComparer.CurrentCultureIgnoreCase;
            var sorted = (sort == "name-desc" ? users.OrderByDescending(u => u.Name, byName)
                : sort == "role" ? users.OrderBy(u => Array.IndexOf(RoleNames, u.Role)).ThenBy(u => u.Name, byName)
                : sort == "status" ? users.OrderBy(u => Array.IndexOf(Statuses, u.Status)).ThenBy(u => u.Name, byName)
                : users.OrderBy(u => u.Name, byName)).ToList();
            Total = sorted.Count;
            Shown = Math.Min(limit, Total);
            Cards = string.Concat(sorted.Take(limit).Select(Card));
            IsEmpty = Total == 0;
            HasMore = Total > limit;

            var learners = Data.Users.Count(u => u.Role == "Learner");
            var share = Ui.Percent(learners, Data.Users.Count);
            Mix = "<div class=\"au-mix\"><div class=\"au-mix-labels\"><span>Learners</span><span>Staff</span></div><div class=\"au-mix-bar\" style=\"--v:" + share + "\"><i></i><b>" + share
                + "%</b></div><div class=\"au-mix-labels au-mix-values\"><span>" + learners + "</span><span>" + (Data.Users.Count - learners) + "</span></div></div>";
            Network = BuildNetwork();

            Target = Data.User(QueryInt("actions"));
        }

        /// <summary>Two role-specific figures per account.</summary>
        string[][] Stats(UserRow u)
        {
            if (u.Role == "Learner")
            {
                var enrolled = Data.Enrollments.Where(x => x.UserId == u.Id).ToList();
                var progress = Charts.Avg(enrolled.Select(x => Progress(u.Id, x.CourseId)));
                return new[] { new[] { "Enrolled", Ui.Plural(enrolled.Count, "course") }, new[] { "Avg progress", progress + "%", progress.ToString(CultureInfo.InvariantCulture) } };
            }
            if (u.Role == "Instructor")
            {
                var taught = new HashSet<int>(Data.Courses.Where(c => c.InstructorId == u.Id).Select(c => c.Id));
                var learners = Data.Enrollments.Where(x => taught.Contains(x.CourseId)).Select(x => x.UserId).Distinct().Count();
                return new[] { new[] { "Teaching", Ui.Plural(taught.Count, "course") }, new[] { "Learners", learners.ToString(CultureInfo.InvariantCulture) } };
            }
            if (u.Role == "Program Leader")
            {
                var led = new HashSet<int>(Data.Programs.Where(p => p.LeaderId == u.Id).Select(p => p.Id));
                return new[] { new[] { "Programs", led.Count.ToString(CultureInfo.InvariantCulture) }, new[] { "Courses", Data.Courses.Count(c => led.Contains(c.ProgramId)).ToString(CultureInfo.InvariantCulture) } };
            }
            return new[] { new[] { "Access", "All areas" }, new[] { "Audit entries", Data.Audit.Count(a => a.UserId == u.Id).ToString(CultureInfo.InvariantCulture) } };
        }

        string Card(UserRow u, int i)
        {
            string key;
            if (!RoleKey.TryGetValue(u.Role ?? "", out key)) key = "learner";
            var stats = Stats(u);
            var face = string.IsNullOrEmpty(u.Avatar) ? "<b>" + Ui.H(Initials(u.Name)) + "</b>" : "<img src=\"" + Ui.H(u.Avatar) + "\" alt=\"\">";
            return "<article class=\"au-card\" data-user-id=\"" + u.Id + "\" data-role=\"" + key + "\" style=\"--i:" + Math.Min(i, 24) + "\"><a class=\"au-user\" href=\"/UserEditor?id=" + u.Id + "\" aria-label=\"View "
                + Ui.H(u.Name) + ", " + Ui.H(u.Role) + ", " + Ui.H(u.Status) + "\"><span class=\"au-media\"><span class=\"au-role-tag\">" + Svg(key) + Ui.H(u.Role) + "</span><span class=\"au-avatar\">" + face
                + "</span><span class=\"au-handle\">@" + Ui.H(string.IsNullOrEmpty(u.Username) ? u.Id.ToString(CultureInfo.InvariantCulture) : u.Username) + "</span><span class=\"au-open-hint\">View profile" + Svg("arrow")
                + "</span></span><span class=\"au-user-copy\"><strong title=\"" + Ui.H(u.Name) + "\">" + Ui.H(u.Name) + "</strong><span class=\"au-email\">" + Ui.H(u.Email) + "</span><span class=\"au-status-line\"><span class=\"au-status au-status-"
                + Ui.H((u.Status ?? "").ToLowerInvariant()) + "\" title=\"" + Ui.H(u.Status) + "\" aria-hidden=\"true\"></span>" + Ui.H(u.Status) + "<span class=\"au-role-text\">" + Ui.H(u.Role) + "</span></span></span>"
                + "<span class=\"au-stats\"><span><small>" + stats[0][0] + "</small><b>" + Ui.H(stats[0][1]) + "</b></span><span><small>" + stats[1][0] + "</small><b>" + Ui.H(stats[1][1]) + "</b>"
                + (stats[1].Length > 2 ? "<i class=\"au-meter\" style=\"--v:" + stats[1][2] + "\"></i>" : "") + "</span></span></a><a class=\"au-user-menu\" href=\"" + System.Net.WebUtility.HtmlEncode(ListUrl(actions: u.Id))
                + "\" aria-label=\"Actions for " + Ui.H(u.Name) + "\">" + Svg("more") + "</a></article>";
        }

        /// <summary>The first 18 accounts placed on a map, joined to the previous account of the same role, and 42 days of recorded actions.</summary>
        string BuildNetwork()
        {
            var users = Data.Users;
            var shown = users.Take(18).ToList();
            var active = users.Count(u => u.Status == "Active");
            var nodes = shown.Select((u, i) => new { U = u, X = Slots[i][0], Y = Slots[i][1], Role = RoleKey.ContainsKey(u.Role ?? "") ? RoleKey[u.Role] : "learner" }).ToList();
            var paths = new StringBuilder();
            foreach (var r in RoleNames)
            {
                var group = nodes.Where(n => n.U.Role == r).ToList();
                for (int i = 1; i < group.Count; i++)
                {
                    var a = group[i - 1];
                    var b = group[i];
                    var mx = (a.X + b.X) / 2.0;
                    paths.Append("<path d=\"M").Append(a.X).Append(' ').Append(a.Y).Append(" C").Append(mx.ToString(CultureInfo.InvariantCulture)).Append(' ').Append(a.Y + 24).Append(' ')
                        .Append(mx.ToString(CultureInfo.InvariantCulture)).Append(' ').Append(b.Y - 24).Append(' ').Append(b.X).Append(' ').Append(b.Y).Append("\"/>");
                }
            }
            var today = DateTime.Now.Date;
            var days = Enumerable.Range(0, 42).Select(i => today.AddDays(i - 41)).ToList();
            var counts = days.Select(d => Data.Audit.Count(a => a.CreatedAt >= d && a.CreatedAt < d.AddDays(1))).ToList();
            var max = Math.Max(1, counts.Max());
            var total = counts.Sum();
            Func<DateTime, string> fmt = d => d.ToString("MMM d", CultureInfo.InvariantCulture);
            var html = new StringBuilder("<section class=\"au-diary\" aria-labelledby=\"au-diary-title\"><header class=\"au-diary-head\"><h2 id=\"au-diary-title\">USER<br>NETWORK</h2><div class=\"au-diary-summary\"><span><b>")
                .Append(active).Append("</b> active</span><span><b>").Append(users.Count).Append("</b> accounts</span></div></header><div class=\"au-diary-map\"><svg class=\"au-diary-paths\" viewBox=\"0 0 100 100\" preserveAspectRatio=\"none\" aria-hidden=\"true\">")
                .Append(paths).Append("</svg>");
            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                html.Append("<a class=\"au-diary-person\" data-role=\"").Append(n.Role).Append("\" data-status=\"").Append(Ui.H((n.U.Status ?? "").ToLowerInvariant())).Append("\" href=\"/UserEditor?id=").Append(n.U.Id)
                    .Append("\" style=\"--x:").Append(n.X).Append("%;--y:").Append(n.Y).Append("%;--i:").Append(i).Append("\" aria-label=\"View ").Append(Ui.H(n.U.Name)).Append(", ").Append(Ui.H(n.U.Role)).Append(", ")
                    .Append(Ui.H(n.U.Status)).Append("\"><span class=\"au-diary-caption\">").Append(Ui.H(n.U.Name)).Append(" <i></i></span><span class=\"au-diary-face\">")
                    .Append(string.IsNullOrEmpty(n.U.Avatar) ? "<b>" + Ui.H(Initials(n.U.Name)) + "</b>" : "<img src=\"" + Ui.H(n.U.Avatar) + "\" alt=\"\">").Append("</span><span class=\"au-diary-role\">").Append(Ui.H(n.U.Role)).Append("</span></a>");
            }
            if (nodes.Count == 0) html.Append("<p class=\"au-diary-empty\">Your workspace network starts with your first account.</p>");
            html.Append("</div><footer class=\"au-diary-timeline\"><div class=\"au-diary-timeline-head\"><span>WORKSPACE ACTIVITY <b>").Append(total).Append("</b></span><small>Connections by role")
                .Append(users.Count > shown.Count ? " &middot; Showing " + shown.Count + " of " + users.Count : "").Append("</small></div><div class=\"au-diary-bars\" role=\"img\" aria-label=\"").Append(total)
                .Append(" recorded actions over the last 42 days\">");
            for (int i = 0; i < counts.Count; i++)
                html.Append("<span style=\"--height:").Append(counts[i] > 0 ? Math.Max(12, counts[i] * 100.0 / max).ToString("0.##", CultureInfo.InvariantCulture) : "5").Append("%\" class=\"")
                    .Append(counts[i] > 0 ? "has-activity" : "").Append("\"><i></i><span class=\"sr-only\">").Append(fmt(days[i])).Append(": ").Append(counts[i]).Append(" actions. </span></span>");
            html.Append("</div><div class=\"au-diary-dates\"><span>").Append(fmt(days[0])).Append("</span><span>Last 6 weeks</span><span>Today</span></div></footer></section>");
            return html.ToString();
        }

        /// <summary>This page's address with the current filters, changing only what is passed.</summary>
        public string RoleFacetUrl(string picked) => ListUrl(q: term, r: role == picked ? "" : picked, max: PageSize);
        public string StatusFacetUrl(string picked) => ListUrl(q: term, s: status == picked ? "" : picked, max: PageSize);
        public string ViewUrl(string view) => ListUrl(view: view);
        public string ClearUrl => ListUrl(q: "", r: "", s: "", max: PageSize);
        public string MoreUrl => ListUrl(max: limit + PageSize);
        public string Term => term;
        public string Role => role;
        public string Status => status;
        public string Sort => sort;

        public string ListUrl(string q = null, string r = null, string s = null, string by = null, string view = null, int? max = null, int? actions = null)
        {
            var parts = new List<string>();
            Action<string, string> add = (key, value) => { if (!string.IsNullOrEmpty(value)) parts.Add(key + "=" + System.Net.WebUtility.UrlEncode(value)); };
            add("q", q ?? term);
            add("role", r ?? role);
            add("status", s ?? status);
            var order = by ?? sort;
            add("sort", order == "name" ? null : order);
            var layout = view ?? View;
            add("view", layout == "grid" ? null : layout);
            var count = max ?? limit;
            add("limit", count == PageSize ? null : count.ToString(CultureInfo.InvariantCulture));
            add("actions", actions?.ToString(CultureInfo.InvariantCulture));
            return "/Users" + (parts.Count == 0 ? "" : "?" + string.Join("&", parts));
        }

    }
}
