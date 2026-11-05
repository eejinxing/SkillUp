using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SkillUp.Services;

namespace SkillUp.Models
{
    /// <summary>
    /// The instructor's overview, bound to their own courses: learners and daily learning sessions, average completion
    /// with cumulative sessions, the quiz average with a 7-day rolling trend, and course cards grouped by program.
    /// </summary>
    public sealed class InstructorDashboardModel : PageModel
    {
        public InstructorDashboardModel(SkillContext ctx) : base(ctx) { }
        public IEnumerable<dynamic> ProgramList;
        public bool NoCourses;

        static string Svg(string d) => "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">" + d + "</svg>";
        public static readonly string More = Svg("<circle cx=\"12\" cy=\"5.5\" r=\"1.2\"/><circle cx=\"12\" cy=\"12\" r=\"1.2\"/><circle cx=\"12\" cy=\"18.5\" r=\"1.2\"/>");
        static readonly Dictionary<string, string> Glyphs = new Dictionary<string, string>
        {
            ["db"] = Svg("<ellipse cx=\"12\" cy=\"5.5\" rx=\"7.5\" ry=\"3\"/><path d=\"M4.5 5.5v6c0 1.7 3.4 3 7.5 3s7.5-1.3 7.5-3v-6M4.5 11.5v6c0 1.7 3.4 3 7.5 3s7.5-1.3 7.5-3v-6\"/>"),
            ["csharp"] = "C#", ["api"] = "{ }",
            ["web"] = Svg("<circle cx=\"12\" cy=\"12\" r=\"8.5\"/><path d=\"M3.5 12h17M12 3.5c2.6 2.4 3.9 5.2 3.9 8.5s-1.3 6.1-3.9 8.5c-2.6-2.4-3.9-5.2-3.9-8.5S9.4 5.9 12 3.5Z\"/>"),
            ["data"] = Svg("<path d=\"M4 20h16M7 20v-7M12 20V6M17 20v-10\"/>"),
            ["ai"] = Svg("<path d=\"m12 3 2.2 5.8L20 11l-5.8 2.2L12 19l-2.2-5.8L4 11l5.8-2.2Z\"/>"),
            ["chip"] = Svg("<rect x=\"7\" y=\"7\" width=\"10\" height=\"10\" rx=\"2\"/><path d=\"M10 3v4M14 3v4M10 17v4M14 17v4M3 10h4M3 14h4M17 10h4M17 14h4\"/>"),
            ["code"] = Svg("<path d=\"m8 7-5 5 5 5M16 7l5 5-5 5M14 5l-4 14\"/>")
        };
        static readonly string Lessons = Svg("<circle cx=\"6.5\" cy=\"12\" r=\"3\"/><circle cx=\"17.5\" cy=\"12\" r=\"3\"/><path d=\"M9.5 12h5\"/>"),
            Deadline = Svg("<circle cx=\"12\" cy=\"12\" r=\"8.5\"/><path d=\"M12 7.5V12l3 2\"/>"),
            Questions = Svg("<rect x=\"4\" y=\"4\" width=\"6\" height=\"6\" rx=\"1\"/><rect x=\"4\" y=\"14\" width=\"6\" height=\"6\" rx=\"1\"/><path d=\"M14 6h6M14 9h4M14 16h6M14 19h4\"/>"),
            People = Svg("<circle cx=\"9\" cy=\"8\" r=\"3.2\"/><path d=\"M3.5 19c.8-3.3 3-5 5.5-5s4.7 1.7 5.5 5M16 5.2a3 3 0 0 1 0 5.6M18 14.4c1.4.7 2.3 2.2 2.6 4.6\"/>");
        static readonly string[][] RailLinks =
        {
            new[] { "Dashboard", "Overview", "<rect x=\"3\" y=\"3\" width=\"7\" height=\"7\" rx=\"1\"/><rect x=\"14\" y=\"3\" width=\"7\" height=\"7\" rx=\"1\"/><rect x=\"3\" y=\"14\" width=\"7\" height=\"7\" rx=\"1\"/><rect x=\"14\" y=\"14\" width=\"7\" height=\"7\" rx=\"1\"/>" },
            new[] { "Courses", "My courses", "<path d=\"M12 5v16M3 4c4-1 6 0 9 2 3-2 5-3 9-2v15c-4-1-6 0-9 2-3-2-5-3-9-2Z\"/>" },
            new[] { "CourseRequests", "My requests", "<path d=\"m5 12 4 4L20 5\"/><path d=\"M21 12v8H3V3h12\"/>" },
            new[] { "Learners", "Enrolled learners", "<circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M2 21v-3a7 7 0 0 1 14 0v3M17 3a4 4 0 0 1 0 8m2 3c3 1 3 4 3 7\"/>" },
            new[] { "Analytics", "Course analytics", "<path d=\"M4 3v18h17M8 17v-5m5 5V7m5 10V4\"/>" }
        };

