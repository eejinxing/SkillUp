using System;
using System.Globalization;
using System.Linq;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend
{
    /// <summary>Program leader: one deletion request of the leader's programs (?id=request) and its decision form.</summary>
    public partial class RequestReviewPage : SkillPage
    {
        public override string Route => "request-review";
        protected override string[] Roles => new[] { "Program Leader" };

        protected RequestRow R;
        protected string Requester, CourseTitle, History;
        protected int Learners;

        protected void Page_Load(object sender, EventArgs e)
        {
            var id = QueryInt("id");
            if (id == null) Go("Approvals");
            R = Data.Requests.FirstOrDefault(r => r.Id == id && r.Kind == "Delete" && Data.Programs.Any(p => p.Id == r.ProgramId && p.LeaderId == Me.Id));
            if (R == null)
            {
                Denied.Visible = true;
                Review.Visible = false;
                return;
            }
            Requester = Data.User(R.RequesterId)?.Name ?? "Former user";
            CourseTitle = Data.Course(R.CourseId)?.Title ?? "Deleted";
            Learners = Data.Enrollments.Count(x => x.CourseId == R.CourseId);
            History = string.Concat(R.Events.Select(x => "<div class=\"list-item\"><strong>" + Ui.H(x.Action) + "</strong><p>" + Ui.H(x.Comment) + "</p><small>" + Ui.Stamp(x.CreatedAt) + "</small></div>"));
            var pending = R.Status == "Pending review";
            StartReview.Visible = pending && !R.Events.Any(x => x.Action == "Review started");
            DecisionForm.Visible = pending;
            Decided.Visible = !pending;
        }

        protected void StartReview_Click(object sender, EventArgs e)
        {
            if (TryRun("courseRequestSeen", new { id = R.Id.ToString(CultureInfo.InvariantCulture) })) ReloadPage("Review started");
        }

        protected void SubmitDecision_Click(object sender, EventArgs e)
        {
            if (IsValid && TryRun("courseRequestReview", new { id = R.Id.ToString(CultureInfo.InvariantCulture), decision = Decision.SelectedValue, comment = Feedback.Text }))
                ReloadPage("Decision submitted");
        }
    }
}
