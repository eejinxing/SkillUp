using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend
{
    /// <summary>
    /// Enrolment, completion and quiz figures for the courses this account looks after, by program and by course
    /// (?filter=active|empty, ?more=1 shows every course). Export CSV downloads the per-course figures.
    /// </summary>
    public partial class AnalyticsPage : SkillPage
    {
        public override string Route => "analytics";
        protected override string[] Roles => new[] { "Instructor", "Program Leader" };
        public override bool Secondary => true;

        protected string Filter = "all", Stats, ProgramRows, CourseRows;
        protected bool Expanded;
        List<CourseRow> courses;
        List<EnrollmentRow> enrolments;
        List<Insights.Attempt> quizzes;

        protected void Page_Load(object sender, EventArgs e)
        {
            Filter = Request.QueryString["filter"] == "active" || Request.QueryString["filter"] == "empty" ? Request.QueryString["filter"] : "all";
            Expanded = Request.QueryString["more"] == "1";
            courses = OwnCourses;
            var ids = new HashSet<int>(courses.Select(c => c.Id));
            enrolments = Data.Enrollments.Where(x => ids.Contains(x.CourseId)).ToList();
            quizzes = Insights.Attempts(this).Where(a => a.Type == "quiz" && ids.Contains(a.CourseId)).ToList();

            var started = enrolments.Count(x => Data.CompletedLessons(x.UserId, x.CourseId).Count > 0);
            var rows = courses.Select(c =>
            {
                var ce = enrolments.Where(x => x.CourseId == c.Id).ToList();
                var scores = quizzes.Where(a => a.CourseId == c.Id).Select(a => a.Score).ToList();
                return new
                {
                    Course = c, Program = Data.Program(c.ProgramId), Enrolled = ce.Count,
                    Participation = Ui.Percent(ce.Count(x => Data.CompletedLessons(x.UserId, x.CourseId).Count > 0), ce.Count),
                    Completion = Charts.Avg(ce.Select(x => Progress(x.UserId, x.CourseId))),
                    Quiz = scores.Count == 0 ? (int?)null : Charts.Avg(scores)
                };
            }).ToList();

            Func<string, int, string, string, bool, string> stat = (label, value, suffix, note, good) =>
                "<article class=\"kx-stat" + (good ? " is-good" : "") + "\"><header><span>" + label + "</span><span class=\"kx-dots\" aria-hidden=\"true\">&#8942;</span></header><strong>" + (good ? "+" : "")
                + "<span data-count=\"" + value + "\">" + value + "</span>" + suffix + "</strong><small>" + note + "</small></article>";
            Stats = stat("Enrolments", enrolments.Count, "", "Across " + rows.Count(r => r.Enrolled > 0) + " of " + courses.Count + " courses", false)
                + stat("Average completion", Charts.Avg(enrolments.Select(x => Progress(x.UserId, x.CourseId))), "%", "Unstarted enrolments included", false)
                + stat("Quiz average", Charts.Avg(quizzes.Select(a => a.Score)), "%", quizzes.Count + " submitted " + (quizzes.Count == 1 ? "attempt" : "attempts"), false)
                + stat("Participation", Ui.Percent(started, enrolments.Count), "%", started + " of " + enrolments.Count + " started", true);

            const string dash = "<span class=\"kx-muted\">&mdash;</span>";
            Func<int, bool, string> val = (n, ok) => ok ? n + "%" : dash;
            Func<int?, string> score = v => v == null ? "<span class=\"kx-muted\">No attempts</span>" : v + "%";

            var programs = new StringBuilder();
            foreach (var programId in rows.Select(r => r.Course.ProgramId).Distinct())
            {
                var pr = rows.Where(r => r.Course.ProgramId == programId).ToList();
                var pe = enrolments.Where(x => pr.Any(r => r.Course.Id == x.CourseId)).ToList();
                var pa = quizzes.Where(a => pr.Any(r => r.Course.Id == a.CourseId)).Select(a => a.Score).ToList();
                var name = Data.Program(programId)?.Name;
                programs.Append("<tr><td><span class=\"kx-asset\"><i aria-hidden=\"true\">").Append(Ui.H(Initials(name))).Append("</i><span><b>").Append(Ui.H(name ?? "Unassigned"))
                    .Append("</b><small>Program</small></span></span></td><td>").Append(pr.Count).Append("</td><td>").Append(pe.Count).Append("</td><td>")
                    .Append(val(Charts.Avg(pe.Select(x => Progress(x.UserId, x.CourseId))), pe.Count > 0)).Append("</td><td>").Append(score(pa.Count == 0 ? (int?)null : Charts.Avg(pa)))
                    .Append("</td><td><div class=\"kx-actions\"><a class=\"kx-btn\" href=\"Learners\">Learners</a></div></td></tr>");
            }
            ProgramRows = programs.Length > 0 ? programs.ToString() : "<tr><td colspan=\"6\" class=\"kx-muted\">No programs yet.</td></tr>";

            var shown = rows.Where(r => Filter == "all" || (Filter == "active") == (r.Enrolled > 0)).ToList();
            var list = new StringBuilder();
            for (int i = 0; i < shown.Count; i++)
            {
                var r = shown[i];
                list.Append("<tr data-kx-row=\"").Append(r.Enrolled > 0 ? "active" : "empty").Append("\"").Append(i >= 6 ? " class=\"kx-extra\"" : "")
                    .Append("><td><span class=\"kx-asset\"><i aria-hidden=\"true\">").Append(Ui.H(Initials(r.Course.Icon ?? r.Course.Title))).Append("</i><span><b>").Append(Ui.H(r.Course.Title))
                    .Append("</b><small>").Append(Ui.H(r.Program?.Name ?? "Course")).Append("</small></span></span></td><td>").Append(r.Enrolled).Append("</td><td>")
                    .Append(val(r.Participation, r.Enrolled > 0)).Append("</td><td>").Append(val(r.Completion, r.Enrolled > 0)).Append("</td><td>").Append(score(r.Quiz))
                    .Append("</td><td><div class=\"kx-actions\"><a class=\"kx-btn\" href=\"Learners?course=").Append(r.Course.Id).Append("\">Learners</a><a class=\"kx-btn\" href=\"Course?id=")
                    .Append(r.Course.Id).Append("\">Open</a></div></td></tr>");
            }
            CourseRows = list.Length > 0 ? list.ToString() : "<tr><td colspan=\"6\" class=\"kx-muted\">No courses yet.</td></tr>";
            MoreButton.Visible = rows.Count > 6;
            foreach (var pair in new[] { Tuple.Create(FilterAll, "all"), Tuple.Create(FilterActive, "active"), Tuple.Create(FilterEmpty, "empty") })
                pair.Item1.Attributes["aria-pressed"] = pair.Item2 == Filter ? "true" : "false";
        }

        /// <summary>A short label of up to three characters stays as it is (a course icon such as "C#"); otherwise the initials of the first two words.</summary>
        static string Initials(string text)
        {
            if (Regex.IsMatch(text ?? "", @"^\S{1,3}$")) return text;
            return string.Concat((text ?? "?").Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Where(w => char.IsLetterOrDigit(w[0])).Take(2).Select(w => w[0])).ToUpperInvariant();
        }

        protected void Filter_Command(object sender, CommandEventArgs e) => Go("Analytics?filter=" + e.CommandArgument);

        protected void More_Click(object sender, EventArgs e) => Go("Analytics?filter=" + Filter + "&more=1");

        protected void Export_Click(object sender, EventArgs e) =>
            SendCsv("skillup-analytics.csv", new[] { new object[] { "Course", "Enrolments", "Completion percent", "Average quiz score" } }.Concat(courses.Select(c =>
            {
                var ce = enrolments.Where(x => x.CourseId == c.Id).ToList();
                return new object[] { c.Title, ce.Count, Charts.Avg(ce.Select(x => Progress(x.UserId, x.CourseId))), Charts.Avg(quizzes.Where(a => a.CourseId == c.Id).Select(a => a.Score)) };
            })));
    }
}
