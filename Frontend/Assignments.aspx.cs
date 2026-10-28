using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend
{
    /// <summary>
    /// Program leader: one card per course in the leader's programs with its instructor, learners and progress
    /// (?q= searches). ?assign=course opens the form that changes the course's instructor.
    /// </summary>
    public partial class AssignmentsPage : SkillPage
    {
        static readonly Dictionary<string, string> Tones = new Dictionary<string, string> { ["DB"] = "#16a34a", ["C#"] = "#2563eb", ["API"] = "#f97316", ["DS"] = "#0d9488" };

        static string Svg(string paths) =>
            "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">" + paths + "</svg>";

        static readonly Dictionary<string, string> Ic = new Dictionary<string, string>
        {
            ["user"] = Svg("<circle cx=\"12\" cy=\"8\" r=\"4\"/><path d=\"M4 21a8 8 0 0 1 16 0\"/>"),
            ["people"] = Svg("<circle cx=\"9\" cy=\"8\" r=\"3.5\"/><path d=\"M2.5 20a6.5 6.5 0 0 1 13 0M16 4.5a3.5 3.5 0 0 1 0 7M18 14a6 6 0 0 1 3.5 6\"/>"),
            ["chart"] = Svg("<path d=\"M4 20V10M10 20V4M16 20v-7M22 20H2\"/>"),
            ["list"] = Svg("<path d=\"M9 6h11M9 12h11M9 18h11M4 6h.01M4 12h.01M4 18h.01\"/>"),
            ["cal"] = Svg("<rect x=\"4\" y=\"5\" width=\"16\" height=\"15\" rx=\"2\"/><path d=\"M4 10h16M9 3v4M15 3v4\"/>"),
            ["mail"] = Svg("<rect x=\"3\" y=\"5\" width=\"18\" height=\"14\" rx=\"2\"/><path d=\"m3 7 9 6 9-6\"/>"),
            ["book"] = Svg("<rect x=\"5\" y=\"3\" width=\"14\" height=\"18\" rx=\"2\"/><path d=\"M9 7h6M9 11h6M9 15h4\"/>"),
            ["lab"] = Svg("<path d=\"M9 3h6M10 3v7L5 19a1 1 0 0 0 1 2h12a1 1 0 0 0 1-2l-5-9V3M8 15h8\"/>"),
            ["quiz"] = Svg("<path d=\"m6 12 4 4 8-8\"/>"),
            ["status"] = Svg("<circle cx=\"12\" cy=\"12\" r=\"8\"/><path d=\"m8 12 3 3 5-6\"/>"),
            ["chevron"] = Svg("<path d=\"m8 14 4-4 4 4\"/>")
        };

        public override string Route => "assignments";
        protected override string[] Roles => new[] { "Program Leader" };
        public override bool Secondary => true;

        protected string Summary, Cards;
        protected CourseRow AssignCourse;
        string term;

        protected string ListUrl => "Assignments" + (term.Length == 0 ? "" : "?q=" + HttpUtility.UrlEncode(term));

        protected void Page_Load(object sender, EventArgs e)
        {
            term = (Request.QueryString["q"] ?? "").Trim();
            if (!IsPostBack) SearchBox.Text = term;
            var courses = OwnCourses;
            AssignCourse = courses.FirstOrDefault(c => c.Id == QueryInt("assign"));
            if (AssignCourse != null)
            {
                AssignForm.Visible = true;
                if (!IsPostBack)
                {
                    var options = new List<object> { new { Id = "", Name = "Unassigned" } };
                    options.AddRange(Data.Users.Where(u => u.Role == "Instructor" && u.Status == "Active").Select(u => new { Id = u.Id.ToString(CultureInfo.InvariantCulture), u.Name }));
                    AssignInstructor.DataSource = options;
                    AssignInstructor.DataBind();
                    AssignInstructor.SelectedValue = AssignCourse.InstructorId?.ToString(CultureInfo.InvariantCulture) ?? "";
                }
            }

            var people = courses.Where(c => c.InstructorId != null).Select(c => c.InstructorId).Distinct().Count();
            var unassigned = courses.Count(c => Data.User(c.InstructorId) == null);
            Summary = "<span><b>" + courses.Count + "</b> courses</span><span><b>" + people + "</b> " + (people == 1 ? "instructor" : "instructors") + "</span><span"
                + (unassigned > 0 ? " class=\"is-warn\"" : "") + "><b>" + unassigned + "</b> unassigned</span>";
            if (courses.Count == 0)
            {
                Cards = "<div class=\"asg-empty\"><h2>No courses to assign yet</h2><p>Courses in your programs will appear here.</p></div>";
                return;
            }
            var shown = courses.Where(c =>
            {
                var instructor = Data.User(c.InstructorId);
                var text = string.Join(" ", new[] { c.Title, Data.Program(c.ProgramId)?.Name, instructor?.Name ?? "unassigned", instructor?.Email }.Where(s => !string.IsNullOrEmpty(s)));
                return text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
            }).ToList();
            Cards = shown.Count == 0 ? "<p class=\"asg-empty\" id=\"asg-empty\">No matching courses or instructors.</p>"
                : "<div class=\"asg-grid\" id=\"asg-grid\">" + string.Concat(shown.Select(Card)) + "</div>";
        }

        static string Initials(string name) => Ui.H(string.Concat((name ?? "?").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => w[0])));

        static string Chip(string text, string cls = "") => "<span class=\"asg-chip " + cls + "\" title=\"" + Ui.H(text) + "\"><span class=\"asg-chip-text\">" + Ui.H(text) + "</span></span>";

        static string Row(string icon, string label, string value) => "<div><dt>" + Ic[icon] + label + "</dt><dd>" + value + "</dd></div>";

        string Card(CourseRow c)
        {
            var program = Data.Program(c.ProgramId);
            var instructor = Data.User(c.InstructorId);
            var es = Data.Enrollments.Where(x => x.CourseId == c.Id).ToList();
            var completion = Charts.Avg(es.Select(x => Progress(x.UserId, x.CourseId)));
            var lessons = Data.LessonsOf(c.Id);
            var questions = Data.QuizFor(c.Id)?.Questions.Count ?? 0;
            var status = c.Status ?? "Draft";
            string tone;
            if (!Tones.TryGetValue((c.Icon ?? "").ToUpperInvariant(), out tone)) tone = "#7c3aed";
            var next = instructor == null ? "Assign an instructor to manage this course and support its learners."
                : status == "Pending review" ? "This course is awaiting publication review. Open the course to check its content."
                : status == "Draft" ? "Review the course with its instructor before submitting it for publication."
                : status == "Hidden" ? "This course is hidden from learners. Open the course to review its availability."
                : "Contact " + instructor.Name + " for a course update, or change the assignment below.";
            var segments = new StringBuilder();
            for (int i = 0; i < 10; i++) segments.Append("<i style=\"--fill:").Append(Math.Min(100, Math.Max(0, completion * 10 - i * 100))).Append("%\"></i>");
            var subtitle = (program?.Name ?? "No program") + " · " + (instructor?.Name ?? "Unassigned");
            var badge = (c.Icon ?? "UP").Length > 2 ? (c.Icon ?? "UP").Substring(0, 2) : (c.Icon ?? "UP");

            var html = new StringBuilder();
            html.Append("<details class=\"asg-card\" open data-asg-course=\"").Append(c.Id).Append("\"><summary class=\"asg-card-head\"><span class=\"asg-face\" aria-hidden=\"true\">").Append(Initials(instructor?.Name))
                .Append("<span class=\"asg-course-badge\" style=\"--tone:").Append(tone).Append("\">").Append(Ui.H(badge)).Append("</span></span><span class=\"asg-heading\"><span class=\"asg-title\" title=\"")
                .Append(Ui.H(c.Title)).Append("\">").Append(Ui.H(c.Title)).Append("</span><span class=\"asg-subtitle\" title=\"").Append(Ui.H(subtitle)).Append("\">").Append(Ui.H(subtitle))
                .Append("</span></span><span class=\"asg-ring").Append(completion == 100 ? " is-complete" : "").Append("\" style=\"--progress:").Append(completion).Append("%\" role=\"img\" aria-label=\"")
                .Append(completion).Append("% average completion\"><b>").Append(completion).Append("%</b></span><span class=\"asg-toggle\">").Append(Ic["chevron"]).Append("</span></summary>");
            html.Append("<div class=\"asg-card-body\"><dl class=\"asg-rows\">");
            html.Append(Row("user", "Instructor", instructor != null ? "<span class=\"asg-chip\"><span class=\"asg-avatar\">" + Initials(instructor.Name) + "</span><span class=\"asg-chip-text\">" + Ui.H(instructor.Name) + "</span></span>" : Chip("Unassigned", "is-warn")));
            html.Append(Row("mail", "Email", Chip(instructor?.Email ?? "Not assigned")));
            html.Append(Row("book", "Course", "<span class=\"asg-chip\"><span class=\"asg-course-dot\" style=\"--tone:" + tone + "\">" + Ic["book"] + "</span><span class=\"asg-chip-text\" title=\"" + Ui.H(c.Title) + "\">" + Ui.H(c.Title) + "</span></span>"));
            html.Append(Row("people", "Learners", Chip(es.Count + " enrolled")));
            html.Append(Row("chart", "Completion", "<span class=\"asg-meter" + (completion == 100 ? " is-complete" : "") + "\" role=\"img\" aria-label=\"" + completion + "% average completion\">" + segments + "</span><b class=\"asg-percent\">" + completion + "%</b>"));
            html.Append(Row("list", "Activities", Chip(Ui.Plural(lessons.Count, "activity", "activities") + " · " + Ui.Plural(questions, "question"))));
            html.Append(Row("cal", "Due date", Chip(c.Deadline == null ? "Not set" : Ui.Date(c.Deadline))));
            html.Append(Row("status", "Status", Chip(status, status == "Published" ? "is-live" : status == "Pending review" ? "is-warn" : "")));
            html.Append("</dl><section class=\"asg-activities\" aria-label=\"Course activities\"><h3>Activities</h3><div class=\"asg-activity-list\">");
            if (lessons.Count == 0) html.Append("<span class=\"asg-muted\">No activities added yet.</span>");
            foreach (var l in lessons)
            {
                var kind = l.Type == "lab" ? "lab" : l.Type == "quiz" ? "quiz" : "article";
                html.Append("<span class=\"asg-activity\" title=\"").Append(Ui.H(l.Title + " · " + (l.Type ?? "article"))).Append("\"><span class=\"asg-activity-icon is-").Append(kind).Append("\">")
                    .Append(Ic[kind == "article" ? "book" : kind]).Append("</span>").Append(Ui.H(l.Title)).Append("</span>");
            }
            html.Append("</div></section><section class=\"asg-next\"><h3>Next step</h3><p>").Append(Ui.H(next)).Append("</p></section></div>");
            html.Append("<footer class=\"asg-footer\"><strong>Follow up?</strong><div><a class=\"asg-view\" href=\"CourseEditor?id=").Append(c.Id).Append("\">View</a>");
            if (instructor != null) html.Append("<a class=\"asg-action\" href=\"Messages?id=").Append(instructor.Id).Append("\">Message</a>");
            html.Append("<a class=\"asg-action\" href=\"").Append(HttpUtility.HtmlAttributeEncode("Assignments?assign=" + c.Id + (term.Length == 0 ? "" : "&q=" + HttpUtility.UrlEncode(term)))).Append("#asg-assign\">Change assignment</a></div></footer></details>");
            return html.ToString();
        }

        protected void Search_Click(object sender, EventArgs e)
        {
            term = SearchBox.Text.Trim();
            Go(ListUrl);
        }

        protected void Assign_Click(object sender, EventArgs e)
        {
            if (AssignCourse != null && TryRun("assign", new { courseId = AssignCourse.Id.ToString(CultureInfo.InvariantCulture), instructorId = AssignInstructor.SelectedValue }))
                Go(ListUrl, "Instructor assignment saved");
        }
    }
}
