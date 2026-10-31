using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend
{
    /// <summary>
    /// The learner's courses grouped by published program (?program=). Without a choice it opens the first program
    /// the learner is enrolled in.
    /// </summary>
    public partial class MyCoursesPage : SkillPage
    {
        public override string Route => "learning";
        protected override string[] Roles => new[] { "Learner" };

        const string OpenedKey = "SkillUp.MyCourses.Opened";

        protected ProgramRow Selected;

        /// <summary>The program whose pack was opened on the previous request: its light fades out over this page, once.</summary>
        int? justOpened;

        public override string BodyClass => justOpened == null ? "" : "mc-kit-arrival";

        protected void Page_Load(object sender, EventArgs e)
        {
            justOpened = Session[OpenedKey] as int?;
            Session.Remove(OpenedKey);
            var programs = Data.Programs.Where(p => p.Published).ToList();
            var enrolled = new HashSet<int>(MyEnrollments.Select(x => x.CourseId));
            var chosen = QueryInt("program");
            Selected = programs.FirstOrDefault(p => p.Id == chosen)
                ?? programs.FirstOrDefault(p => CoursesOf(p).Any(c => enrolled.Contains(c.Id)))
                ?? programs.FirstOrDefault();
            if (Selected == null) return;

            ProgramTabs.DataSource = programs.Select(p => new { p.Id, p.Name, Selected = p.Id == Selected.Id, Enrolled = CoursesOf(p).Any(c => enrolled.Contains(c.Id)), HasCourses = CoursesOf(p).Any(), JustOpened = p.Id == justOpened });
            ProgramTabs.DataBind();
            var courses = CoursesOf(Selected);
            CourseList.DataSource = courses.Select((c, i) =>
            {
                var enrolment = MyEnrollments.FirstOrDefault(x => x.CourseId == c.Id);
                var lessons = Data.LessonsOf(c.Id);
                var done = enrolment == null ? new List<int>() : Data.CompletedLessons(Me.Id, c.Id);
                var value = enrolment == null ? 0 : Ui.Percent(done.Count, lessons.Count);
                var next = enrolment == null ? null : lessons.FirstOrDefault(l => !done.Contains(l.Id));
                return new
                {
                    c.Id, c.Title, c.Difficulty, Percent = value, Done = done.Count, Lessons = lessons.Count,
                    Status = enrolment == null ? "Not enrolled" : value == 100 ? "Completed" : value > 0 ? "In Progress" : "Not started",
                    Due = c.Deadline?.ToString("MMM d, yyyy", CultureInfo.GetCultureInfo("en-US")) ?? "—",
                    Art = string.IsNullOrEmpty(c.Thumbnail) ? "assets/my-courses/computer-essentials-wide-" + (i % 4 + 1).ToString("00") + ".webp" : c.Thumbnail,
                    Action = enrolment == null ? "View course" : value == 100 ? "Review course" : "Continue learning",
                    Href = next != null ? "Activity?course=" + c.Id + "&lesson=" + next.Id : "Course?id=" + c.Id + "&from=mycourses",
                    Ring = Ring(value, c.Title)
                };
            }).ToList();
            CourseList.DataBind();
            NoCourses.Visible = courses.Count == 0;
        }

        /// <summary>
        /// Enroll button on a program pouch: enrols the learner in every course of the program that they are not in yet, and
        /// opens that program. js/pack-opening.js presses this button when a pouch has been torn open.
        /// </summary>
        protected void EnrolProgram_Command(object sender, System.Web.UI.WebControls.CommandEventArgs e)
        {
            int id;
            var program = int.TryParse((string)e.CommandArgument, out id) ? Data.Programs.FirstOrDefault(p => p.Id == id && p.Published) : null;
            var courses = program == null ? new List<CourseRow>() : CoursesOf(program);
            if (courses.Count == 0) { Go("MyCourses", "This program has no open courses yet."); return; }
            var joined = new List<CourseRow>();
            foreach (var course in courses)
            {
                if (MyEnrollments.Any(x => x.CourseId == course.Id)) continue;
                if (!TryRun("enrol", new { courseId = course.Id.ToString() })) return;
                joined.Add(course);
            }
            Session[OpenedKey] = program.Id;
            Go("MyCourses?program=" + program.Id, joined.Count == 0 ? "You’re already enrolled in " + program.Name + "."
                : joined.Count == 1 ? "You’re enrolled in " + joined[0].Title + "."
                : "You’re enrolled in " + joined.Count + " courses of " + program.Name + ".");
        }

        /// <summary>Published courses of a program: those on its learning path first, in path order.</summary>
        List<CourseRow> CoursesOf(ProgramRow p)
        {
            var members = Data.Courses.Where(c => c.ProgramId == p.Id && c.Status == "Published").ToList();
            return Data.PathOf(p.Id).Concat(members.Select(c => c.Id)).Distinct()
                .Select(id => members.FirstOrDefault(c => c.Id == id)).Where(c => c != null).ToList();
        }

        /// <summary>The segmented completion ring: 28 segments, filled in proportion to progress.</summary>
        static string Ring(int value, string title)
        {
            const int count = 28;
            var filled = (int)Math.Round(value * count / 100.0);
            Func<double, double, string> point = (r, degrees) =>
            {
                var a = degrees * Math.PI / 180;
                return (50 + r * Math.Cos(a)).ToString("0.###", CultureInfo.InvariantCulture) + " " + (50 + r * Math.Sin(a)).ToString("0.###", CultureInfo.InvariantCulture);
            };
            var svg = new StringBuilder();
            for (var i = 0; i < count; i++)
            {
                double start = -90 + i * 360.0 / count + 1, end = -90 + (i + 1) * 360.0 / count - 1;
                svg.Append("<path d=\"M " + point(48, start) + " A 48 48 0 0 1 " + point(48, end) + " L " + point(33, end) + " A 33 33 0 0 0 " + point(33, start) + " Z\" fill=\"" + (i < filled ? "#f4f4f5" : "#303033") + "\"/>");
            }
            return "<span class=\"mc-task-ring\" role=\"progressbar\" aria-label=\"" + Ui.H(title) + " completion\" aria-valuemin=\"0\" aria-valuemax=\"100\" aria-valuenow=\"" + value + "\"><svg viewBox=\"0 0 100 100\" aria-hidden=\"true\">" + svg + "</svg><strong>" + value + "<sup>%</sup></strong></span>";
        }
    }
}
