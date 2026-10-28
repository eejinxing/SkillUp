using System;
using System.Linq;
using System.Web.UI.WebControls;

namespace SKILL_UP.Frontend
{
    /// <summary>Scheduled live sessions for the viewer's courses. Instructors schedule them; everyone joins or leaves.</summary>
    public partial class LiveSessionsPage : SkillPage
    {
        public override string Route => "sessions";
        protected override string[] Roles => new string[0];
        public override bool Secondary => true;

        protected void Page_Load(object sender, EventArgs e)
        {
            var courses = AccessibleCourses.ToDictionary(c => c.Id);
            SchedulePanel.Visible = Me.Role == "Instructor";
            if (!IsPostBack && SchedulePanel.Visible)
            {
                SessionCourse.DataSource = CourseOptions(courses.Values);
                SessionCourse.DataBind();
            }
            var sessions = Data.Channels.Where(s => courses.ContainsKey(s.CourseId)).ToList();
            Sessions.DataSource = sessions.Select(s => new
            {
                s.Id, Title = s.Name, CourseTag = Ui.Tag(courses[s.CourseId].Title),
                When = s.StartsAt == null ? "Time to be confirmed" : Ui.Stamp(s.StartsAt.Value),
                Host = Data.User(s.CreatedBy)?.Name ?? "Former user",
                Attendees = s.Attendees.Count,
                Joined = s.Attendees.Contains(Me.Id)
            });
            Sessions.DataBind();
            NoSessions.Visible = sessions.Count == 0;
        }

        protected void Schedule_Click(object sender, EventArgs e)
        {
            if (IsValid && TryRun("sessionSave", new { courseId = SessionCourse.SelectedValue, title = SessionTitle.Text.Trim(), startsAt = SessionStart.Text })) ReloadPage("Session scheduled");
        }

        protected void Join_Command(object sender, CommandEventArgs e)
        {
            if (TryRun("sessionJoin", new { id = (string)e.CommandArgument })) ReloadPage("Joined the session");
        }

        protected void Leave_Command(object sender, CommandEventArgs e)
        {
            if (TryRun("sessionJoin", new { id = (string)e.CommandArgument, leave = true })) ReloadPage("Left the session");
        }
    }
}
