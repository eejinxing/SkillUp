using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using Newtonsoft.Json;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend
{
    /// <summary>
    /// Shared frame of every page: the capsule navigation for the signed-in role (or visitor links), the page
    /// wrapper the stylesheets expect for each layout, the site footer, and one-line notices after a save.
    /// </summary>
    public partial class SkillUpMaster : MasterPage
    {
        sealed class NavLink
        {
            public string Url { get; set; }
            public string Label { get; set; }
            public string Route { get; set; }
            public bool Current { get; set; }
        }

        /// <summary>Navigation per role: route, label and page. The bar shows the main destinations only.</summary>
        static readonly Dictionary<string, string[][]> Menus = new Dictionary<string, string[][]>
        {
            ["Learner"] = new[] { L("dashboard", "Overview", "Dashboard"), L("learning", "My courses", "MyCourses"), L("catalog", "Explore courses", "Catalog"), L("leaderboard", "Leaderboard", "Leaderboard"), L("messages", "Messages", "Messages") },
            ["Instructor"] = new[] { L("dashboard", "Overview", "Dashboard"), L("courses", "My courses", "Courses"), L("requests", "My requests", "CourseRequests"), L("learners", "Learners", "Learners"), L("resources", "Learning resources", "Resources"), L("analytics", "Course analytics", "Analytics"), L("messages", "Messages", "Messages") },
            ["Program Leader"] = new[] { L("dashboard", "Overview", "Dashboard"), L("programs", "Programs", "Programs"), L("assignments", "Instructors", "Assignments"), L("approvals", "Course approvals", "Approvals"), L("messages", "Messages", "Messages") },
            ["Admin"] = new[] { L("dashboard", "Overview", "Dashboard"), L("users", "User accounts", "Users") },
            ["Visitor"] = new[] { L("home", "Home", "Home"), L("catalog", "Courses", "Catalog"), L("instructors", "Instructors", "Instructors"), L("enquiries", "Enquiries", "EnquiryPortal"), L("help", "Help", SkillPage.HomeContact) }
        };

        static string[] L(string route, string label, string url) => new[] { route, label, url };

        /// <summary>The current page, typed so the markup can read the signed-in account.</summary>
        public new SkillPage Page => (SkillPage)base.Page;

        /// <summary>A message shown in the toast on this response, such as a validation error.</summary>
        public string Notice { get; set; }

        /// <summary>
        /// What js/call-preview.js shows in the device-only call preview: the signed-in account, a demo caller for the
        /// "Test video call" buttons, and a course for the outline. JSON, escaped so it is safe inside a script element.
        /// </summary>
        protected string CallContext = "{}";

        protected void Page_Load(object sender, EventArgs e)
        {
            var me = Page.Me;
            var links = Menus[me == null ? "Visitor" : me.Role];
            NavLinks.DataSource = links.Select(l => new NavLink { Route = l[0], Label = l[1], Url = l[2], Current = l[0] == Page.Route });
            NavLinks.DataBind();
            GuestActions.Visible = me == null;
            UserActions.Visible = CallScript.Visible = me != null;
            if (me != null) CallContext = BuildCallContext(me);
        }

        string BuildCallContext(UserRow me)
        {
            var data = Page.Data;
            var peer = data.Users.FirstOrDefault(u => u.Id != me.Id && u.Role == "Instructor" && u.Status == "Active") ?? data.Users.FirstOrDefault(u => u.Id != me.Id && u.Status == "Active");
            var enrolled = Page.MyEnrollments.Select(x => data.Course(x.CourseId)).FirstOrDefault(c => c != null);
            var course = enrolled ?? data.Courses.FirstOrDefault(c => c.InstructorId == me.Id);
            return JsonConvert.SerializeObject(new
            {
                me = new { name = me.Name, avatar = me.Avatar },
                peer = peer == null ? null : new { name = peer.Name, role = peer.Role },
                course = course == null ? null : new
                {
                    title = course.Title, description = course.Description ?? "", outcomes = course.Outcomes ?? "",
                    lessons = data.LessonsOf(course.Id).Select(l => l.Title), progress = enrolled == null ? 0 : Page.Progress(me.Id, course.Id)
                }
            }, new JsonSerializerSettings { StringEscapeHandling = StringEscapeHandling.EscapeHtml });
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            var page = Page;
            var layout = page.Layout;
            ShellOpen.Text = layout == PageLayout.Workspace ? "<div class=\"shell learner-home-shell figma-shell\"><div>" : layout == PageLayout.Home ? "<div class=\"site-home home-refined\">" : "";
            ShellClose.Text = layout == PageLayout.Workspace ? "</div></div>" : layout == PageLayout.Home ? "</div>" : "";
            SiteFooter.Visible = layout == PageLayout.Public;

            if (layout == PageLayout.Workspace)
            {
                main.Attributes["class"] = "main" + (page.Secondary ? " figma-secondary" : "");
                main.Attributes["data-view"] = page.Route;
            }
            else if (layout == PageLayout.Public) main.Attributes["class"] = "public-main";

            if (page.Restricted)
            {
                PageContent.Visible = false;
                RestrictedPanel.Visible = true;
            }

            var classes = new List<string>();
            if (page.Me?.Theme == "light") classes.Add("light");
            if (page.Me != null) classes.Add("role-" + page.Me.Role.ToLowerInvariant().Replace(' ', '-'));
            classes.Add("ui-refined");
            if (page.Me == null) classes.Add("ui-public");
            if (!string.IsNullOrEmpty(page.BodyClass)) classes.Add(page.BodyClass);
            classes.Add("shared-capsule-nav");
            SkillBody.Attributes["class"] = string.Join(" ", classes);

            var flash = Session["SkillUp.Flash"] as string;
            if (flash != null)
            {
                Session.Remove("SkillUp.Flash");
                if (Notice == null) Notice = flash;
            }
            if (!string.IsNullOrEmpty(page.Title) && !page.Title.StartsWith("Skill UP")) page.Title = "Skill UP · " + page.Title;
        }
    }
}