        public int Learners, NewLearners, StartCount, EnrolmentCount, Completion, Completed, QuizCount;
        public int? QuizAverage;
        public string Rail, DailyBars, Axis, GrowthBadge, CumulativeChart, QuizChart;

        PageModel Host => this;

        public void Load()
        {
            var data = Host.Data;
            var me = Host.Me;
            Rail = string.Concat(RailLinks.Select(l => "<a href=\"" + l[0] + "\" aria-label=\"" + l[1] + "\" title=\"" + l[1] + "\"" + (l[0] == "Dashboard" ? " aria-current=\"page\"" : "") + ">"
                + "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" aria-hidden=\"true\">" + l[2] + "</svg></a>"));
            var courses = data.Courses.Where(c => c.InstructorId == me.Id).ToList();
            var ids = new HashSet<int>(courses.Select(c => c.Id));
            var enrolments = data.Enrollments.Where(x => ids.Contains(x.CourseId)).ToList();
            var quiz = Insights.Attempts(Ctx).Where(a => a.Type == "quiz" && ids.Contains(a.CourseId)).ToList();
            EnrolmentCount = enrolments.Count;
            Completed = enrolments.Count(x => Host.Progress(x.UserId, x.CourseId) == 100);
            Learners = enrolments.Select(x => x.UserId).Distinct().Count();
            Completion = Charts.Avg(enrolments.Select(x => Host.Progress(x.UserId, x.CourseId)));
            QuizCount = quiz.Count;
            QuizAverage = quiz.Count == 0 ? (int?)null : Charts.Avg(quiz.Select(a => a.Score));

            // Daily learning sessions over the last 30 days: new enrolments plus quiz attempts.
            var today = DateTime.Today;
            var days = Enumerable.Range(0, 30).Select(i => today.AddDays(i - 29)).ToList();
            var daily = days.Select(d => enrolments.Count(x => x.EnrolledAt.Date == d) + quiz.Count(a => a.At.Date == d)).ToList();
            var dmax = Math.Max(1, daily.Max());
            DailyBars = string.Concat(daily.Select((v, i) => "<span style=\"--h:" + (v > 0 ? Math.Max(8, Ui.Percent(v, dmax)) : 3) + "%;--i:" + i + "\" title=\"" + days[i].ToString("d/M/yyyy") + ": " + v + " sessions\"></span>"));
            Axis = string.Concat(new[] { 0, 7, 14, 21, 29 }.Select(i => "<span>" + days[i].Day + "</span>"));
            StartCount = enrolments.Where(x => x.EnrolledAt < today.AddDays(-29)).Select(x => x.UserId).Distinct().Count();
            NewLearners = Learners - StartCount;
            var run = 0;
            var cumulative = daily.Select(v => (double)(run += v)).ToList();
            GrowthBadge = "+" + run + " sessions";
            CumulativeChart = Charts.GreenArea("id-area-cmp", cumulative, "Cumulative learning sessions by your learners over the last 30 days").ToString();
            // 7-day rolling quiz average; days before the first score repeat it.
            var rolling = days.Select((d, i) =>
            {
                var window = quiz.Where(a => a.At.Date >= days[Math.Max(0, i - 6)] && a.At.Date <= d).ToList();
                return window.Count == 0 ? (double?)null : Charts.Avg(window.Select(a => a.Score));
            }).ToList();
            double? last = null;
            var filled = rolling.Select(v => v ?? last).Select(v => { if (v != null) last = v; return v; }).ToList();
            var first = filled.FirstOrDefault(v => v != null) ?? 0;
            QuizChart = Charts.GreenArea("id-area-qz", filled.Select(v => v ?? first).ToList(), "Practice quiz average over the last 30 days, 7-day rolling", 100).ToString();

            var programs = data.Programs.Where(p => courses.Any(c => c.ProgramId == p.Id)).ToList();
            ProgramList = programs.Select(p =>
            {
                var cs = courses.Where(c => c.ProgramId == p.Id).ToList();
                var learners = enrolments.Where(x => cs.Any(c => c.Id == x.CourseId)).Select(x => x.UserId).Distinct().Select(id => data.User(id)).Where(u => u != null).ToList();
                return new
                {
                    p.Id, p.Name, LearnerCount = learners.Count,
                    FirstLearner = learners.Count > 0 ? Ui.Avatar(learners[0]).ToString() : "<span class=\"id-empty-person\">" + Ui.H(p.Name.Substring(0, 1)) + "</span>",
                    CardsHtml = string.Concat(cs.Select(Card))
                };
            });
            NoCourses = programs.Count == 0;
        }

