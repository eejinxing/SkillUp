using System;
using System.Linq;

namespace SKILL_UP.Frontend
{
    /// <summary>Every activity of the instructor's courses, with links to edit them, and the course files.</summary>
    public partial class ResourcesPage : SkillPage
    {
        public override string Route => "resources";
        protected override string[] Roles => new[] { "Instructor" };
        public override bool Secondary => true;

        protected void Page_Load(object sender, EventArgs e)
        {
            var lessons = OwnCourses.SelectMany(c => Data.LessonsOf(c.Id).Select(l => new
            {
                l.Id, l.CourseId, l.Title, l.Minutes, Course = c.Title, Colour = c.Color ?? "mint", Art = CourseArt(c),
                Kind = l.Type == "article" ? "DOC" : l.Type == "lab" ? "LAB" : "QUIZ",
                Audience = l.Preview ? "Public preview" : "Enrolled learners"
            })).ToList();
            Lessons.DataSource = lessons;
            Lessons.DataBind();
            NoLessons.Visible = lessons.Count == 0;
        }
    }
}
