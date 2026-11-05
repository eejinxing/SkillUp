using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SkillUp.Services;

namespace SkillUp.Models
{
    /// <summary>
    /// Learning standings (?period=week|all&amp;q=). The order comes from Insights.Leaderboard: courses completed first, then best quiz
    /// score, then quiz average, then the earlier quiz time and the earlier course completion time.
    /// </summary>
    public sealed class LeaderboardModel : PageModel
    {
        public LeaderboardModel(SkillContext ctx) : base(ctx) { }
        public IEnumerable<dynamic> RankRows;


        static readonly Dictionary<string, string> MarkColours = new Dictionary<string, string> { ["DB"] = "#13aa52", ["C#"] = "#2563eb", ["API"] = "#f97316", ["DS"] = "#0d9488" };
        public const string Star = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"m12 3 2.7 5.5 6.1.9-4.4 4.3 1 6.1-5.4-2.9L6.6 20l1-6.1-4.4-4.3 6.1-.9Z\"/></svg>";
        public const string Trophy = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M8 4h8v5a4 4 0 0 1-8 0Z\"/><path d=\"M8 6H5a3 3 0 0 0 3 4M16 6h3a3 3 0 0 1-3 4M12 13v4M9 20h6\"/></svg>";

        public Board Board;
        public List<RankRow> Rows, Shown;
        public RankRow Mine, AllMine, WeekMine;
        public string Period, PeriodLabel, TimeLeft, EmptySlots = "", PodiumHtml = "";
        public int Gap, MyQuizCount, MyCourseCount;
        public string Term => (Request.QueryString["q"] ?? "").Trim();

        List<Insights.Attempt> attempts;
        Dictionary<int, CourseRow> courses;

        public void Load()
        {
            Period = Request.QueryString["period"] == "week" ? "week" : "all";
            PeriodLabel = Period == "week" ? "This week" : "All time";
            Board = Insights.Leaderboard(Ctx, DateTime.Now);
            Rows = Period == "week" ? Board.Week : Board.All;
            Mine = Rows.FirstOrDefault(r => r.User.Id == Me.Id);
            AllMine = Board.All.FirstOrDefault(r => r.User.Id == Me.Id);
            WeekMine = Board.Week.FirstOrDefault(r => r.User.Id == Me.Id);
            var hours = Math.Max(0, (int)Math.Round((Board.End - Board.UpdatedAt).TotalHours));
            TimeLeft = hours > 48 ? Math.Round(hours / 24.0) + "d left" : hours + "h left";
            var ranked = Rows.Where(r => r.Points > 0).ToList();
            Gap = Math.Max(0, (ranked.FirstOrDefault()?.Points ?? 0) - (Mine?.Points ?? 0));
            courses = VisibleCourses.ToDictionary(c => c.Id);
            attempts = Insights.Attempts(Ctx).Where(a => a.Type == "quiz" && courses.ContainsKey(a.CourseId) && a.At <= Board.UpdatedAt).ToList();
            MyQuizCount = Insights.Attempts(Ctx).Count(a => a.UserId == Me.Id && a.Type == "quiz");
            MyCourseCount = MyEnrollments.Count(x => courses.ContainsKey(x.CourseId));

            // The Top 10: ranked learners first, then unranked ones; a search filters the whole field.
            var ordered = ranked.Concat(Rows.Where(r => r.Points <= 0)).ToList();
            var term = Term.ToLowerInvariant();
            Shown = term.Length > 0 ? ordered.Where(r => SearchMatch.Matches(term, r.User.Name + " " + r.User.Username)).ToList() : ordered.Take(10).ToList();
            // Podium of the top three ranked learners, shown as #2, #1, #3 (hidden while searching).
            if (term.Length == 0 && ranked.Count > 0)
            {
                var heights = new[] { 142, 118, 96 };
                PodiumHtml = string.Concat(new[] { 1, 0, 2 }.Where(i => i < ranked.Count).Select(i =>
                    "<div class=\"lb-sk-pc" + (i == 0 ? " is-first" : "") + "\"><span class=\"lb-sk-pface\">" + Ui.H(Ui.Initials(ranked[i].User)) + "</span><div class=\"lb-sk-pstep\" style=\"--h:" + heights[i]
                    + "px\"><b>#" + ranked[i].Rank + "</b><span>" + ranked[i].Points.ToString("N0") + "</span><small>" + Ui.H(ranked[i].User.Name) + "</small></div></div>"));
            }
            RankRows = Shown.Select(r => RowView(r)).ToList();
            // Open positions keep the Top 10 at ten rows when fewer learners are active; nobody is invented to fill them.
            if (term.Length == 0)
                EmptySlots = string.Concat(Enumerable.Range(Shown.Count, Math.Max(0, 10 - Shown.Count)).Select(EmptySlot));
        }

