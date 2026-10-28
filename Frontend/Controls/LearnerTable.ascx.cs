using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend.Controls
{
    /// <summary>
    /// Learners table: person, course, completion, status and last activity of each enrolment, with a link to the
    /// learner's record. Instructors can also remove an enrolment. Set Enrollments before the control loads.
    /// </summary>
    public partial class LearnerTable : UserControl
    {
        public IEnumerable<EnrollmentRow> Enrollments { get; set; }

        SkillPage Host => (SkillPage)Page;
        protected bool CanRemove => Host.Me?.Role == "Instructor";

        protected void Page_Load(object sender, EventArgs e)
        {
            // On a postback the rows come back from view state, so Remove acts on the row the viewer saw.
            if (IsPostBack) return;
            var lines = Host.LearnerLines(Enrollments ?? Enumerable.Empty<EnrollmentRow>());
            Rows.DataSource = lines;
            Rows.DataBind();
            Rows.Visible = lines.Count > 0;
            Empty.Visible = lines.Count == 0;
        }

        protected void Remove_Command(object sender, CommandEventArgs e)
        {
            var ids = e.CommandArgument.ToString().Split(':');
            if (Host.TryRun("enrollmentSave", new { courseId = ids[1], userId = ids[0], remove = true })) Host.ReloadPage("Enrolment removed");
        }
    }
}
