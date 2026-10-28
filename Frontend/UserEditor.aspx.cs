using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using Newtonsoft.Json.Linq;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend
{
    /// <summary>
    /// Admin: create an account (no ?id, or ?id=new), or manage one (?id=): information and access, the activity log
    /// (?tab=audit) and learning activity (?tab=learning).
    /// </summary>
    public partial class UserEditorPage : SkillPage
    {
        static string Outline(string paths) =>
            "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">" + paths + "</svg>";

        static readonly Dictionary<string, string> Glyphs = new Dictionary<string, string>
        {
            ["name"] = Outline("<circle cx=\"12\" cy=\"8\" r=\"4\"/><path d=\"M4 21a8 8 0 0 1 16 0\"/>"),
            ["username"] = Outline("<circle cx=\"12\" cy=\"12\" r=\"4\"/><path d=\"M16 8v6a2 2 0 0 0 4 0v-2a8 8 0 1 0-3 6\"/>"),
            ["email"] = Outline("<rect x=\"3\" y=\"5\" width=\"18\" height=\"14\" rx=\"2\"/><path d=\"m3 7 9 6 9-6\"/>"),
            ["bio"] = Outline("<path d=\"M4 6h16M4 11h16M4 16h10\"/>"),
            ["role"] = Outline("<path d=\"m12 3 8 4v5c0 5-8 9-8 9s-8-4-8-9V7Z\"/><path d=\"m8 12 3 3 5-6\"/>"),
            ["status"] = Outline("<circle cx=\"12\" cy=\"12\" r=\"8\"/><path d=\"m8 12 3 3 5-6\"/>"),
            ["password"] = Outline("<circle cx=\"8\" cy=\"15\" r=\"5\"/><path d=\"m12 11 8-8M16 7l3 3M18 5l3 3\"/>"),
            ["id"] = Outline("<rect x=\"3\" y=\"5\" width=\"18\" height=\"14\" rx=\"2\"/><path d=\"M7 10h3M7 14h3M14 10h3M14 14h3\"/>"),
            ["edit"] = Outline("<path d=\"m15 5 4 4M4 20l4-1L20 7a2.8 2.8 0 0 0-4-4L4 15Z\"/>"),
            ["chevron"] = Outline("<path d=\"m8 14 4-4 4 4\"/>")
        };
        static readonly Dictionary<string, string> Past = new Dictionary<string, string>
        {
            ["Delete"] = "deleted", ["Create"] = "created", ["Update"] = "updated", ["Add"] = "added", ["Remove"] = "removed", ["Submit"] = "submitted", ["Save"] = "saved",
            ["Edit"] = "edited", ["Post"] = "posted", ["Approve"] = "approved", ["Reject"] = "rejected", ["Complete"] = "completed", ["Publish"] = "published", ["Enroll"] = "enrolled"
        };

        public override string Route => "users";
        protected override string[] Roles => new[] { "Admin" };

        protected UserRow U;
        protected bool Self;
        protected string Tab = "information", Summary, Tabs, Joined, LastActive, AccessText, Recent, TabPanel;
        List<AuditRow> logs = new List<AuditRow>();

        protected static string Icon(string name) => Glyphs[name];

        protected void Page_Load(object sender, EventArgs e)
        {
            var key = Request.QueryString["id"];
            if (string.IsNullOrEmpty(key) || key == "new")
            {
                CreateForm.Visible = true;
                return;
            }
            U = Data.User(QueryInt("id"));
            if (U == null)
            {
                Missing.Visible = true;
                return;
            }
            Manage.Visible = true;
            Self = U.Id == Me.Id;
            Tab = Request.QueryString["tab"] == "audit" ? "audit" : Request.QueryString["tab"] == "learning" ? "learning" : "information";
            logs = Data.Audit.Where(a => a.UserId == U.Id || a.TargetType == "User" && a.TargetId == U.Id
                || a.TargetId == null && (a.Action == "User account saved" || a.Action == "User deleted") && (a.Detail == U.Email || a.Detail == U.Name))
                .OrderByDescending(a => a.CreatedAt).ToList();
            var activity = new List<DateTime>();
            if (U.LastLogin != null) activity.Add(U.LastLogin.Value);
            activity.AddRange(Data.Enrollments.Where(x => x.UserId == U.Id).Select(x => x.LastActive ?? x.EnrolledAt));
            activity.AddRange(Insights.Attempts(this).Where(a => a.UserId == U.Id).Select(a => a.At));
            activity.AddRange(Data.Messages.Where(m => m.SenderId == U.Id).Select(m => m.SentAt));
            activity.AddRange(Data.Audit.Where(a => a.UserId == U.Id).Select(a => a.CreatedAt));
            var last = activity.Where(d => d <= DateTime.Now).DefaultIfEmpty(DateTime.MinValue).Max();
            Joined = Ui.Date(U.CreatedAt);
            LastActive = last == DateTime.MinValue ? "Not recorded" : Ui.Date(last);

            var initials = string.Concat((U.Name ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => w[0])).ToUpperInvariant();
            Summary = "<div class=\"uem-avatar\"" + (string.IsNullOrEmpty(U.Avatar) ? " aria-label=\"" + Ui.H(U.Name) + " initials\">" + Ui.H(initials) : "><img src=\"" + Ui.H(U.Avatar) + "\" alt=\"" + Ui.H(U.Name) + " profile image\">")
                + "</div><a class=\"uem-directory\" href=\"Users\">" + Icon("role") + "<span>" + Ui.H(U.Role) + "</span><span aria-hidden=\"true\">&rsaquo;</span></a><dl><div><dt>Full name</dt><dd>" + Ui.H(U.Name)
                + "</dd></div><div><dt>Email address</dt><dd>" + Ui.H(U.Email) + "</dd></div><div><dt>Username</dt><dd>" + Ui.H(string.IsNullOrEmpty(U.Username) ? "Not set" : U.Username)
                + "</dd></div><div><dt>Account status</dt><dd><i class=\"uem-status-dot\" data-status=\"" + Ui.H(U.Status) + "\" aria-hidden=\"true\"></i>" + Ui.H(U.Status) + "</dd></div><div><dt>User ID</dt><dd>" + U.Id
                + "</dd></div><div><dt>Joined</dt><dd>" + Joined + "</dd></div><div><dt>Last active</dt><dd>" + LastActive + "</dd></div></dl>";
            Tabs = string.Concat(new[] { new[] { "information", "Information", "" }, new[] { "audit", "Activity Logs", "&amp;tab=audit" }, new[] { "learning", "Learning activity", "&amp;tab=learning" } }
                .Select(t => "<a href=\"UserEditor?id=" + U.Id + t[2] + "\"" + (Tab == t[0] ? " class=\"active\" aria-current=\"page\"" : "") + ">" + t[1] + "</a>"));

            var information = Tab == "information";
            HeaderSave.Visible = InformationForm.Visible = InformationExtras.Visible = information;
            if (information) ShowInformation();
            else TabPanel = Tab == "audit" ? AuditPanel() : LearningPanel();
        }

        /// <summary>"userSave" stays as is (it has a space); a camel-case name becomes a sentence with the last word in the past tense.</summary>
        static string ActionLabel(string action)
        {
            if (Regex.IsMatch(action ?? "", @"\s")) return action;
            var words = Regex.Replace(string.IsNullOrEmpty(action) ? "Action" : action, "([a-z0-9])([A-Z])", "$1 $2").Split(' ');
            string past;
            if (Past.TryGetValue(words[words.Length - 1], out past)) words[words.Length - 1] = past;
            var text = string.Join(" ", words).ToLowerInvariant();
            return char.ToUpperInvariant(text[0]) + text.Substring(1);
        }

        void ShowInformation()
        {
            AccessText = U.Status == "Active" ? "Active &middot; Can sign in" : U.Status == "Pending" ? "Pending &middot; Awaiting approval" : "Inactive &middot; Sign-in disabled";
            StatusButton.Text = U.Status == "Inactive" ? "Reactivate account" : U.Status == "Pending" ? "Approve account" : "Deactivate account";
            StatusButton.Attributes["data-confirm"] = U.Status == "Active" ? "Deactivate this account? The user will be unable to sign in until the account is reactivated."
                : "Allow this account to sign in with its assigned role?";
            StatusButton.Enabled = DeleteButton.Enabled = !Self;
            Recent = logs.Count == 0 ? "<p class=\"uem-empty\">No activity recorded for this account.</p>"
                : "<ol>" + string.Concat(logs.Take(3).Select((a, i) => "<li><span>" + (i + 1).ToString("00", CultureInfo.InvariantCulture) + "</span><div><strong>" + Ui.H(ActionLabel(a.Action)) + "</strong><p>"
                    + Ui.H(string.IsNullOrEmpty(a.Detail) ? "—" : a.Detail) + "</p></div><time>" + Ui.Date(a.CreatedAt) + "</time></li>")) + "</ol>";
            if (IsPostBack) return;
            EditName.Text = U.Name;
            EditEmail.Text = U.Email;
            EditUsername.Text = U.Username;
            EditBio.Text = U.Bio;
            EditRole.SelectedValue = U.Role;
            EditStatus.SelectedValue = U.Status;
        }

        string AuditPanel()
        {
            var html = new StringBuilder("<section class=\"ue-log-panel\"><header class=\"uem-panel-head\"><div><h2>Activity Logs</h2><p>Actions performed by this account and account changes recorded by an administrator.</p></div><span class=\"uem-count\">"
                + Ui.Plural(logs.Count, "entry", "entries") + "</span></header><div class=\"uem-card\">");
            if (logs.Count == 0) html.Append("<p class=\"uem-empty\">No activity recorded. Account actions will appear here.</p>");
            else
            {
                html.Append("<ol class=\"uem-log-list\">");
                foreach (var a in logs)
                    html.Append("<li><span class=\"uem-log-dot\" aria-hidden=\"true\"></span><div><strong>").Append(Ui.H(ActionLabel(a.Action))).Append("</strong><p>").Append(Ui.H(string.IsNullOrEmpty(a.Detail) ? "—" : a.Detail))
                        .Append(" &middot; by ").Append(Ui.H(Data.User(a.UserId)?.Name ?? "Former user")).Append("</p></div><time datetime=\"").Append(Workspace.Iso(a.CreatedAt)).Append("\">").Append(Ui.Stamp(a.CreatedAt)).Append("</time></li>");
                html.Append("</ol>");
            }
            return html.Append("</div></section>").ToString();
        }

        string LearningPanel()
        {
            var enrolled = Data.Enrollments.Where(x => x.UserId == U.Id).Select(x => new { E = x, C = Data.Course(x.CourseId), P = Progress(U.Id, x.CourseId), Done = Data.CompletedLessons(U.Id, x.CourseId).Count }).ToList();
            var assigned = Data.Courses.Where(c => c.InstructorId == U.Id).ToList();
            var html = new StringBuilder("<section class=\"ue-log-panel\"><header class=\"uem-panel-head\"><div><h2>Learning activity</h2><p>Course progress for this account.</p></div></header><div class=\"uem-stats\"><div><small>Enrolled courses</small><strong>"
                + enrolled.Count + "</strong></div><div><small>Completed activities</small><strong>" + enrolled.Sum(x => x.Done) + "</strong></div><div><small>Average completion</small><strong>" + Charts.Avg(enrolled.Select(x => x.P))
                + "%</strong></div></div><div class=\"uem-card\">");
            if (enrolled.Count == 0) html.Append("<p class=\"uem-empty\">Not enrolled in any courses yet.</p>");
            else
            {
                html.Append("<ol class=\"uem-course-list\">");
                foreach (var x in enrolled)
                    html.Append("<li><div><strong>").Append(Ui.H(x.C?.Title ?? "Removed course")).Append("</strong><p>").Append(Ui.Plural(x.Done, "activity", "activities")).Append(" completed &middot; Last active ")
                        .Append(Ui.Date(x.E.LastActive ?? x.E.EnrolledAt)).Append("</p></div><div class=\"uem-progress\" role=\"progressbar\" aria-label=\"").Append(Ui.H(x.C?.Title ?? "Course")).Append(" completion\" aria-valuemin=\"0\" aria-valuemax=\"100\" aria-valuenow=\"")
                        .Append(x.P).Append("\"><span><i style=\"width:").Append(x.P).Append("%\"></i></span><b>").Append(x.P).Append("%</b></div></li>");
                html.Append("</ol>");
            }
            html.Append("</div>");
            if (U.Role == "Instructor" || assigned.Count > 0)
            {
                html.Append("<header class=\"uem-panel-head uem-sub-head\"><div><h2>Assigned courses</h2><p>Courses this account teaches.</p></div><span class=\"uem-count\">").Append(assigned.Count).Append("</span></header><div class=\"uem-card\">");
                if (assigned.Count == 0) html.Append("<p class=\"uem-empty\">No courses assigned to this instructor yet.</p>");
                else html.Append("<ol class=\"uem-course-list\">").Append(string.Concat(assigned.Select(c => "<li><div><strong>" + Ui.H(c.Title) + "</strong></div><span class=\"uem-pill\" data-status=\"" + Ui.H(c.Status) + "\">" + Ui.H(c.Status) + "</span></li>"))).Append("</ol>");
                html.Append("</div>");
            }
            return html.Append("</section>").ToString();
        }

        protected void Create_Click(object sender, EventArgs e)
        {
            if (!IsValid) return;
            object result;
            if (TryRun("userSave", new
            {
                id = "", name = NewName.Text.Trim(), email = NewEmail.Text.Trim(), username = NewUsername.Text.Trim(), bio = NewBio.Text, role = NewRole.SelectedValue, status = NewStatus.SelectedValue, password = NewPassword.Text
            }, out result)) Go("UserEditor?id=" + ((JObject)result)["id"], "User account created");
        }

        protected void Save_Click(object sender, EventArgs e)
        {
            if (!IsValid) return;
            if (TryRun("userSave", new
            {
                id = U.Id.ToString(CultureInfo.InvariantCulture), name = EditName.Text.Trim(), email = EditEmail.Text.Trim(), username = EditUsername.Text.Trim(), bio = EditBio.Text,
                role = EditRole.SelectedValue, status = EditStatus.SelectedValue
            })) ReloadPage("User account saved");
        }

        protected void Discard_Click(object sender, EventArgs e) => Go(Request.RawUrl);

        protected void Status_Click(object sender, EventArgs e)
        {
            if (TryRun("userSave", new
            {
                id = U.Id.ToString(CultureInfo.InvariantCulture), name = U.Name, email = U.Email, username = U.Username, bio = U.Bio ?? "", role = U.Role, status = U.Status == "Active" ? "Inactive" : "Active"
            })) ReloadPage(U.Status == "Active" ? "Account deactivated" : "Account activated");
        }

        protected void Delete_Click(object sender, EventArgs e)
        {
            if (TryRun("userDelete", new { id = U.Id.ToString(CultureInfo.InvariantCulture) })) Go("Users", "User account deleted");
        }
    }
}
