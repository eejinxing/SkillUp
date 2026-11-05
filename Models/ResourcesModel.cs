using System;
using System.Linq;

namespace SkillUp.Models
{
    /// <summary>Every activity of the instructor's courses, with links to edit them, and the course files.</summary>
    public sealed class ResourcesModel : PageModel
    {
        public ResourcesModel(SkillContext ctx) : base(ctx) { }
        public IEnumerable<dynamic> LessonList;
        public bool NoLessons;


        public void Load()
        {
            var lessons = OwnCourses.SelectMany(c => Data.LessonsOf(c.Id).Select(l => new
            {
                l.Id, l.CourseId, l.Title, l.Minutes, Course = c.Title, Colour = c.Color ?? "mint", Art = CourseArt(c),
                Kind = l.Type == "article" ? "DOC" : l.Type == "lab" ? "LAB" : "QUIZ",
                Audience = l.Preview ? "Public preview" : "Enrolled learners"
            })).ToList();
            LessonList = lessons;
            NoLessons = lessons.Count == 0;
        }
    }
}
