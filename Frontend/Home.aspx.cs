using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace SKILL_UP.Frontend
{
    public partial class HomePage : SkillPage
    {
        public override string Route => "home";
        public override PageLayout Layout => PageLayout.Home;
        /// <summary>
        /// smooth-scroll: links to a section of this page (visitor Help, the footer's social icons) glide to it. Left off
        /// after a postback, so the reply to the enquiry form opens at the form instead of scrolling down from the top.
        /// </summary>
        public override string BodyClass => IsPostBack ? "ui-home" : "ui-home smooth-scroll";

        /// <summary>Words that tie a topic card to a course title or description.</summary>
        static readonly Dictionary<string, Regex> Topics = new Dictionary<string, Regex>
        {
            ["computer"] = new Regex("computer|hardware|basics", RegexOptions.IgnoreCase),
            ["programming"] = new Regex("c#|programming|code|javascript|python", RegexOptions.IgnoreCase),
            ["database"] = new Regex("mongo|database|sql", RegexOptions.IgnoreCase),
            ["web"] = new Regex(@"web|asp\.net|api|html", RegexOptions.IgnoreCase),
            ["networks"] = new Regex("network", RegexOptions.IgnoreCase),
            ["security"] = new Regex("security|cyber", RegexOptions.IgnoreCase),
            ["data"] = new Regex("data science|dataset|analysis|visuali", RegexOptions.IgnoreCase),
            ["ai"] = new Regex(@"\bai\b|machine learning|automation", RegexOptions.IgnoreCase)
        };

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        /// <summary>A topic card opens the first published course that matches it, or the catalogue when none does.</summary>
        protected string TopicUrl(string topic)
        {
            var course = Data.Courses.FirstOrDefault(c => c.Status == "Published" && Topics[topic].IsMatch(c.Title + " " + c.Description));
            return course == null ? "Catalog" : "Course?id=" + course.Id;
        }

        protected void SignUp_Click(object sender, EventArgs e) =>
            Go("Register" + (SignUpEmail.Text.Trim().Length > 0 ? "?email=" + HttpUtility.UrlEncode(SignUpEmail.Text.Trim()) : ""));
    }
}
