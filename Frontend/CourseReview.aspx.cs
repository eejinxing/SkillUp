using System;
using System.Globalization;
using System.Linq;
using System.Text;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend
{
    /// <summary>
    /// Program leader: one submitted course (?id=course) with its outcomes, activities, quiz answers and files,
    /// the decision form (approve, request changes or reject) and the course's review history.
    /// </summary>
    public partial class CourseReviewPage : SkillPage
    {
        public override string Route => "review";
        protected override string[] Roles => new[] { "Program Leader" };

        protected CourseRow C;
        protected string Submission, History;
        RequestRow pendingRequest;

        protected void Page_Load(object sender, EventArgs e)
        {
            var id = QueryInt("id");
            if (id == null) Go("Approvals");
            C = OwnCourses.FirstOrDefault(c => c.Id == id);
            if (C == null)
            {
                Denied.Visible = true;
                Review.Visible = false;
                return;
            }
            Files.CourseId = C.Id;
            var lessons = Data.LessonsOf(C.Id);
            var questions = Data.QuizFor(C.Id)?.Questions;
            var html = new StringBuilder("<div class=\"row between\"><h2>" + Ui.H(C.Title) + "</h2>" + Ui.Tag(C.Status) + "</div><p>" + Ui.H(C.Description) + "</p>");
            html.Append("<div><h3>Learning outcomes</h3><p style=\"white-space:pre-line\">").Append(Ui.H(C.Outcomes)).Append("</p></div><div><h3>Course structure</h3>");
            foreach (var l in lessons)
            {
                html.Append("<details class=\"list-item\"><summary>").Append(Ui.H(l.Title)).Append(" &middot; ").Append(Ui.H(l.Type)).Append("</summary><p style=\"white-space:pre-line\">").Append(Ui.H(l.Body)).Append("</p>");
                if (l.Type == "lab" && !string.IsNullOrEmpty(l.Answer)) html.Append("<small>Expected answer: ").Append(Ui.H(l.Answer)).Append("</small>");
                html.Append("</details>");
            }
            html.Append("</div><div><h3>Assessment &middot; ").Append(questions?.Count ?? 0).Append(" questions</h3>");
            foreach (var q in questions ?? Enumerable.Empty<QuestionRow>())
                html.Append("<div class=\"list-item\"><strong>").Append(Ui.H(q.Text)).Append("</strong><p>Correct answer: ").Append(Ui.H(q.Options.FirstOrDefault(o => o.Correct)?.Text)).Append("</p></div>");
            Submission = html.Append("</div>").ToString();

            var events = Data.Requests.Where(r => r.CourseId == C.Id && r.Kind == "Create").SelectMany(r => r.Events).Where(x => x.Action != "Review started")
                .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).ToList();
            History = events.Count == 0 ? "<p>No reviews yet.</p>" : string.Concat(events.Select(x => "<div class=\"list-item\">" + Ui.Tag(x.Action) + "<p>" + Ui.H(x.Comment) + "</p><small>"
                + Ui.H(Data.User(x.ActorId)?.Name ?? "Former user") + " &middot; " + Ui.Date(x.CreatedAt) + "</small></div>"));

            pendingRequest = Data.Requests.FirstOrDefault(r => r.CourseId == C.Id && r.Kind == "Create" && r.Status == "Pending review");
            StartReview.Visible = pendingRequest != null && !pendingRequest.Events.Any(x => x.Action == "Review started");
            DecisionForm.Visible = C.Status == "Pending review";
            NotWaiting.Visible = !DecisionForm.Visible;
        }

        protected void StartReview_Click(object sender, EventArgs e)
        {
            if (pendingRequest != null && TryRun("courseRequestSeen", new { id = pendingRequest.Id.ToString(CultureInfo.InvariantCulture) })) ReloadPage("Review started");
        }

        protected void SubmitDecision_Click(object sender, EventArgs e)
        {
            if (IsValid && TryRun("review", new { courseId = C.Id.ToString(CultureInfo.InvariantCulture), decision = Decision.SelectedValue, comment = Feedback.Text }))
                ReloadPage("Decision submitted");
        }
    }
}
