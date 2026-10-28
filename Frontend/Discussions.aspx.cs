using System;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SKILL_UP.Frontend
{
    /// <summary>
    /// Course discussions for the courses the viewer can open. Learners see visible posts; staff who manage a course
    /// also see hidden or flagged posts and can moderate them.
    /// </summary>
    public partial class DiscussionsPage : SkillPage
    {
        public override string Route => "discussions";
        protected override string[] Roles => new string[0];
        public override bool Secondary => true;

        protected bool NewOpen, CanModerate;

        protected void Page_Load(object sender, EventArgs e)
        {
            var courses = AccessibleCourses.ToDictionary(c => c.Id);
            CanModerate = Me.Role != "Learner";
            if (!IsPostBack)
            {
                PostCourse.DataSource = CourseOptions(courses.Values);
                PostCourse.DataBind();
            }
            var posts = Data.Posts.Where(p => p.ParentId == null && p.CourseId != null && courses.ContainsKey(p.CourseId.Value)
                    && (Me.Role == "Learner" ? p.Status == "Visible" : (Managed.Contains(p.CourseId.Value) || p.Status == "Visible" || p.UserId == Me.Id)))
                .OrderByDescending(p => p.CreatedAt).ToList();
            Posts.DataSource = posts.Select(p =>
            {
                var author = Data.User(p.UserId);
                return new
                {
                    p.Id, p.Status, Text = p.Content, p.Reason,
                    AvatarHtml = Ui.Avatar(author), Author = author?.Name ?? "Former user", Course = courses[p.CourseId.Value].Title, Date = Ui.Date(p.CreatedAt), StatusTag = Ui.Tag(p.Status),
                    Replies = Data.Posts.Where(r => r.ParentId == p.Id).OrderBy(r => r.CreatedAt).Select(r =>
                    {
                        var who = Data.User(r.UserId);
                        return new { Author = who?.Name ?? "Former user", RoleTag = Ui.Tag(who?.Role ?? "Member"), Text = r.Content, Date = Ui.Date(r.CreatedAt) };
                    }).ToList()
                };
            });
            Posts.DataBind();
            NoPosts.Visible = posts.Count == 0;
        }

        protected void Post_Click(object sender, EventArgs e)
        {
            NewOpen = true;
            if (IsValid && TryRun("post", new { courseId = PostCourse.SelectedValue, text = PostText.Text.Trim() })) ReloadPage("Discussion posted");
        }

        protected void Reply_Command(object sender, CommandEventArgs e)
        {
            var text = ((TextBox)((Control)sender).NamingContainer.FindControl("ReplyText")).Text.Trim();
            if (TryRun("reply", new { id = (string)e.CommandArgument, kind = "post", text })) ReloadPage("Reply posted");
        }

        protected void Moderate_Command(object sender, CommandEventArgs e)
        {
            var item = ((Control)sender).NamingContainer;
            var status = ((DropDownList)item.FindControl("ModerateStatus")).SelectedValue;
            var reason = ((TextBox)item.FindControl("ModerateReason")).Text.Trim();
            if (TryRun("moderate", new { id = (string)e.CommandArgument, status, reason })) ReloadPage("Decision saved");
        }
    }
}
