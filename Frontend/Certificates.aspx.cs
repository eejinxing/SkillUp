using System;
using System.Linq;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend
{
    /// <summary>Certificates for courses the learner has completed (every activity done); ?id= shows one to print.</summary>
    public partial class CertificatesPage : SkillPage
    {
        public override string Route => "certificates";
        protected override string[] Roles => new[] { "Learner" };
        public override bool Secondary => true;

        protected CourseRow Shown;

        protected void Page_Load(object sender, EventArgs e)
        {
            var completed = AccessibleCourses.Where(c => Data.LessonsOf(c.Id).Count > 0 && Progress(Me.Id, c.Id) == 100).ToList();
            var id = QueryInt("id");
            if (id != null)
            {
                Shown = completed.FirstOrDefault(c => c.Id == id);
                Views.ActiveViewIndex = Shown == null ? 2 : 1;
                return;
            }
            Earned.DataSource = completed.Select(c => new { c.Id, c.Title, Lessons = Data.LessonsOf(c.Id).Count });
            Earned.DataBind();
            NoneYet.Visible = completed.Count == 0;
        }
    }
}
