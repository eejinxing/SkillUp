using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend
{
    /// <summary>
    /// The signed-in account's profile: overview, achievements, a year of activity, courses, the editable information
    /// (Commands.Profile), profile picture, feedback and, in development, switching to another demo role.
    /// </summary>
    public partial class ProfilePage : SkillPage
    {
        public override string Route => "profile";
        protected override string[] Roles => new string[0];
        public override bool Secondary => true;

        static readonly Dictionary<string, string> Icons = new Dictionary<string, string>
        {
            ["overview"] = "<path d=\"M4 5h16v14H4z\"/><path d=\"M4 9h16\"/>", ["activity"] = "<path d=\"M3 12h4l3-8 4 16 3-8h4\"/>",
            ["info"] = "<circle cx=\"12\" cy=\"8\" r=\"4\"/><path d=\"M4 21c1-4 4-6 8-6s7 2 8 6\"/>", ["account"] = "<rect x=\"4\" y=\"10\" width=\"16\" height=\"11\" rx=\"2\"/><path d=\"M8 10V7a4 4 0 0 1 8 0v3\"/>",
            ["mail"] = "<rect x=\"3\" y=\"5\" width=\"18\" height=\"14\" rx=\"2\"/><path d=\"m3 7 9 6 9-6\"/>", ["pin"] = "<path d=\"M12 21s-7-6.2-7-11.5a7 7 0 0 1 14 0C19 14.8 12 21 12 21Z\"/><circle cx=\"12\" cy=\"9.5\" r=\"2.5\"/>",
            ["badge"] = "<path d=\"M12 3 4.5 6v6c0 4.5 3.2 7.8 7.5 9 4.3-1.2 7.5-4.5 7.5-9V6Z\"/>", ["user"] = "<circle cx=\"12\" cy=\"8\" r=\"4\"/><path d=\"M4 21c1-4 4-6 8-6s7 2 8 6\"/>",
            ["at"] = "<circle cx=\"12\" cy=\"12\" r=\"4\"/><path d=\"M16 12v1.5a2.5 2.5 0 0 0 5 0V12a9 9 0 1 0-3.5 7.1\"/>", ["text"] = "<path d=\"M4 6h16M4 12h16M4 18h10\"/>",
            ["moon"] = "<path d=\"M20 14.5A8 8 0 1 1 9.5 4a6.5 6.5 0 0 0 10.5 10.5Z\"/>", ["pencil"] = "<path d=\"M16.5 3.5a2.1 2.1 0 0 1 3 3L8 18l-4 1 1-4Z\"/>",
            ["key"] = "<circle cx=\"8\" cy=\"15\" r=\"4\"/><path d=\"m11 12 9-9M17 6l3 3\"/>", ["chat"] = "<path d=\"M21 12a8.5 8.5 0 0 1-12.4 7.6L3 21l1.4-5.4A8.5 8.5 0 1 1 21 12Z\"/>",
            ["chevron"] = "<path d=\"m6 9 6 6 6-6\"/>", ["book"] = "<path d=\"M2.5 5.5C5 4.5 8.5 4.5 12 6.5c3.5-2 7-2 9.5-1v13c-2.5-1-6-1-9.5 1-3.5-2-7-2-9.5-1Z\"/><path d=\"M12 6.5v13\"/>",
            ["star"] = "<path d=\"m12 3 2.7 5.5 6.1.9-4.4 4.3 1 6.1-5.4-2.9L6.6 20l1-6.1-4.4-4.3 6.1-.9Z\"/>",
            ["users"] = "<circle cx=\"9\" cy=\"8\" r=\"3.5\"/><path d=\"M2.5 20c.8-3.5 3.3-5.5 6.5-5.5s5.7 2 6.5 5.5M16 4.6a3.5 3.5 0 0 1 0 6.8M18 14.8c1.8.7 3 2.5 3.5 5.2\"/>"
        };
        /// <summary>Sticker artwork per achievement icon.</summary>
        static readonly Dictionary<string, string> Stickers = new Dictionary<string, string>
        {
            ["compass"] = "fox", ["check"] = "frogs", ["star"] = "cloud", ["trophy"] = "jellyfish", ["fire"] = "shoe", ["layers"] = "moon", ["bolt"] = "bolt",
            ["message"] = "plane", ["book"] = "fox", ["users"] = "frogs", ["sparkle"] = "moon", ["flag"] = "jellyfish", ["shield"] = "plane"
        };
        static readonly Dictionary<string, string> AvatarTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".webp"] = "image/webp", [".gif"] = "image/gif"
        };

        protected int HeatTotal, HeatWeeks, CourseCount, Certificates, Earned, BadgeCount;
        protected string HeatCells, HeatMonths, RoleLine;

        protected string Icon(string name, string extraClass = "") =>
            "<svg class=\"pf-ic " + extraClass + "\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">" + Icons[name] + "</svg>";

        protected void Page_Load(object sender, EventArgs e)
        {
            Title = Me.Name;
            DemoSwitch.Visible = Backend.Settings.DemoLogin;
            var enrolments = MyEnrollments;
            Certificates = enrolments.Count(x => Data.LessonsOf(x.CourseId).Count > 0 && Progress(Me.Id, x.CourseId) == 100);
            CourseCount = Me.Role == "Learner" ? enrolments.Count : Data.Courses.Count(c => c.InstructorId == Me.Id);
            var titles = enrolments.Select(x => Data.Course(x.CourseId)?.Title).Where(t => t != null).Take(2).ToList();
            RoleLine = Me.Role == "Learner" ? (titles.Count > 0 ? "I'm learning " + string.Join(" and ", titles) + " on Skill UP." : "I am just getting started on Skill UP.")
                : Me.Role == "Instructor" ? "I teach practical, hands-on courses on Skill UP."
                : Me.Role == "Program Leader" ? "I lead learning programs and review new courses on Skill UP."
                : "I look after the Skill UP workspace and its accounts.";

            var days = ActivityDays();
            BuildHeatmap(days);
            BindBadges(days);
            BindCourses();
            BindCertificates();
            if (!IsPostBack)
            {
                NameBox.Text = Me.Name;
                UsernameBox.Text = Me.Username;
                EmailBox.Text = Me.Email;
                LocationBox.Text = Me.Location;
                BioBox.Text = Me.Bio;
            }
        }

        /// <summary>
        /// Activity per day: quiz and lab results for a learner; for staff, their messages, announcements, course request
        /// decisions and (for Admin) audit entries.
        /// </summary>
        Dictionary<DateTime, int> ActivityDays()
        {
            var days = new Dictionary<DateTime, int>();
            Action<DateTime> add = t => days[t.Date] = (days.ContainsKey(t.Date) ? days[t.Date] : 0) + 1;
            if (Me.Role == "Learner")
            {
                foreach (var a in Insights.Attempts(this).Where(a => a.UserId == Me.Id)) add(a.At);
                return days;
            }
            if (Me.Role == "Admin") foreach (var a in Data.Audit.Where(a => a.UserId == Me.Id)) add(a.CreatedAt);
            foreach (var m in Data.Messages.Where(m => m.SenderId == Me.Id)) add(m.SentAt);
            foreach (var a in Data.Announcements.Where(a => a.PostedBy == Me.Id)) add(a.CreatedAt);
            foreach (var ev in Data.Requests.SelectMany(r => r.Events).Where(ev => ev.ActorId == Me.Id)) add(ev.CreatedAt);
            return days;
        }

        /// <summary>A year of activity as week columns, Sunday to Saturday, darker for busier days.</summary>
        void BuildHeatmap(Dictionary<DateTime, int> days)
        {
            var end = DateTime.Today;
            var start = end.AddDays(-364 - (int)end.DayOfWeek);
            var max = Math.Max(1, days.Values.DefaultIfEmpty(0).Max());
            var cells = new StringBuilder();
            var months = new StringBuilder();
            var i = 0;
            for (var d = start; d <= end; d = d.AddDays(1), i++)
            {
                int v;
                days.TryGetValue(d, out v);
                HeatTotal += v;
                var level = v == 0 ? 0 : Math.Min(4, (int)Math.Ceiling(v * 4.0 / max));
                if (d.Day == 1) months.Append("<span style=\"--c:" + (i / 7) + "\">" + d.ToString("MMM", System.Globalization.CultureInfo.GetCultureInfo("en-GB")) + "</span>");
                cells.Append("<i data-l=\"" + level + "\" style=\"--c:" + (i / 7) + "\" title=\"" + d.ToString("yyyy-MM-dd") + ": " + v + " " + (v == 1 ? "activity" : "activities") + "\"></i>");
            }
            HeatCells = cells.ToString();
            HeatMonths = months.ToString();
            HeatWeeks = (int)Math.Ceiling(i / 7.0);
        }

        void BindBadges(Dictionary<DateTime, int> days)
        {
            var enrolments = MyEnrollments;
            var done = enrolments.Count(x => Data.LessonsOf(x.CourseId).Count > 0 && Progress(Me.Id, x.CourseId) == 100);
            var best = Insights.Attempts(this).Where(a => a.UserId == Me.Id && a.Type == "quiz").Select(a => a.Score).DefaultIfEmpty(0).Max();
            var sorted = days.Keys.OrderBy(d => d).ToList();
            int run = 0, streak = 0;
            for (var k = 0; k < sorted.Count; k++)
            {
                run = k > 0 && (sorted[k] - sorted[k - 1]).TotalDays == 1 ? run + 1 : 1;
                streak = Math.Max(streak, run);
            }
            var sent = Data.Messages.Count(m => m.SenderId == Me.Id);
            var taught = Data.Courses.Where(c => c.InstructorId == Me.Id).ToList();
            var programs = Data.Programs.Where(p => p.LeaderId == Me.Id).Select(p => p.Id).ToList();
            var reviews = Data.Requests.Count(r => r.Events.Any(ev => ev.ActorId == Me.Id && ev.Action != "Submitted"));
            Func<string, string, string, string, bool, object> b = (title, detail, icon, colour, earned) => new { Title = title, Detail = detail, Sticker = Stickers[icon], Colour = colour, Earned = earned };
            List<object> list;
            switch (Me.Role)
            {
                case "Instructor":
                    list = new List<object> { b("Course author", "Created a course", "book", "#8b5cf6", taught.Count > 0), b("Published", "A course is live", "check", "#22c55e", taught.Any(c => c.Status == "Published")),
                        b("Mentor", "Learners enrolled", "users", "#2dd4bf", Data.Enrollments.Any(x => taught.Any(c => c.Id == x.CourseId))), b("Announcer", "Posted an update", "sparkle", "#f5c542", Data.Announcements.Any(a => a.PostedBy == Me.Id)),
                        b("Conversation", "Sent a message", "message", "#ec4899", sent > 0), b("Prolific", "Three or more courses", "layers", "#3b82f6", taught.Count >= 3) };
                    break;
                case "Program Leader":
                    list = new List<object> { b("Program lead", "Leads a program", "flag", "#8b5cf6", programs.Count > 0), b("Reviewer", "Reviewed a request", "shield", "#22c55e", reviews > 0),
                        b("Curator", "Five or more courses", "layers", "#3b82f6", Data.Courses.Count(c => programs.Contains(c.ProgramId)) >= 5), b("Conversation", "Sent a message", "message", "#ec4899", sent > 0),
                        b("Active", "Recorded activity", "bolt", "#f5c542", days.Count > 0) };
                    break;
                case "Admin":
                    list = new List<object> { b("Administrator", "Full workspace access", "shield", "#ef4444", true), b("Gatekeeper", "Reviewed an account", "check", "#22c55e", Data.Audit.Any(a => a.UserId == Me.Id)),
                        b("Conversation", "Sent a message", "message", "#ec4899", sent > 0), b("Active", "Recorded activity", "bolt", "#f5c542", days.Count > 0) };
                    break;
                default:
                    list = new List<object> { b("First steps", "Enrolled in a course", "compass", "#2dd4bf", enrolments.Count > 0), b("Getting going", "Completed an activity", "check", "#22c55e", enrolments.Any(x => Data.CompletedLessons(Me.Id, x.CourseId).Count > 0)),
                        b("Quiz ace", "Scored 90% or more", "star", "#f5c542", best >= 90), b("Finisher", "Earned a certificate", "trophy", "#f97316", done > 0),
                        b("On a roll", "7-day learning streak", "fire", "#ef4444", streak >= 7), b("Explorer", "Two or more courses", "layers", "#8b5cf6", enrolments.Count >= 2),
                        b("Centurion", "100 learning sessions", "bolt", "#3b82f6", days.Count >= 100), b("Conversation", "Sent a message", "message", "#ec4899", sent > 0) };
                    break;
            }
            BadgeCount = list.Count;
            Earned = list.Count(x => (bool)x.GetType().GetProperty("Earned").GetValue(x));
            Badges.DataSource = list;
            Badges.DataBind();
        }

        /// <summary>The learner's earned certificates (every activity done), newest first. Learners only.</summary>
        void BindCertificates()
        {
            CertSection.Visible = Me.Role == "Learner";
            if (!CertSection.Visible) return;
            var enGb = System.Globalization.CultureInfo.GetCultureInfo("en-GB");
            var earned = MyEnrollments.Where(x => Data.LessonsOf(x.CourseId).Count > 0 && Progress(Me.Id, x.CourseId) == 100)
                .Select(x => new { Course = Data.Course(x.CourseId), When = x.CompletedAt ?? x.LastActive ?? x.EnrolledAt })
                .Where(x => x.Course != null).OrderByDescending(x => x.When).ToList();
            CertCards.DataSource = earned.Select(x =>
            {
                var hours = Math.Max(1, (int)Math.Ceiling(Data.LessonsOf(x.Course.Id).Sum(l => l.Minutes) / 60.0));
                return new { x.Course.Id, x.Course.Title, Date = x.When.ToString("dd MMMM yyyy", enGb), Iso = x.When.ToString("s"), Hours = hours + (hours == 1 ? " Hour" : " Hours") };
            }).ToList();
            CertCards.DataBind();
            NoCerts.Visible = earned.Count == 0;
        }

        void BindCourses()
        {
            IEnumerable<Tuple<CourseRow, string>> list;
            if (Me.Role == "Learner") list = MyEnrollments.Select(x => Data.Course(x.CourseId)).Where(c => c != null).Select(c => Tuple.Create(c, Progress(Me.Id, c.Id) == 100 ? "Completed" : Progress(Me.Id, c.Id) + "% done"));
            else if (Me.Role == "Admin") list = Data.Courses.Where(c => c.Status == "Published").Select(c => Tuple.Create(c, "Published"));
            else list = OwnCourses.Select(c => Tuple.Create(c, c.Status));
            var cards = list.Take(4).Select(t =>
            {
                var reviews = Data.Feedback.Where(f => f.CourseId == t.Item1.Id).ToList();
                return new
                {
                    t.Item1.Id, t.Item1.Title, Description = t.Item1.Description ?? "", Tag = t.Item2, TagKey = new string(t.Item2.ToLowerInvariant().Where(char.IsLetter).ToArray()),
                    Rating = reviews.Count == 0 ? "New" : reviews.Average(f => f.Rating).ToString("0.0"),
                    Learners = Data.Enrollments.Count(x => x.CourseId == t.Item1.Id), Activities = Data.LessonsOf(t.Item1.Id).Count
                };
            }).ToList();
            CourseCards.DataSource = cards;
            CourseCards.DataBind();
            NoCourses.Visible = cards.Count == 0;
        }

        protected void SaveProfile_Click(object sender, EventArgs e)
        {
            // Without an avatar field the command keeps the current picture.
            var payload = new { name = NameBox.Text.Trim(), username = UsernameBox.Text.Trim(), email = EmailBox.Text.Trim(), location = LocationBox.Text.Trim(), bio = BioBox.Text, theme = Me.Theme, language = Me.Language };
            if (IsValid && TryRun("profile", payload)) ReloadPage("Profile saved");
        }

        protected void Avatar_Click(object sender, EventArgs e)
        {
            string type;
            if (!AvatarFile.HasFile || !AvatarTypes.TryGetValue(Path.GetExtension(AvatarFile.FileName), out type))
            {
                ShowError("Choose a PNG, JPG, WebP or GIF image.");
                return;
            }
            if (AvatarFile.FileBytes.Length > 2 * 1024 * 1024)
            {
                ShowError("Profile images must be smaller than 2 MB.");
                return;
            }
            var me = Me;
            var payload = new { name = me.Name, username = me.Username, email = me.Email, location = me.Location, bio = me.Bio, theme = me.Theme, language = me.Language, avatar = "data:" + type + ";base64," + Convert.ToBase64String(AvatarFile.FileBytes) };
            if (TryRun("profile", payload)) ReloadPage("Profile image updated");
        }

        protected void RemoveAvatar_Click(object sender, EventArgs e)
        {
            var me = Me;
            if (TryRun("profile", new { name = me.Name, username = me.Username, email = me.Email, location = me.Location, bio = me.Bio, theme = me.Theme, language = me.Language, avatar = "" })) ReloadPage("Profile image removed");
        }

        protected void Feedback_Click(object sender, EventArgs e)
        {
            if (IsValid && TryRun("feedback", new { rating = FeedbackRating.SelectedValue, text = FeedbackText.Text.Trim() })) ReloadPage("Thanks for your feedback");
        }

        protected void Demo_Command(object sender, CommandEventArgs e)
        {
            if (TryRun("demo", new { role = (string)e.CommandArgument })) Go("Dashboard", "Opened " + e.CommandArgument + " workspace");
        }

        protected void SignOut_Click(object sender, EventArgs e)
        {
            if (TryRun("logout")) Go("Home", "You have signed out.");
        }
    }
}
