using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend.Controls
{
    /// <summary>
    /// The learner's overview: greeting and latest announcement, leaderboard ranking, active rate (share of days with any
    /// learning activity), activity progress, and enrolled plus suggested courses with a status filter and sort.
    /// </summary>
    public partial class LearnerDashboard : UserControl
    {
        static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

        protected UserRow Me;
        protected List<object> Shown = new List<object>();
        protected int Done, Total, Percent, AnnouncementCount, Rate, RateDelta, Active30, Field, MyPoints, RankFill;
        protected int? MyRank, WeekRank;
        protected string Greeting, LatestTitle, LatestDate, ResumeUrl, RankChip, RankVisual, RankNote, RateChart, MiniBars;
        protected List<CourseRow> Courses;

        SkillPage Host => (SkillPage)Page;

        string Range
        {
            get { return ViewState["range"] as string ?? "month"; }
            set { ViewState["range"] = value; }
        }

        bool SortByProgress
        {
            get { return ViewState["sort"] as bool? ?? false; }
            set { ViewState["sort"] = value; }
        }

        protected void Range_Command(object sender, CommandEventArgs e) => Range = (string)e.CommandArgument;

        protected void Sort_Click(object sender, EventArgs e) => SortByProgress = !SortByProgress;

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            var data = Host.Data;
            Me = Host.Me;
            var now = DateTime.Now;
            var mine = Host.MyEnrollments.Where(x => data.Course(x.CourseId) != null).ToList();
            Courses = mine.Select(x => data.Course(x.CourseId)).ToList();
            int DoneIn(EnrollmentRow x) => data.CompletedLessons(Me.Id, x.CourseId).Count;
            Total = Courses.Sum(c => data.LessonsOf(c.Id).Count);
            Done = mine.Sum(DoneIn);
            Percent = Ui.Percent(Done, Total);

            var hour = now.Hour;
            Greeting = hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening";
            var enrolled = new HashSet<int>(Courses.Select(c => c.Id));
            var notes = data.Announcements.Where(a => a.CourseId != null && enrolled.Contains(a.CourseId.Value)).OrderByDescending(a => a.CreatedAt).ToList();
            AnnouncementCount = notes.Count;
            LatestTitle = notes.FirstOrDefault()?.Title ?? "Your learning workspace is ready";
            LatestDate = notes.Count > 0 ? Ui.Date(notes[0].CreatedAt) : "Course updates will appear here";
            ResumeUrl = ResumeLink(mine) ?? "Catalog";

            BindRanking(now);
            BindActivity(mine, now);
            BindCourses(mine, enrolled);
            MonthlyButton.Attributes["aria-pressed"] = Range == "month" ? "true" : "false";
            YearlyButton.Attributes["aria-pressed"] = Range == "year" ? "true" : "false";
            SortButton.Attributes["aria-pressed"] = SortByProgress ? "true" : "false";
            SortButton.Attributes["aria-label"] = SortByProgress ? "Sort courses by name" : "Sort courses by progress";
        }

        /// <summary>
        /// Where "Continue learning" goes: the activity this learner last opened (Activity.aspx saves it in a cookie), or the
        /// next unfinished one in that course when they had already completed it. With no usable memory it falls back to the
        /// next unfinished activity of the most recently active course. Null when everything is finished.
        /// </summary>
        string ResumeLink(List<EnrollmentRow> mine)
        {
            var saved = Request.Cookies["SkillUp.Resume." + Me.Id]?.Value;
            var parts = (saved ?? "").Split('.');
            int courseId, lessonId;
            if (parts.Length == 2 && int.TryParse(parts[0], out courseId) && int.TryParse(parts[1], out lessonId)
                && mine.Any(x => x.CourseId == courseId) && Host.Data.Course(courseId)?.Status == "Published")
            {
                var lessons = Host.Data.LessonsOf(courseId);
                var done = Host.Data.CompletedLessons(Me.Id, courseId);
                var last = lessons.FirstOrDefault(l => l.Id == lessonId);
                if (last != null)
                {
                    var next = done.Contains(last.Id) ? lessons.FirstOrDefault(l => !done.Contains(l.Id)) : last;
                    return "Activity?course=" + courseId + "&lesson=" + (next ?? last).Id;
                }
            }
            var open = mine.Select(x => new { x, next = Host.Data.LessonsOf(x.CourseId).FirstOrDefault(l => !Host.Data.CompletedLessons(Me.Id, x.CourseId).Contains(l.Id)) })
                .Where(o => o.next != null).OrderByDescending(o => o.x.LastActive).FirstOrDefault();
            return open == null ? null : "Activity?course=" + open.x.CourseId + "&lesson=" + open.next.Id;
        }

        /// <summary>Same all-time and weekly ranks as the Leaderboard page; an unranked learner sees the current top three.</summary>
        void BindRanking(DateTime now)
        {
            var board = Insights.Leaderboard(Host, now);
            var ranked = board.All.Where(r => r.Rank != null).ToList();
            var me = board.All.FirstOrDefault(r => r.User.Id == Me.Id);
            WeekRank = board.Week.FirstOrDefault(r => r.User.Id == Me.Id)?.Rank;
            MyRank = me?.Rank;
            Field = board.All.Count;
            MyPoints = me?.Points ?? 0;
            RankFill = MyRank == null || Field == 0 ? 0 : Ui.Percent(Field - MyRank.Value + 1, Field);
            var topPct = MyRank == null ? 0 : Math.Max(1, Ui.Percent(MyRank.Value, Field));
            RankChip = MyRank == null ? "Unranked" : topPct <= 50 ? "Top " + topPct + "%" : MyPoints.ToString("N0") + " pts";
            var ahead = MyRank == null ? null : ranked.LastOrDefault(r => r.Points > me.Points);
            RankNote = MyRank == null ? "Pass a quiz or finish a course to earn a rank"
                : ahead != null ? (ahead.Points - me.Points).ToString("N0") + " points behind #" + ahead.Rank + " " + ahead.User.Name : "You are leading the leaderboard";
            if (MyRank != null)
            {
                RankVisual = "<div class=\"vd-main-value\">#" + MyRank + "<small>/ " + Field + "</small></div>";
                return;
            }
            var top = ranked.Take(3).ToList();
            var max = Math.Max(1, top.Count == 0 ? 1 : top.Max(r => r.Points));
            var label = string.Join("; ", top.Select(r => "#" + r.Rank + " " + r.User.Name + ", " + r.Points + " points")) + "; you, 0 points";
            var cols = new[] { top.ElementAtOrDefault(1), top.ElementAtOrDefault(0), top.ElementAtOrDefault(2) }.Where(r => r != null)
                .Select(r => "<div class=\"vd-podium-col" + (r.Rank == 1 ? " is-first" : "") + "\" style=\"--h:" + Math.Max(28, Ui.Percent(r.Points, max)) + "%\"><span class=\"vd-podium-face\">"
                    + Ui.H(Ui.Initials(r.User)) + "</span><span class=\"vd-podium-step\"><b>#" + r.Rank + "</b><small>" + r.Points.ToString("N0") + "</small></span></div>");
            RankVisual = "<div class=\"vd-podium\" role=\"img\" aria-label=\"" + Ui.H(label) + "\">" + string.Concat(cols)
                + "<div class=\"vd-podium-col is-you\" style=\"--h:14%\"><span class=\"vd-podium-face\">You</span><span class=\"vd-podium-step\"><small>0</small></span></div></div>";
        }

        /// <summary>Active rate, streak, the 30-day or 12-month chart, and the XP bars of the last 30 days.</summary>
        void BindActivity(List<EnrollmentRow> mine, DateTime now)
        {
            var data = Host.Data;
            var attempts = Insights.Attempts(Host).Where(a => a.UserId == Me.Id).ToList();
            // XP: 100 per completed activity (dated by the enrolment's last activity) plus each result's score.
            var gains = mine.Select(x => new { Day = (x.LastActive ?? x.EnrolledAt).Date, Xp = data.CompletedLessons(Me.Id, x.CourseId).Count * 100 })
                .Concat(attempts.Select(a => new { Day = a.At.Date, Xp = a.Score })).Where(g => g.Xp > 0).ToList();
            var today = now.Date;
            Func<int, DateTime> ago = n => today.AddDays(-n);
            Func<DateTime, DateTime, int> sum = (from, to) => gains.Where(g => g.Day >= from && g.Day <= to).Sum(g => g.Xp);
            var buckets = Enumerable.Range(0, 12).Select(i => new { Label = ago(30 - (i + 1) * 30 / 12).ToString("MMM d", En), Value = sum(ago(29 - i * 30 / 12), ago(30 - (i + 1) * 30 / 12)) }).ToList();
            var maxBar = Math.Max(1, buckets.Max(b => b.Value));
            MiniBars = string.Concat(buckets.Select(b => "<i style=\"--h:" + (b.Value > 0 ? Math.Max(8, Ui.Percent(b.Value, maxBar)) : 5) + "%\" class=\"" + (b.Value > 0 ? "has-value" : "") + "\" title=\"" + b.Label + " · " + b.Value + " XP\"></i>"));

            var activeDays = new HashSet<DateTime>(gains.Select(g => g.Day).Concat(attempts.Select(a => a.At.Date)).Concat(mine.Where(x => x.LastActive != null).Select(x => x.LastActive.Value.Date)));
            Func<int, int, int> activeCount = (from, days) => Enumerable.Range(0, days).Count(k => activeDays.Contains(ago(from + k)));
            Active30 = activeCount(0, 30);
            Rate = Ui.Percent(Active30, 30);
            RateDelta = Rate - Ui.Percent(activeCount(30, 30), 30);
            var streak = 0;
            for (var k = activeDays.Contains(today) ? 0 : 1; activeDays.Contains(ago(k)); k++) streak++;

            if (Range == "year")
            {
                var months = Enumerable.Range(0, 12).Select(i =>
                {
                    var first = new DateTime(today.Year, today.Month, 1).AddMonths(i - 11);
                    var days = i == 11 ? today.Day : DateTime.DaysInMonth(first.Year, first.Month);
                    var active = Enumerable.Range(0, days).Count(k => activeDays.Contains(first.AddDays(k)));
                    return Tuple.Create(first.ToString("MMM", En), Ui.Percent(active, days));
                }).ToList();
                RateChart = Chart(months, "Monthly active rate · last 12 months", streak);
            }
            else
            {
                var rolling = Enumerable.Range(0, 30).Select(i => Tuple.Create(ago(29 - i).ToString("MMM d", En), Ui.Percent(activeCount(29 - i, 7), 7))).ToList();
                RateChart = Chart(rolling, "7-day active rate · last 30 days", streak);
            }
        }

        static string Chart(List<Tuple<string, int>> rows, string caption, int streak)
        {
            var points = rows.Select((r, i) => (8 + i * 284.0 / (rows.Count - 1)).ToString("0.0", CultureInfo.InvariantCulture) + "," + (106 - r.Item2 / 100.0 * 84).ToString("0.0", CultureInfo.InvariantCulture)).ToList();
            var last = points[points.Count - 1].Split(',');
            return "<div class=\"vd-line-view\"><div class=\"vd-chart-caption\"><span>" + caption + "</span><b>" + streak + "-day streak</b></div>"
                + "<svg class=\"vd-line-chart\" viewBox=\"0 0 300 124\" role=\"img\" aria-label=\"" + Ui.H(string.Join(", ", rows.Select(r => r.Item1 + ": " + r.Item2 + "% active"))) + "\">"
                + "<path d=\"M8 22H292M8 64H292M8 106H292\" class=\"vd-chart-grid\"/><polyline points=\"" + string.Join(" ", points) + "\" class=\"vd-chart-line\"/>"
                + "<circle cx=\"" + last[0] + "\" cy=\"" + last[1] + "\" r=\"3.5\" class=\"vd-chart-point\"/></svg>"
                + "<div class=\"vd-chart-axis\"><span>" + rows[0].Item1 + "</span><span>" + rows[rows.Count - 1].Item1 + "</span></div></div>";
        }

        /// <summary>Enrolled courses, topped up to three with published courses from the same programs.</summary>
        void BindCourses(List<EnrollmentRow> mine, HashSet<int> enrolled)
        {
            var data = Host.Data;
            var programs = new HashSet<int>(Courses.Select(c => c.ProgramId));
            var suggested = data.Courses.Where(c => c.Status == "Published" && programs.Contains(c.ProgramId) && !enrolled.Contains(c.Id)).Take(Math.Max(0, 3 - Courses.Count));
            var rows = Courses.Select(c => (c, enrolment: true)).Concat(suggested.Select(c => (c, enrolment: false))).Select(x =>
            {
                var lessons = data.LessonsOf(x.c.Id).Count;
                var finished = x.enrolment ? data.CompletedLessons(Me.Id, x.c.Id).Count : 0;
                var p = Ui.Percent(finished, lessons);
                var status = !x.enrolment ? "suggested" : p == 100 ? "complete" : p > 0 ? "progress" : "new";
                return new
                {
                    x.c.Id, x.c.Title, Percent = p, Status = status, Icon = Glyph(x.c.Title),
                    Detail = x.enrolment ? "Instructor: " + (data.User(x.c.InstructorId)?.Name ?? "Skill UP instructor") + " · " + Math.Max(0, lessons - finished) + " activities left"
                                         : "Suggested for you · " + lessons + " activities"
                };
            }).ToList();
            var filter = StatusFilter.SelectedValue;
            var shown = rows.Where(r => filter == "all" || r.Status == filter);
            shown = SortByProgress ? shown.OrderByDescending(r => r.Percent).ThenBy(r => r.Title) : shown;
            Shown = shown.Cast<object>().ToList();
            CourseRows.DataSource = Shown;
            CourseRows.DataBind();
        }

        static string Glyph(string title)
        {
            var path = Regex.IsMatch(title, "mongo|sql|database", RegexOptions.IgnoreCase) ? "<ellipse cx=\"12\" cy=\"5\" rx=\"8\" ry=\"3\"></ellipse><path d=\"M4 5v14c0 4 16 4 16 0V5M4 12c0 4 16 4 16 0\"></path>"
                     : Regex.IsMatch(title, "c#|code|api|program", RegexOptions.IgnoreCase) ? "<path d=\"m8 6-6 6 6 6m8-12 6 6-6 6m-3-14-2 16\"></path>"
                     : "<path d=\"M4 3v18h17M8 17v-5m5 5V7m5 10V4\"></path>";
            return "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">" + path + "</svg>";
        }
    }
}
