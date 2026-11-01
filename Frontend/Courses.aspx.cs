using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend
{
    /// <summary>The courses an instructor teaches or a program leader manages (?q=&amp;status=).</summary>
    public partial class CoursesPage : SkillPage
    {
        public override string Route => "courses";
        protected override string[] Roles => new[] { "Instructor", "Program Leader" };
        public override bool Secondary => true;

        protected List<CourseRow> Mine;

        protected void Page_Load(object sender, EventArgs e)
        {
            Mine = OwnCourses;
            var term = (Request.QueryString["q"] ?? "").Trim();
            var status = Request.QueryString["status"] ?? "All statuses";
            if (!IsPostBack)
            {
                SearchBox.Text = term;
                if (StatusFilter.Items.FindByText(status) != null) StatusFilter.SelectedValue = status;
            }
            var shown = Mine.Where(c => (status == "All statuses" || c.Status == status)
                && SearchMatch.Matches(term, c.Title, c.Description, true)).OrderBy(c => SearchMatch.Rank(term, c.Title)).ToList();
            Results.DataSource = shown;
            Results.DataBind();
            NoCourses.Visible = shown.Count == 0;
        }

        protected void Filter_Click(object sender, EventArgs e) =>
            Go("Courses?q=" + HttpUtility.UrlEncode(SearchBox.Text.Trim()) + "&status=" + HttpUtility.UrlEncode(StatusFilter.SelectedValue));
    }
}
