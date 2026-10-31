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
        /// <summary>The completion date for the certificate: written here, and rewritten in the viewer's own date and time zone by js/site.js.</summary>
        protected string CompletedOn, CompletedIso, CourseHours;
        /// <summary>
        /// Where the breadcrumb and Close lead. A certificate opened from its course (?from=course) goes back to that course;
        /// one opened from the list goes back to the list.
        /// </summary>
        protected string BackUrl = "Certificates", BackLabel = "Certificates", HereLabel;

        protected void Page_Load(object sender, EventArgs e)
        {
            var completed = AccessibleCourses.Where(c => Data.LessonsOf(c.Id).Count > 0 && Progress(Me.Id, c.Id) == 100).ToList();
            var id = QueryInt("id");
            if (id != null)
            {
                Shown = completed.FirstOrDefault(c => c.Id == id);
                Views.ActiveViewIndex = Shown == null ? 2 : 1;
                if (Shown != null)
                {
                    HereLabel = Shown.Title;
                    // The browser names a saved PDF after the page title, so the file comes out as "Skill UP Certificate 2-1.pdf".
                    Title = "Skill UP Certificate " + Me.Id + "-" + Shown.Id;
                    if (Request.QueryString["from"] == "course") { BackUrl = "Course?id=" + Shown.Id + (Request.QueryString["via"] == "mycourses" ? "&from=mycourses" : ""); BackLabel = Shown.Title; HereLabel = "Certificate"; }
                    var done = MyEnrollments.FirstOrDefault(x => x.CourseId == Shown.Id)?.CompletedAt;
                    // Without a recorded date the certificate shows the day it is opened, by the viewing device's clock.
                    CompletedOn = (done ?? DateTime.Now).ToString("dd MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
                    CompletedIso = done == null ? "today" : done.Value.ToString("s");
                    var hours = Math.Max(1, (int)Math.Ceiling(Data.LessonsOf(Shown.Id).Sum(l => l.Minutes) / 60.0));
                    CourseHours = hours + (hours == 1 ? " Hour" : " Hours");
                }
                return;
            }
            Earned.DataSource = completed.Select(c => new { c.Id, c.Title, Lessons = Data.LessonsOf(c.Id).Count });
            Earned.DataBind();
            NoneYet.Visible = completed.Count == 0;
        }
    }
}
