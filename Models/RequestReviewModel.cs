using System;
using System.Globalization;
using System.Linq;
using SkillUp.Services;

namespace SkillUp.Models
{
    /// <summary>Program leader: one deletion request of the leader's programs (?id=request) and its decision form.</summary>
    public sealed class RequestReviewModel : PageModel
    {
        public RequestReviewModel(SkillContext ctx) : base(ctx) { }
        public bool IsDenied, CanStart, CanDecide;


        public RequestRow R;
        public string Requester, CourseTitle, History;
        public int Learners;

        public void Load()
        {
            var id = QueryInt("id");
            if (id == null) { IsDenied = true; return; }
            R = Data.Requests.FirstOrDefault(r => r.Id == id && r.Kind == "Delete" && Data.Programs.Any(p => p.Id == r.ProgramId && p.LeaderId == Me.Id));
            if (R == null)
            {
                IsDenied = true;
                return;
            }
            Requester = Data.User(R.RequesterId)?.Name ?? "Former user";
            CourseTitle = Data.Course(R.CourseId)?.Title ?? "Deleted";
            Learners = Data.Enrollments.Count(x => x.CourseId == R.CourseId);
            History = string.Concat(R.Events.Select(x => "<div class=\"list-item\"><strong>" + Ui.H(x.Action) + "</strong><p>" + Ui.H(x.Comment) + "</p><small>" + Ui.Stamp(x.CreatedAt) + "</small></div>"));
            var pending = R.Status == "Pending review";
            CanStart = pending && !R.Events.Any(x => x.Action == "Review started");
            CanDecide = pending;
        }

    }
}