        static string TechKey(CourseRow c)
        {
            var t = (c.Title + " " + c.Icon).ToLowerInvariant();
            if (Regex.IsMatch(t, @"\bapi\b|\brest\b")) return "api";
            if (Regex.IsMatch(t, @"c#|csharp|\.net")) return "csharp";
            if (Regex.IsMatch(t, @"mongo|sql|database|\bdb\b")) return "db";
            if (Regex.IsMatch(t, @"network|\bweb\b|http|html")) return "web";
            if (Regex.IsMatch(t, @"\bai\b|\bmodels?\b|machine")) return "ai";
            if (Regex.IsMatch(t, @"\bdata\b|\bds\b|analy")) return "data";
            if (Regex.IsMatch(t, @"computer|hardware|system|\bcpu\b")) return "chip";
            return "code";
        }

        string Card(CourseRow c)
        {
            var data = Host.Data;
            var es = data.Enrollments.Where(x => x.CourseId == c.Id).ToList();
            var value = Charts.Avg(es.Select(x => Host.Progress(x.UserId, c.Id)));
            var people = es.Select(x => x.UserId).Distinct().Select(id => data.User(id)).Where(u => u != null).ToList();
            var lit = (int)Math.Round(value * 32 / 100.0);
            var key = TechKey(c);
            var lessons = data.LessonsOf(c.Id).Count;
            var questions = data.QuizFor(c.Id)?.Questions.Count ?? 0;
            var segments = string.Concat(Enumerable.Range(0, 32).Select(i => "<i class=\"" + (i < lit ? "is-lit" : "") + "\" style=\"--i:" + i + "\"></i>"));
            var avatars = string.Concat(people.Take(4).Select(p => Ui.Avatar(p))) + (people.Count > 4 ? "<b>+" + (people.Count - 4) + "</b>" : "") + (people.Count == 0 ? "<span>No learners yet</span>" : "");
            return "<article class=\"id-course-card\"><a class=\"id-course-title\" href=\"CourseEditor?id=" + c.Id + "\"><span class=\"id-course-logo id-tech-" + key + "\" aria-hidden=\"true\">" + Glyphs[key] + "</span><h3>" + Ui.H(c.Title) + "</h3></a>"
                + "<div class=\"id-segments\" role=\"progressbar\" aria-label=\"" + Ui.H(c.Title) + " average completion\" aria-valuenow=\"" + value + "\" aria-valuemin=\"0\" aria-valuemax=\"100\" title=\"" + value + "% average completion\">" + segments + "</div>"
                + "<ul class=\"id-course-facts\"><li>" + Lessons + Ui.Plural(lessons, "lesson") + "</li><li>" + Deadline + (c.Deadline == null ? "No deadline" : "Due " + c.Deadline.Value.ToString("MM.dd.yyyy", CultureInfo.InvariantCulture)) + "</li>"
                + "<li>" + Questions + Ui.Plural(questions, "question") + "</li><li>" + People + value + "% · " + es.Count + " enrolled</li></ul>"
                + "<footer><a class=\"id-learner-avatars\" href=\"Learners?course=" + c.Id + "\" aria-label=\"View enrolled learners for " + Ui.H(c.Title) + "\">" + avatars + "</a><span class=\"id-card-status\">" + Ui.H(c.Status) + "</span>"
                + "<a class=\"id-manage-course\" href=\"CourseEditor?id=" + c.Id + "\" aria-label=\"Manage " + Ui.H(c.Title) + "\" title=\"Manage course\">" + More + "</a></footer></article>";
        }
    }
}
