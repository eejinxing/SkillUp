using System;
using System.Linq;

namespace SkillUp.Models
{
    /// <summary>Scheduled live sessions for the viewer's courses. Instructors schedule them; everyone joins or leaves.</summary>
    public sealed class LiveSessionsModel : PageModel
    {
        public LiveSessionsModel(SkillContext ctx) : base(ctx) { }
        public bool ShowSchedule, NoSessions;
        public IEnumerable<object> SessionCourses;
        public IEnumerable<dynamic> Sessions;


        public void Load()
        {
            var courses = AccessibleCourses.ToDictionary(c => c.Id);
            ShowSchedule = Me.Role == "Instructor";
            if (ShowSchedule) SessionCourses = CourseOptions(courses.Values);
            var sessions = Data.Channels.Where(s => courses.ContainsKey(s.CourseId)).ToList();
            Sessions = sessions.Select(s => new
            {
                s.Id, Title = s.Name, CourseTag = Ui.Tag(courses[s.CourseId].Title),
                When = s.StartsAt == null ? "Time to be confirmed" : Ui.Stamp(s.StartsAt.Value),
                Host = Data.User(s.CreatedBy)?.Name ?? "Former user",
                Attendees = s.Attendees.Count,
                Joined = s.Attendees.Contains(Me.Id)
            }).ToList();
            NoSessions = sessions.Count == 0;
        }

    }
}
