using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend
{
    /// <summary>Published courses, filtered by level and a search over title, description and instructor (?level=&amp;q=).</summary>
    public partial class CatalogPage : SkillPage
    {
        public override string Route => "catalog";
        public override bool Secondary => true;

        static readonly string[] LevelNames = { "All levels", "Beginner", "Intermediate", "Advanced" };

        protected List<CourseRow> Published, Shown;
        protected int EnrolledCount, ActivityCount;

        string Level => LevelNames.Contains(Request.QueryString["level"]) ? Request.QueryString["level"] : "All levels";
        string Term => (Request.QueryString["q"] ?? "").Trim();

        protected void Page_Load(object sender, EventArgs e)
        {
            Published = Data.Courses.Where(c => c.Status == "Published").ToList();
            ActivityCount = Published.Sum(c => Data.LessonsOf(c.Id).Count);
            EnrolledCount = Published.Count(c => MyEnrollments.Any(x => x.CourseId == c.Id));
            EnrolledStat.Visible = Me != null;
            if (!IsPostBack) SearchBox.Text = Term;

            // Words that start with what was typed; titles that start with it come first.
            var term = Term;
            Shown = Published.Where(c => (Level == "All levels" || c.Difficulty == Level)
                && SearchMatch.Matches(term, c.Title + " " + Data.User(c.InstructorId)?.Name, c.Description, true))
                .OrderBy(c => SearchMatch.Rank(term, c.Title)).ToList();

            Levels.DataSource = LevelNames.Select(l => new
            {
                Level = l,
                Label = l == "All levels" ? "All" : l,
                Count = l == "All levels" ? Published.Count : Published.Count(c => c.Difficulty == l),
                Pressed = l == Level
            });
            Levels.DataBind();
            Results.DataSource = Shown;
            Results.DataBind();
            NoResults.Visible = Shown.Count == 0;
        }

        void Show(string level, string term) =>
            Go("Catalog" + (level == "All levels" && term.Length == 0 ? "" : "?level=" + HttpUtility.UrlEncode(level) + (term.Length > 0 ? "&q=" + HttpUtility.UrlEncode(term) : "")));

        protected void Search_Click(object sender, EventArgs e) => Show(Level, SearchBox.Text.Trim());

        protected void Level_Command(object sender, CommandEventArgs e) => Show((string)e.CommandArgument, SearchBox.Text.Trim());
    }
}
