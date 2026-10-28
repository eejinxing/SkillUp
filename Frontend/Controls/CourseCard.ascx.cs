using System;
using System.Linq;
using System.Web.UI;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend.Controls
{
    /// <summary>A course tile: cover art, level bars, status, instructor, activity count, and the learner's progress when enrolled.</summary>
    public partial class CourseCard : UserControl
    {
        public CourseRow Course { get; set; }
        /// <summary>Staff view: links to the course editor and shows the course status instead of Enrolled/Open.</summary>
        public bool Manage { get; set; }

        protected string Url, Art, InstructorName;
        protected int Level, Activities, Progress;
        protected bool Enrolled;

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            var page = (SkillPage)Page;
            Url = (Manage ? "CourseEditor?id=" : "Course?id=") + Course.Id;
            Art = page.CourseArt(Course);
            Level = Course.Difficulty == "Advanced" ? 3 : Course.Difficulty == "Intermediate" ? 2 : Course.Difficulty == "Beginner" ? 1 : 0;
            InstructorName = page.Data.User(Course.InstructorId)?.Name ?? "Former user";
            Activities = page.Data.LessonsOf(Course.Id).Count;
            Enrolled = page.Me != null && page.Data.Enrollments.Any(x => x.UserId == page.Me.Id && x.CourseId == Course.Id);
            if (Enrolled) Progress = page.Progress(page.Me.Id, Course.Id);
        }
    }
}
