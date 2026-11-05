using System;
using System.Linq;
using SkillUp.Services;

namespace SkillUp.Models
{
    /// <summary>Certificates for courses the learner has completed (every activity done); ?id= shows one to print.</summary>
    public sealed class CertificatesModel : PageModel
    {
        public CertificatesModel(SkillContext ctx) : base(ctx) { }
        public int ViewIndex;
        public string PageTitle, Term = "";
        public IEnumerable<dynamic> Earned;
        public bool ShowSearch, NoneYet, NoMatch;


        public CourseRow Shown;
        /// <summary>The completion date for the certificate: written here, and rewritten in the viewer's own date and time zone by js/site.js.</summary>
        public string CompletedOn, CompletedIso, CourseHours;
        /// <summary>
        /// Where the breadcrumb and Close lead. A certificate opened from its course (?from=course) goes back to that course;
        /// one opened from the list goes back to the list.
        /// </summary>
        public string BackUrl = "Certificates", BackLabel = "Certificates", HereLabel;

        public void Load()
        {
            var completed = AccessibleCourses.Where(c => Data.LessonsOf(c.Id).Count > 0 && Progress(Me.Id, c.Id) == 100).ToList();
            var id = QueryInt("id");
            if (id != null)
            {
                Shown = completed.FirstOrDefault(c => c.Id == id);
                ViewIndex = Shown == null ? 2 : 1;
                if (Shown != null)
                {
                    HereLabel = Shown.Title;
                    // The browser names a saved PDF after the page title, so the file comes out as "Skill UP Certificate 2-1.pdf".
                    PageTitle = "Skill UP Certificate " + Me.Id + "-" + Shown.Id;
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
            // Search (?q=): words that start with what was typed; one letter matches only titles that start with it.
            var term = (Request.QueryString["q"] ?? "").Trim();
            Term = term;
            var shown = completed.Where(c => SearchMatch.Matches(term, c.Title, c.Description, true)).OrderBy(c => SearchMatch.Rank(term, c.Title)).ToList();
            Earned = shown.Select(c => new { c.Id, c.Title, Lessons = Data.LessonsOf(c.Id).Count });
            ShowSearch = completed.Count > 0;
            NoneYet = completed.Count == 0;
            NoMatch = completed.Count > 0 && shown.Count == 0;
        }

    }
}