        public string RankText(RankRow r) => r?.Rank == null ? "—" : r.Rank.ToString();
        public string Standing(List<RankRow> list) { var v = RankText(list.FirstOrDefault(r => r.User.Id == Me.Id)); return v == "—" ? "Unranked" : "#" + v; }
        public string PeriodUrl(string period) => "Leaderboard?period=" + period + (Term.Length > 0 ? "&q=" + System.Net.WebUtility.UrlEncode(Term) : "");

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

        object RowView(RankRow r)
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
                Level = r.Completed + 1,
                CourseTitles = string.Join(", ", enrolments.Select(c => c.Title)),
                CourseContext = (enrolments.FirstOrDefault()?.Title ?? "No courses yet") + (enrolments.Count > 1 ? " +" + (enrolments.Count - 1) : ""),
                BestTitle = best == null ? "No quiz score yet" : "Best quiz " + best + "%",
                PointsText = r.Points.ToString("N0"),
                LastActive = r.LastActive == null ? "—" : Ui.Day(r.LastActive),
                TimingHtml = Timing(r, progress.Select(x => Tuple.Create(x.c, x.p, x.done, x.total)).ToList()),
                Status = r.Rank == 1 ? "TOP" : r.Rank != null ? "RANKED" : "UNRANKED",
                StatusClass = r.Rank == 1 ? "is-top" : r.Rank != null ? "is-ranked" : ""
            };
        }

        /// <summary>
        /// Ranking columns in ranking order: courses completed, best quiz score, average quiz score, time taken for the best quiz score
        /// (faster wins a tie) and total time taken to complete the courses (the last tie-breaker).
        /// </summary>
        string Timing(RankRow r, List<Tuple<CourseRow, int, int, int>> sectors)
        {
            var inv = CultureInfo.InvariantCulture;
            var pct = Ui.Percent(r.Completed, r.Enrolled);
            Func<double?, string, string, string> num = (value, kind, caption) => "<span class=\"lb-sk-num lb-sk-" + kind + " " + (value == null ? "is-missing" : value >= 70 ? "is-good" : "") + "\" title=\""
                + caption + " (%)\"><b>" + (value == null ? "—" : value.Value.ToString(kind == "best" ? "0" : "0.0", inv)) + "</b></span>";
            Func<TimeSpan?, string, string> when = (t, caption) => "<span class=\"lb-sk-time" + (t == null ? " is-missing" : "") + "\" title=\"" + caption + "\"><b>" + (t == null ? "—" : Duration(t.Value)) + "</b></span>";
            return "<span class=\"lb-sk-course\" style=\"--p:" + pct + "%\" title=\"" + r.Completed + " of " + r.Enrolled + " courses completed\"><b>" + r.Completed + "<em>/" + r.Enrolled + "</em></b><i aria-hidden=\"true\"></i></span>"
                + num(r.Best, "best", "Best quiz score") + num(r.Average, "avg", "Average quiz score")
                + when(r.QuizTaken, "Time taken for the best quiz score (faster ranks higher on a tie)") + when(r.CourseTaken, "Total time from enrolling to completing the finished courses");
        }

        /// <summary>Compact duration such as 42s, 12m 05s, 3h 20m or 2d 4h.</summary>
        static string Duration(TimeSpan t) =>
            t.TotalMinutes < 1 ? Math.Max(1, (int)t.TotalSeconds) + "s"
            : t.TotalHours < 1 ? (int)t.TotalMinutes + "m " + t.Seconds.ToString("00") + "s"
            : t.TotalDays < 1 ? (int)t.TotalHours + "h " + t.Minutes.ToString("00") + "m"
            : (int)t.TotalDays + "d " + t.Hours + "h";

        static string EmptySlot(int i) =>
            "<div class=\"lb-rank-card lb-empty-slot\" aria-label=\"Position " + (i + 1) + " open\"><span class=\"lb-timing-position is-unranked\">" + (i + 1).ToString("00") + "</span>"
            + "<div class=\"lb-timing-identity\"><span class=\"lb-empty-dot\" aria-hidden=\"true\"></span><div class=\"lb-driver-copy\"><h2>Open position</h2><p><span class=\"lb-row-context\">Complete a quiz to claim it</span></p></div></div>"
            + "<span class=\"lb-sk-course\" style=\"--p:0%\"><b>—</b><i aria-hidden=\"true\"></i></span><span class=\"lb-sk-num is-missing\"><b>—</b></span><span class=\"lb-sk-num is-missing\"><b>—</b></span><span class=\"lb-sk-time is-missing\"><b>—</b></span><span class=\"lb-sk-time is-missing\"><b>—</b></span><span class=\"lb-row-status\">OPEN</span></div>";
    }
}
