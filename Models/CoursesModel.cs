using System;
using System.Collections.Generic;
using System.Linq;
using SkillUp.Services;

namespace SkillUp.Models
{
    /// <summary>The courses an instructor teaches or a program leader manages (?q=&amp;status=).</summary>
    public sealed class CoursesModel : PageModel
    {
        public CoursesModel(SkillContext ctx) : base(ctx) { }
        public List<CourseRow> Results;
        public bool NoCourses;
        public string Term = "", StatusValue = "All statuses";


        public List<CourseRow> Mine;

        public void Load()
        {
            Mine = OwnCourses;
            var term = (Request.QueryString["q"] ?? "").Trim();
            var status = Request.QueryString["status"] ?? "All statuses";
            Term = term;
            StatusValue = status;
            var shown = Mine.Where(c => (status == "All statuses" || c.Status == status)
                && SearchMatch.Matches(term, c.Title, c.Description, true)).OrderBy(c => SearchMatch.Rank(term, c.Title)).ToList();
            Results = shown;
            NoCourses = shown.Count == 0;
        }

    }
}
