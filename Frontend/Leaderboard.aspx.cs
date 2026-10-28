using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend
{
    /// <summary>
    /// Learning standings styled as a race timing screen (?period=week|all&amp;q=). Points come from Insights.Leaderboard:
    /// 100 per completed course plus the best quiz score per course (all time), or this week's best quiz scores.
    /// </summary>
    public partial class LeaderboardPage : SkillPage
    {
        public override string Route => "leaderboard";
        protected override string[] Roles => new[] { "Learner" };

        static readonly Dictionary<string, string> MarkColours = new Dictionary<string, string> { ["DB"] = "#13aa52", ["C#"] = "#2563eb", ["API"] = "#f97316", ["DS"] = "#0d9488" };
        protected const string Star = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"m12 3 2.7 5.5 6.1.9-4.4 4.3 1 6.1-5.4-2.9L6.6 20l1-6.1-4.4-4.3 6.1-.9Z\"/></svg>";
        protected const string Trophy = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M8 4h8v5a4 4 0 0 1-8 0Z\"/><path d=\"M8 6H5a3 3 0 0 0 3 4M16 6h3a3 3 0 0 1-3 4M12 13v4M9 20h6\"/></svg>";

        protected Board Board;
        protected List<RankRow> Rows, Shown;
        protected RankRow Mine, AllMine, WeekMine;
        protected string Period, PeriodLabel, TimeLeft, EmptySlots = "";
        protected int Gap, MyQuizCount, MyCourseCount;
        protected string Term => (Request.QueryString["q"] ?? "").Trim();

        List<Insights.Attempt> attempts;
        Dictionary<int, CourseRow> courses;

        protected void Page_Load(object sender, EventArgs e)
        {
            Period = Request.QueryString["period"] == "week" ? "week" : "all";
            PeriodLabel = Period == "week" ? "This week" : "All time";
            Board = Insights.Leaderboard(this, DateTime.Now);
            Rows = Period == "week" ? Board.Week : Board.All;
            Mine = Rows.FirstOrDefault(r => r.User.Id == Me.Id);
            AllMine = Board.All.FirstOrDefault(r => r.User.Id == Me.Id);
            WeekMine = Board.Week.FirstOrDefault(r => r.User.Id == Me.Id);
            var hours = Math.Max(0, (int)Math.Round((Board.End - Board.UpdatedAt).TotalHours));
            TimeLeft = hours > 48 ? Math.Round(hours / 24.0) + "d left" : hours + "h left";
            var ranked = Rows.Where(r => r.Points > 0).ToList();
            Gap = Math.Max(0, (ranked.FirstOrDefault()?.Points ?? 0) - (Mine?.Points ?? 0));
            courses = VisibleCourses.ToDictionary(c => c.Id);
            attempts = Insights.Attempts(this).Where(a => a.Type == "quiz" && courses.ContainsKey(a.CourseId) && a.At <= Board.UpdatedAt).ToList();
            MyQuizCount = Insights.Attempts(this).Count(a => a.UserId == Me.Id && a.Type == "quiz");
            MyCourseCount = MyEnrollments.Count(x => courses.ContainsKey(x.CourseId));
            if (!IsPostBack) SearchBox.Text = Term;
            WeekButton.Attributes["aria-pressed"] = Period == "week" ? "true" : "false";
            AllButton.Attributes["aria-pressed"] = Period == "all" ? "true" : "false";

            // The Top 10: ranked learners first, then unranked ones; a search filters the whole field.
            var ordered = ranked.Concat(Rows.Where(r => r.Points <= 0)).ToList();
            var term = Term.ToLowerInvariant();
            Shown = term.Length > 0 ? ordered.Where(r => (r.User.Name + " " + r.User.Username).ToLowerInvariant().Contains(term)).ToList() : ordered.Take(10).ToList();
            var top = ranked.FirstOrDefault()?.Points ?? 0;
            RankRows.DataSource = Shown.Select(r => RowView(r, top));
            RankRows.DataBind();
            // Open positions keep the Top 10 at ten rows when fewer learners are active; nobody is invented to fill them.
            if (term.Length == 0)
                EmptySlots = string.Concat(Enumerable.Range(Shown.Count, Math.Max(0, 10 - Shown.Count)).Select(EmptySlot));
        }

        protected string RankText(RankRow r) => r?.Rank == null ? "—" : r.Rank.ToString();
        protected string Standing(List<RankRow> list) { var v = RankText(list.FirstOrDefault(r => r.User.Id == Me.Id)); return v == "—" ? "Unranked" : "#" + v; }
        protected string PeriodUrl(string period) => "Leaderboard?period=" + period + (Term.Length > 0 ? "&q=" + HttpUtility.UrlEncode(Term) : "");

        protected void Period_Command(object sender, CommandEventArgs e) => Go(PeriodUrl((string)e.CommandArgument));

        protected void Search_Click(object sender, EventArgs e) =>
            Go("Leaderboard?period=" + Period + (SearchBox.Text.Trim().Length > 0 ? "&q=" + HttpUtility.UrlEncode(SearchBox.Text.Trim()) : ""));

        /// <summary>Quiz scores of a learner within the selected period, clamped to 0-100.</summary>
        List<int> Scores(int userId) => attempts.Where(a => a.UserId == userId && (Period == "all" || (a.At >= Board.Start && a.At < Board.End)))
            .Select(a => Math.Max(0, Math.Min(100, a.Score))).ToList();

        int? Best(int userId) { var s = Scores(userId); return s.Count == 0 ? (int?)null : s.Max(); }

        static string Mark(CourseRow c, string extraClass, string suffix)
        {
            string colour;
            if (!MarkColours.TryGetValue((c.Icon ?? "").ToUpperInvariant(), out colour)) colour = "#52525b";
            var text = string.IsNullOrEmpty(c.Icon) ? c.Title.Substring(0, 1) : c.Icon;
            return "<span class=\"lb-mark " + extraClass + "\" style=\"--mk:" + colour + "\" title=\"" + Ui.H(c.Title) + suffix + "\">" + Ui.H(text.Length > 2 ? text.Substring(0, 2) : text) + "</span>";
        }

        object RowView(RankRow r, int top)
        {
            var enrolments = Data.Enrollments.Where(x => x.UserId == r.User.Id && courses.ContainsKey(x.CourseId)).Select(x => courses[x.CourseId]).ToList();
            var progress = enrolments.Select(c => new { c, p = Progress(r.User.Id, c.Id), done = Data.CompletedLessons(r.User.Id, c.Id).Count, total = Data.LessonsOf(c.Id).Count }).ToList();
            var best = Best(r.User.Id);
            string accent;
            if (!MarkColours.TryGetValue((enrolments.FirstOrDefault()?.Icon ?? "").ToUpperInvariant(), out accent)) accent = "#9b8abf";

            // Course badges overlap like team logos: full colour with a check when completed, dimmed while in progress.
            var badgeList = progress.OrderByDescending(x => x.p == 100).ToList();
            var badges = badgeList.Count == 0 ? "" : "<span class=\"lb-done-badges\" role=\"img\" aria-label=\"" + badgeList.Count(x => x.p == 100) + " of " + badgeList.Count + " courses completed: "
                + Ui.H(string.Join(", ", badgeList.Select(x => x.c.Title + (x.p == 100 ? " completed" : " " + x.p + "%")))) + "\">"
                + string.Concat(badgeList.Take(4).Select(x => Mark(x.c, x.p == 100 ? "is-done" : "is-open", x.p == 100 ? " · completed" : " · " + x.p + "%")))
                + (badgeList.Count > 4 ? "<span class=\"lb-mark lb-done-more\">+" + (badgeList.Count - 4) + "</span>" : "") + "</span>";

            return new
            {
                r.Rank, r.Points, Name = r.User.Name, IsMe = r.User.Id == Me.Id, Accent = accent,
                PodiumClass = r.Rank == 1 ? "lb-winner" : r.Rank == 2 ? "lb-p2" : r.Rank == 3 ? "lb-p3" : "",
                AvatarHtml = Ui.Avatar(r.User),
                Online = r.LastActive != null && (Board.UpdatedAt - r.LastActive.Value).TotalDays < 7,
                BadgesHtml = badges,
                Level = r.Points / 100 + 1,
                CourseTitles = string.Join(", ", enrolments.Select(c => c.Title)),
                CourseContext = (enrolments.FirstOrDefault()?.Title ?? "No courses yet") + (enrolments.Count > 1 ? " +" + (enrolments.Count - 1) : ""),
                BestTitle = best == null ? "No quiz score yet" : "Best quiz " + best + "%",
                PointsText = r.Points.ToString("N0"),
                LastActive = r.LastActive == null ? "—" : Ui.Day(r.LastActive),
                TimingHtml = Timing(r, top, progress.Select(x => Tuple.Create(x.c, x.p, x.done, x.total)).ToList()),
                Status = r.Rank == 1 ? "TOP" : r.Rank != null ? "RANKED" : "UNRANKED",
                StatusClass = r.Rank == 1 ? "is-top" : r.Rank != null ? "is-ranked" : ""
            };
        }

        /// <summary>
        /// Timing-screen columns: ERS = overall activity progress, DRS = a passed quiz (70%+), INTERVAL = points behind the
        /// leader, S1-S3 = progress in the first three courses, then best and average quiz score, activities and attempts.
        /// </summary>
        string Timing(RankRow r, int top, List<Tuple<CourseRow, int, int, int>> sectors)
        {
            var total = sectors.Sum(s => s.Item4);
            var done = sectors.Sum(s => s.Item3);
            var ers = Ui.Percent(done, total);
            var scores = Scores(r.User.Id);
            double? best = scores.Count == 0 ? (double?)null : scores.Max(), average = scores.Count == 0 ? (double?)null : scores.Average();
            var inv = CultureInfo.InvariantCulture;
            Func<double?, string, string> score = (value, kind) => "<span class=\"lb-datum lb-data-" + kind + " " + (value == null ? "is-missing" : value >= 70 ? "is-good" : "is-pending") + "\" title=\""
                + (kind == "best" ? "Best quiz score" : "Average quiz score") + " (%)\"><b>" + (value == null ? "—" : value.Value.ToString("0.000", inv)) + "</b><small>" + (kind == "best" ? "PERSONAL BEST" : "QUIZ AVERAGE") + "</small></span>";
            Func<int, string> sector = k =>
            {
                if (k >= sectors.Count) return "<span class=\"lb-sector is-none\"><b>—</b><small class=\"lb-sector-detail\">—/— ACT</small></span>";
                var s = sectors[k];
                return "<span class=\"lb-sector " + (s.Item2 == 100 ? "is-done" : s.Item2 > 0 ? "is-live" : "") + "\" title=\"" + Ui.H(s.Item1.Title) + " · " + s.Item2 + "% complete\" aria-label=\"" + Ui.H(s.Item1.Title) + ": " + s.Item2
                    + "% complete\"><b>" + s.Item2.ToString("0.000", inv) + "<small class=\"lb-sector-unit\">%</small></b><small class=\"lb-sector-detail\">" + s.Item3.ToString("00") + "/" + s.Item4.ToString("00") + " ACT</small></span>";
            };
            return "<span class=\"lb-ers " + (ers == 100 ? "is-full" : ers == 0 ? "is-empty" : ers < 25 ? "is-low" : ers < 65 ? "is-mid" : "") + "\" style=\"--p:" + ers + "%\" aria-label=\"" + ers + "% of activities complete\"><b>" + ers + "%</b></span>"
                + "<span class=\"lb-drs " + (scores.Any(s => s >= 70) ? "is-on" : "") + "\" title=\"" + (scores.Any(s => s >= 70) ? "Quiz passed" : "No quiz passed yet") + "\">DRS</span>"
                + "<span class=\"lb-interval " + (r.Rank == 1 ? "is-leader" : "") + "\" title=\"Points behind the leader\">" + (r.Rank == 1 ? "Interval" : "+" + Math.Max(0, top - r.Points).ToString("0.000", inv)) + "</span>"
                + sector(0) + sector(1) + sector(2) + score(best, "best") + score(average, "avg")
                + "<span class=\"lb-datum lb-data-act\" title=\"Completed / enrolled activities\"><b>" + done.ToString("00") + "/" + total.ToString("00") + "</b><small>ACTIVITIES</small></span>"
                + "<span class=\"lb-datum lb-data-quiz\" title=\"Quiz attempts in this ranking period\"><b>" + scores.Count.ToString("00") + "</b><small>ATTEMPTS</small></span>";
        }

        static string EmptySlot(int i) =>
            "<div class=\"lb-rank-card lb-empty-slot\" aria-label=\"Position " + (i + 1) + " open\"><span class=\"lb-timing-position is-unranked\">" + (i + 1).ToString("00") + "</span>"
            + "<div class=\"lb-timing-identity\"><span class=\"lb-empty-dot\" aria-hidden=\"true\"></span><div class=\"lb-driver-copy\"><h2>Open position</h2><p><span class=\"lb-row-context\">Complete a quiz to claim it</span></p></div></div>"
            + "<span class=\"lb-ers is-empty\" style=\"--p:0%\"><b>—</b></span><span class=\"lb-drs\">DRS</span><span class=\"lb-interval\">—</span><span class=\"lb-sector is-none\">—</span><span class=\"lb-sector is-none\">—</span><span class=\"lb-sector is-none\">—</span>"
            + "<span class=\"lb-datum lb-data-best is-missing\"><b>—</b></span><span class=\"lb-datum lb-data-avg is-missing\"><b>—</b></span><span class=\"lb-datum lb-data-act is-missing\"><b>—</b></span><span class=\"lb-datum lb-data-quiz is-missing\"><b>—</b></span><span class=\"lb-row-status\">OPEN</span></div>";
    }
}
