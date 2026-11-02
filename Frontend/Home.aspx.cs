using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using SKILL_UP.Backend;

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

        /// <summary>The "Learn from the people behind your next skill" cards and the "Our Learners" feedback, both from the database.</summary>
        protected string InstructorCards = "", StoryGrid = "", StoryNote = "";
        protected int InstructorCount;

        const string Arrow = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M7 17 17 7M9 7h8v8\"></path></svg>";

        protected void Page_Load(object sender, EventArgs e)
        {
            BuildInstructors();
            BuildStories();
        }

        /// <summary>
        /// Active instructors with at least one published course (what a visitor can open), most courses first. Each card shows
        /// their profile picture (else their initials), the programs they teach in, their bio and the courses they teach.
        /// </summary>
        void BuildInstructors()
        {
            var published = Data.Courses.Where(c => c.Status == "Published").ToList();
            var team = Data.Users.Where(u => u.Role == "Instructor" && u.Status == "Active")
                .Select(u => new { U = u, Courses = published.Where(c => c.InstructorId == u.Id).OrderBy(c => c.Title).ToList() })
                .Where(x => x.Courses.Count > 0)
                .OrderByDescending(x => x.Courses.Count).ThenBy(x => x.U.Name).ToList();
            InstructorCount = team.Count;
            var html = new StringBuilder();
            foreach (var x in team)
            {
                var name = Ui.H(x.U.Name);
                var subject = Ui.H(string.Join(" \u00B7 ", x.Courses.Select(c => Data.Program(c.ProgramId)?.Name).Where(n => !string.IsNullOrEmpty(n)).Distinct().Take(2)));
                if (subject.Length == 0) subject = "Skill UP instructor";
                var shown = x.Courses.Take(3).Select(c => c.Title).ToList();
                var teaches = "Teaches " + string.Join(", ", shown) + (x.Courses.Count > shown.Count ? " and " + (x.Courses.Count - shown.Count) + " more" : "") + ".";
                var bio = string.IsNullOrWhiteSpace(x.U.Bio) ? "Practical lessons, one step at a time." : x.U.Bio.Trim();
                html.Append("<button type=\"button\" class=\"instructor-card\" aria-expanded=\"false\" aria-label=\"About ").Append(name).Append("\">")
                    .Append("<span class=\"instructor-photo\" aria-hidden=\"true\">").Append(Portrait(x.U)).Append("</span>")
                    .Append("<span class=\"instructor-identity\"><strong>").Append(name).Append("</strong><small>").Append(subject).Append("</small></span>")
                    .Append("<span class=\"instructor-bio\"><strong>").Append(name).Append("</strong><small>").Append(subject).Append("</small>")
                    .Append("<span>").Append(Ui.H(bio)).Append("</span><span class=\"instructor-teaches\">").Append(Ui.H(teaches)).Append("</span>")
                    .Append("<i aria-hidden=\"true\">").Append(Arrow).Append("</i></span></button>");
            }
            InstructorCards = html.ToString();
        }

        static string Portrait(UserRow u) => string.IsNullOrEmpty(u.Avatar)
            ? "<span class=\"instructor-initials\">" + Ui.H(Ui.Initials(u)) + "</span>"
            : "<img src=\"" + Ui.H(u.Avatar) + "\" alt=\"\" loading=\"lazy\">";

        /// <summary>
        /// Public feedback from active accounts, about the platform or a published course. The highest rated (newest first) is
        /// featured in the middle; the next four newest sit either side. The note above shows the average rating of them all.
        /// </summary>
        void BuildStories()
        {
            var items = Data.Feedback.Where(f => f.Public && !string.IsNullOrWhiteSpace(f.Comment))
                .Select(f => new { F = f, U = Data.User(f.UserId), C = f.CourseId == null ? null : Data.Course(f.CourseId.Value) })
                .Where(x => x.U != null && x.U.Status == "Active" && (x.F.CourseId == null || x.C != null && x.C.Status == "Published"))
                .OrderByDescending(x => x.F.CreatedAt).ThenByDescending(x => x.F.Id).ToList();
            if (items.Count == 0)
            {
                StoryNote = "No feedback yet";
                StoryGrid = "<div class=\"home-story-empty home-reveal\"><p>No learner feedback has been shared yet.</p><span>Learners can rate Skill UP from their profile, and rate each course they take.</span></div>";
                return;
            }
            var average = items.Average(x => x.F.Rating);
            StoryNote = average.ToString("0.0", CultureInfo.InvariantCulture) + " average &middot; " + Ui.Plural(items.Count, "review");
            var featured = items.OrderByDescending(x => x.F.Rating).ThenByDescending(x => x.F.CreatedAt).First();
            var rest = items.Where(x => x != featured).Take(4).ToList();
            Func<UserRow, CourseRow, DateTime, string> about = (u, c, at) =>
                Ui.H(u.Name) + "<span>" + (c == null ? "About Skill UP" : "On " + Ui.H(c.Title)) + " &middot; " + Ui.Date(at) + "</span>";
            Func<int, string> card = i =>
                "<article class=\"home-story home-reveal\"><div class=\"home-story-top\"><span class=\"home-story-mark\" aria-hidden=\"true\">" + Ui.H(Ui.Initials(rest[i].U)) + "</span>"
                + Stars(rest[i].F.Rating, "home-stars home-stars-sm") + "</div><blockquote>" + Ui.H(rest[i].F.Comment) + "</blockquote><footer>" + about(rest[i].U, rest[i].C, rest[i].F.CreatedAt) + "</footer></article>";
            var html = new StringBuilder("<svg width=\"0\" height=\"0\" style=\"position:absolute\" aria-hidden=\"true\"><defs><path id=\"story-star\" d=\"m10 1 2.7 5.6 6.2.9-4.5 4.4 1.1 6.2L10 15.2l-5.5 2.9 1.1-6.2-4.5-4.4 6.2-.9Z\"></path></defs></svg>");
            html.Append("<div class=\"home-story-grid count-").Append(Math.Min(items.Count, 5)).Append("\">");
            if (rest.Count > 0) html.Append("<div>").Append(string.Concat(Enumerable.Range(0, Math.Min(2, rest.Count)).Select(card))).Append("</div>");
            var portrait = string.IsNullOrEmpty(featured.U.Avatar)
                ? "<span class=\"home-story-portrait home-story-initials\" aria-hidden=\"true\">" + Ui.H(Ui.Initials(featured.U)) + "</span>"
                : "<img class=\"home-story-portrait\" src=\"" + Ui.H(featured.U.Avatar) + "\" alt=\"\" width=\"68\" height=\"68\" loading=\"lazy\" style=\"object-fit:cover\">";
            html.Append("<article class=\"home-story home-story-featured home-reveal\">").Append(portrait).Append(Stars(featured.F.Rating, "home-stars"))
                .Append("<blockquote>").Append(Ui.H(featured.F.Comment)).Append("</blockquote><footer>").Append(about(featured.U, featured.C, featured.F.CreatedAt)).Append("</footer></article>");
            if (rest.Count > 2) html.Append("<div>").Append(string.Concat(Enumerable.Range(2, rest.Count - 2).Select(card))).Append("</div>");
            StoryGrid = html.Append("</div>").ToString();
        }

        /// <summary>Five stars, the first rating of them filled, drawn from the shared #story-star path.</summary>
        static string Stars(int rating, string cls)
        {
            rating = Math.Max(0, Math.Min(5, rating));
            var html = new StringBuilder("<svg class=\"" + cls + "\" viewBox=\"0 0 112 20\" role=\"img\" aria-label=\"" + rating + " out of 5 stars\">");
            for (var i = 0; i < 5; i++)
                html.Append(i < rating ? "<use href=\"#story-star\" x=\"" + i * 23 + "\" fill=\"#e8d07a\"></use>" : "<use href=\"#story-star\" x=\"" + i * 23 + "\" fill=\"none\" stroke=\"#e8d07a\"></use>");
            return html.Append("</svg>").ToString();
        }

        /// <summary>
        /// A topic card opens the first published course whose title matches it, or the catalogue when none does. Only the
        /// title counts: a description that merely mentions a word (a data course "with security in mind") is not that topic.
        /// </summary>
        protected string TopicUrl(string topic)
        {
            var course = Data.Courses.FirstOrDefault(c => c.Status == "Published" && Topics[topic].IsMatch(c.Title));
            return course == null ? "Catalog" : "Course?id=" + course.Id;
        }
    }
}
