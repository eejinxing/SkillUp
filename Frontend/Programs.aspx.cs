using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI.WebControls;
using Newtonsoft.Json.Linq;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend
{
    /// <summary>
    /// Program leader: the programs the leader runs (?q= searches) and, with ?id=, one program's learning path editor.
    /// Path changes are kept in the session until Save path. ?edit=, ?menu=, ?delete=, ?help=1, ?add=1 and ?move=course
    /// open the forms that used to be dialogs.
    /// </summary>
    public partial class ProgramsPage : SkillPage
    {
        protected sealed class ProgramInfo
        {
            public ProgramRow P;
            public List<CourseRow> Courses;
            public List<int> Path;
            public List<UserRow> Instructors;
        }

        static readonly string[] Covers =
        {
            "imgThumbnailEditableGeometricArtwork.svg", "imgThumbnailEditableGeometricArtwork4.svg", "imgThumbnailEditableGeometricArtwork5.svg",
            "imgThumbnailEditableGeometricArtwork1.svg", "imgThumbnailEditableGeometricArtwork2.svg", "imgThumbnailEditableGeometricArtwork3.svg"
        };
        static readonly string[] Tones = { "#6d5dfc", "#14b8a6", "#f59e0b", "#ec4899", "#3b82f6", "#84cc16" };

        public override string Route => "programs";
        protected override string[] Roles => new[] { "Program Leader" };

        protected List<ProgramInfo> Rows = new List<ProgramInfo>();
        protected ProgramRow P, FormProgram, MenuProgram;
        protected List<CourseRow> Assigned = new List<CourseRow>(), Available = new List<CourseRow>();
        protected List<int> SavedPath = new List<int>(), Path = new List<int>();
        protected int Enrolments;
        protected bool NoMatches;
        protected string Rail, Cards, Cover, DraftState, MoveTitle, MoveLabel, MoveMessage, MoveAction;
        CourseRow moving;
        bool adding;

        protected string HomeUrl => P == null ? "Programs" : "Programs?id=" + P.Id;
        string DraftKey(int programId) => "SkillUp.ProgramPath." + Me.Id + "." + programId;

        protected void Page_Load(object sender, EventArgs e)
        {
            Rows = Data.Programs.Where(p => p.LeaderId == Me.Id).Select(p =>
            {
                var courses = Data.Courses.Where(c => c.ProgramId == p.Id).ToList();
                var ids = new HashSet<int>(courses.Select(c => c.Id));
                return new ProgramInfo
                {
                    P = p, Courses = courses, Path = Data.PathOf(p.Id).Where(ids.Contains).Distinct().ToList(),
                    Instructors = courses.Where(c => c.InstructorId != null).Select(c => c.InstructorId).Distinct().Select(id => Data.User(id)).Where(u => u != null).ToList()
                };
            }).ToList();
            Rail = "<nav class=\"pm-rail\" aria-label=\"Program management navigation\">" + string.Concat(new[]
            {
                new[] { "Dashboard", "Overview", "grid" }, new[] { "Programs", "Programs", "book" }, new[] { "Assignments", "Instructors", "users" },
                new[] { "Approvals", "Course approvals", "check" }, new[] { "Analytics", "Program analytics", "chart" }
            }.Select(l => "<a href=\"" + l[0] + "\" aria-label=\"" + l[1] + "\" title=\"" + l[1] + "\"" + (l[0] == "Programs" ? " aria-current=\"page\"" : "") + ">" + Icons.App(l[2]) + "</a>")) + "</nav>";

            if (Request.QueryString["id"] != null)
            {
                var info = Rows.FirstOrDefault(r => r.P.Id == QueryInt("id"));
                if (info == null)
                {
                    Denied.Visible = true;
                    Library.Visible = false;
                    return;
                }
                ShowEditor(info);
            }
            else ShowLibrary();
            OpenForms();
        }

        // ---- program library -----------------------------------------------------------------

        void ShowLibrary()
        {
            var term = (Request.QueryString["q"] ?? "").Trim();
            if (!IsPostBack) SearchBox.Text = term;
            var html = new StringBuilder();
            for (int i = 0; i < Rows.Count; i++)
            {
                var r = Rows[i];
                if ((r.P.Name + " " + r.P.Description).IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0) continue;
                html.Append(Card(r, i));
            }
            Cards = html.ToString();
            NoMatches = Rows.Count > 0 && Cards.Length == 0;
        }

        static string Card(ProgramInfo r, int i)
        {
            var initials = string.Concat(Regex.Split(r.P.Name ?? "", @"\s+").Select(w => Regex.Replace(w, "[^A-Za-z0-9]", "")).Where(w => w.Length > 0).Select(w => w[0]));
            initials = initials.Length == 0 ? "P" : initials.Substring(0, Math.Min(2, initials.Length)).ToUpperInvariant();
            var chips = r.Courses.Count == 0 ? "<i class=\"is-empty\">No courses yet</i>" : string.Concat(r.Courses.Take(3).Select(c => "<i>" + Ui.H(c.Title) + "</i>"));
            var people = string.Concat(r.Instructors.Take(3).Select(u => "<span class=\"pm-person\">"
                + (string.IsNullOrEmpty(u.Avatar) ? Ui.H(string.Concat((u.Name ?? "?").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => w[0]))) : "<img src=\"" + Ui.H(u.Avatar) + "\" alt=\"\">")
                + "</span>"));
            return "<article class=\"pm-card\" data-program-id=\"" + r.P.Id + "\"><a class=\"pm-card-link\" href=\"Programs?id=" + r.P.Id + "\" aria-label=\"Manage courses in " + Ui.H(r.P.Name) + "\">"
                + "<span class=\"pm-art\" style=\"--pm-c:" + Tones[i % Tones.Length] + "\"><span class=\"pm-art-mark\" aria-hidden=\"true\">" + Ui.H(initials) + "</span><span class=\"pm-art-chips\" aria-hidden=\"true\">" + chips + "</span></span>"
                + "<span class=\"pm-body\"><span class=\"pm-title\"><strong title=\"" + Ui.H(r.P.Name) + "\">" + Ui.H(r.P.Name) + "</strong><span>" + Ui.Plural(r.Courses.Count, "course") + "</span></span>"
                + "<span class=\"pm-state" + (r.P.Published ? " is-live" : "") + "\"><i aria-hidden=\"true\"></i>" + (r.P.Published ? "Published" : "Draft") + " &middot; " + r.Path.Count + " in learning path</span></span></a>"
                + "<a class=\"pm-menu\" href=\"Programs?menu=" + r.P.Id + "\" aria-label=\"Actions for " + Ui.H(r.P.Name) + "\">&#8942;</a>"
                + "<a class=\"pm-people\" href=\"Assignments\" aria-label=\"Manage instructors for " + Ui.H(r.P.Name) + "\">" + people + "<span class=\"pm-people-count\">" + Ui.Plural(r.Instructors.Count, "instructor") + "</span></a></article>";
        }

        // ---- one program's learning path ---------------------------------------------------------

        void ShowEditor(ProgramInfo info)
        {
            P = info.P;
            Library.Visible = false;
            Editor.Visible = true;
            Assigned = info.Courses;
            SavedPath = info.Path;
            var assigned = new HashSet<int>(Assigned.Select(c => c.Id));
            var draft = Session[DraftKey(P.Id)] as List<int>;
            Path = (draft ?? SavedPath).Where(assigned.Contains).Distinct().ToList();
            Available = Assigned.Where(c => !Path.Contains(c.Id)).ToList();
            Enrolments = Data.Enrollments.Count(x => assigned.Contains(x.CourseId));
            Cover = Covers[Rows.IndexOf(info) % Covers.Length];
            DraftState = draft == null ? "No unsaved path changes." : "Unsaved path changes kept for this session. Save path to apply them.";
            if (IsPostBack) return;
            PathRows.DataSource = Path.Select(id => Data.Course(id)).Select(c => new { c.Id, c.Title, c.Status, Instructor = Data.User(c.InstructorId)?.Name ?? "Unassigned instructor" }).ToList();
            PathRows.DataBind();
            AvailableRows.DataSource = Available.Select(c => new { c.Id, c.Title, c.Status }).ToList();
            AvailableRows.DataBind();
            AssignedRows.DataSource = Assigned.Select(c => new { c.Id, c.Title, c.Status }).ToList();
            AssignedRows.DataBind();
        }

        /// <summary>The path being edited is kept in the session; every change reloads the editor so a refresh never repeats it.</summary>
        void ChangePath(Func<List<int>, List<int>> change)
        {
            Session[DraftKey(P.Id)] = change(new List<int>(Path));
            Go(HomeUrl);
        }

        static int Arg(CommandEventArgs e) => int.Parse(e.CommandArgument.ToString(), CultureInfo.InvariantCulture);

        protected void Up_Command(object sender, CommandEventArgs e) => ChangePath(ids =>
        {
            var i = ids.IndexOf(Arg(e));
            if (i > 0) { ids[i] = ids[i - 1]; ids[i - 1] = Arg(e); }
            return ids;
        });

        protected void Down_Command(object sender, CommandEventArgs e) => ChangePath(ids =>
        {
            var i = ids.IndexOf(Arg(e));
            if (i >= 0 && i < ids.Count - 1) { ids[i] = ids[i + 1]; ids[i + 1] = Arg(e); }
            return ids;
        });

        protected void Remove_Command(object sender, CommandEventArgs e) => ChangePath(ids => ids.Where(id => id != Arg(e)).ToList());

        protected void Add_Command(object sender, CommandEventArgs e) => ChangePath(ids =>
        {
            if (!ids.Contains(Arg(e))) ids.Add(Arg(e));
            return ids;
        });

        protected void Discard_Click(object sender, EventArgs e)
        {
            Session.Remove(DraftKey(P.Id));
            Go(HomeUrl, "Path restored to the saved order.");
        }

        protected void SavePath_Click(object sender, EventArgs e)
        {
            if (!TryRun("pathSave", new { id = P.Id.ToString(CultureInfo.InvariantCulture), courseIds = Path.Select(id => id.ToString(CultureInfo.InvariantCulture)) })) return;
            Session.Remove(DraftKey(P.Id));
            Go(HomeUrl, "Learning path saved");
        }

        // ---- forms --------------------------------------------------------------------------------

        void OpenForms()
        {
            var query = Request.QueryString;
            if (query["edit"] != null)
            {
                FormProgram = P ?? Rows.FirstOrDefault(r => r.P.Id == QueryInt("edit"))?.P;
                ProgramForm.Visible = query["edit"] == "new" || FormProgram != null;
                if (ProgramForm.Visible && !IsPostBack && FormProgram != null)
                {
                    ProgramName.Text = FormProgram.Name;
                    ProgramDescription.Text = FormProgram.Description;
                    ProgramPublished.Checked = FormProgram.Published;
                }
            }
            MenuProgram = Rows.FirstOrDefault(r => r.P.Id == QueryInt("menu"))?.P;
            MenuPanel.Visible = MenuProgram != null;
            DeletePanel.Visible = query["delete"] != null && (P != null || Rows.Any(r => r.P.Id == QueryInt("delete")));
            HelpPanel.Visible = query["help"] == "1";
            if (P == null || (query["add"] != "1" && query["move"] == null)) return;

            adding = query["add"] == "1";
            moving = adding ? null : Assigned.FirstOrDefault(c => c.Id == QueryInt("move"));
            if (!adding && moving == null) return;
            MovePanel.Visible = true;
            MoveTitle = adding ? "Add existing course" : "Move " + moving.Title;
            MoveLabel = adding ? "Course to move" : "Move to program";
            var targets = Rows.Where(r => r.P.Id != P.Id).ToList();
            var choices = adding
                ? targets.SelectMany(r => r.Courses.Select(c => new { Text = c.Title + " · " + r.P.Name, Value = c.Id.ToString(CultureInfo.InvariantCulture) })).ToList()
                : targets.Select(r => new { Text = r.P.Name, Value = r.P.Id.ToString(CultureInfo.InvariantCulture) }).ToList();
            if (Session[DraftKey(P.Id)] != null) MoveMessage = "Save or discard path changes before moving courses.";
            else if (choices.Count == 0)
            {
                MoveMessage = adding ? "No courses are available in your other managed programs." : "Create another program before moving this course.";
                MoveAction = adding ? "<a class=\"button\" href=\"CourseEditor?program=" + P.Id + "\">Create a course</a>" : "<a class=\"button\" href=\"Programs?edit=new\">Create program</a>";
            }
            MoveBlocked.Visible = MoveMessage != null;
            MoveFields.Visible = MoveMessage == null;
            if (MoveFields.Visible && !IsPostBack)
            {
                MoveChoice.DataSource = choices;
                MoveChoice.DataBind();
            }
        }

        protected void Open_Command(object sender, CommandEventArgs e) => Go(e.CommandArgument.ToString());

        protected void EditorOpen_Command(object sender, CommandEventArgs e) => Go(HomeUrl + "&" + e.CommandArgument);

        protected void Search_Click(object sender, EventArgs e)
        {
            var term = SearchBox.Text.Trim();
            Go(term.Length == 0 ? "Programs" : "Programs?q=" + HttpUtility.UrlEncode(term));
        }

        protected void SaveProgram_Click(object sender, EventArgs e)
        {
            if (!IsValid) return;
            object result;
            if (TryRun("programSave", new
            {
                id = FormProgram?.Id.ToString(CultureInfo.InvariantCulture) ?? "", name = ProgramName.Text, description = ProgramDescription.Text, published = ProgramPublished.Checked
            }, out result)) Go("Programs?id=" + ((JObject)result)["id"], "Program saved");
        }

        protected void DeleteProgram_Click(object sender, EventArgs e)
        {
            var id = P?.Id ?? QueryInt("delete");
            if (TryRun("programDelete", new { id = id?.ToString(CultureInfo.InvariantCulture) })) Go("Programs", "Program deleted");
        }

        protected void MoveCourse_Click(object sender, EventArgs e)
        {
            var targetProgram = adding ? P.Id : int.Parse(MoveChoice.SelectedValue, CultureInfo.InvariantCulture);
            if (Session[DraftKey(P.Id)] != null || Session[DraftKey(targetProgram)] != null)
            {
                ShowError("Save or discard path changes before moving courses.");
                return;
            }
            if (TryRun("programCourseMove", new
            {
                courseId = adding ? MoveChoice.SelectedValue : moving.Id.ToString(CultureInfo.InvariantCulture), programId = targetProgram.ToString(CultureInfo.InvariantCulture),
                includeInPath = IncludeInPath.Checked
            })) Go(HomeUrl, "Course moved");
        }
    }
}
