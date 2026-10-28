using System;
using System.Linq;

namespace SKILL_UP.Frontend
{
    public partial class InstructorsPage : SkillPage
    {
        public override string Route => "instructors";
        public override PageLayout Layout => PageLayout.Public;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Visitors see the instructors behind published courses; signed-in accounts see every active instructor.
            Team.DataSource = Data.Users
                .Where(u => u.Role == "Instructor" && u.Status == "Active")
                .Select(u => new
                {
                    User = u,
                    Bio = string.IsNullOrWhiteSpace(u.Bio) ? "Practical learning guided by an experienced instructor." : u.Bio,
                    Published = Data.Courses.Count(c => c.InstructorId == u.Id && c.Status == "Published")
                })
                .Where(x => Me != null || x.Published > 0)
                .ToList();
            Team.DataBind();
        }
    }
}
