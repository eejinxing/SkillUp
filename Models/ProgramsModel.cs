using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SkillUp.Services;

namespace SkillUp.Models
{
    /// <summary>
    /// Program leader: the programs the leader runs (?q= searches) and, with ?id=, one program's learning path editor.
    /// Path changes are kept in the session until Save path. ?edit=, ?menu=, ?delete=, ?help=1, ?add=1 and ?move=course
    /// open the forms that used to be dialogs.
    /// </summary>
    public sealed class ProgramsModel : PageModel
    {
        public ProgramsModel(SkillContext ctx) : base(ctx) { }
        public bool IsDenied, IsEditor, ShowProgramForm, ShowMenu, ShowDelete, ShowHelp, ShowMove, MoveBlocked, MoveAdding;
        public string SearchTerm = "";
        public int? DeleteId, MovingId;
        public IEnumerable<dynamic> PathItems = new List<object>(), AvailableItems = new List<object>(), AssignedItems = new List<object>(), MoveChoices = new List<object>();

        public sealed class ProgramInfo
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


        public List<ProgramInfo> Rows = new List<ProgramInfo>();
        public ProgramRow P, FormProgram, MenuProgram;
        public List<CourseRow> Assigned = new List<CourseRow>(), Available = new List<CourseRow>();
        public List<int> SavedPath = new List<int>(), Path = new List<int>();
        public int Enrolments;
        public bool NoMatches;
        public string Rail, Cards, Cover, DraftState, MoveTitle, MoveLabel, MoveMessage, MoveAction;
        CourseRow moving;
        bool adding;

        public string HomeUrl => P == null ? "/Programs" : "/Programs?id=" + P.Id;

        // The learning path being edited is kept in the session until it is saved or discarded.
        public static string DraftKey(int userId, int programId) => "SkillUp.ProgramPath." + userId + "." + programId;

        public static List<int> ReadDraft(SkillContext ctx, int programId)
        {
            var text = ctx.Http.Session.GetString(DraftKey(ctx.Me.Id, programId));
            return text == null ? null : (text.Length == 0 ? new List<int>() : text.Split(',').Select(int.Parse).ToList());
        }

        public static void WriteDraft(SkillContext ctx, int programId, IEnumerable<int> path) => ctx.Http.Session.SetString(DraftKey(ctx.Me.Id, programId), string.Join(",", path));

        public static void ClearDraft(SkillContext ctx, int programId) => ctx.Http.Session.Remove(DraftKey(ctx.Me.Id, programId));

        public void Load()
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
            }.Select(l => "<a href=\"/" + l[0] + "\" aria-label=\"" + l[1] + "\" title=\"" + l[1] + "\"" + (l[0] == "Programs" ? " aria-current=\"page\"" : "") + ">" + Icons.App(l[2]) + "</a>")) + "</nav>";

            if (Request.QueryString["id"] != null)
            {
                var info = Rows.FirstOrDefault(r => r.P.Id == QueryInt("id"));
                if (info == null)
                {
                    IsDenied = true;
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
            SearchTerm = term;
            var html = new StringBuilder();
            // Names that start with what was typed come first; each card keeps its own position number (its colour).
            foreach (var i in Enumerable.Range(0, Rows.Count).Where(i => SearchMatch.Matches(term, Rows[i].P.Name, Rows[i].P.Description, true)).OrderBy(i => SearchMatch.Rank(term, Rows[i].P.Name)))
                html.Append(Card(Rows[i], i));
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
            return "<article class=\"pm-card\" data-program-id=\"" + r.P.Id + "\"><a class=\"pm-card-link\" href=\"/Programs?id=" + r.P.Id + "\" aria-label=\"Manage courses in " + Ui.H(r.P.Name) + "\">"
                + "<span class=\"pm-art\" style=\"--pm-c:" + Tones[i % Tones.Length] + "\"><span class=\"pm-art-mark\" aria-hidden=\"true\">" + Ui.H(initials) + "</span><span class=\"pm-art-chips\" aria-hidden=\"true\">" + chips + "</span></span>"
                + "<span class=\"pm-body\"><span class=\"pm-title\"><strong title=\"" + Ui.H(r.P.Name) + "\">" + Ui.H(r.P.Name) + "</strong><span>" + Ui.Plural(r.Courses.Count, "course") + "</span></span>"
                + "<span class=\"pm-state" + (r.P.Published ? " is-live" : "") + "\"><i aria-hidden=\"true\"></i>" + (r.P.Published ? "Published" : "Draft") + " &middot; " + r.Path.Count + " in learning path</span></span></a>"
                + "<a class=\"pm-menu\" href=\"/Programs?menu=" + r.P.Id + "\" aria-label=\"Actions for " + Ui.H(r.P.Name) + "\">&#8942;</a>"
                + "<a class=\"pm-people\" href=\"/Assignments\" aria-label=\"Manage instructors for " + Ui.H(r.P.Name) + "\">" + people + "<span class=\"pm-people-count\">" + Ui.Plural(r.Instructors.Count, "instructor") + "</span></a></article>";
        }

        // ---- one program's learning path ---------------------------------------------------------

        void ShowEditor(ProgramInfo info)
        {
            P = info.P;
            IsEditor = true;
            Assigned = info.Courses;
            SavedPath = info.Path;
            var assigned = new HashSet<int>(Assigned.Select(c => c.Id));
            var draft = ReadDraft(Ctx, P.Id);
            Path = (draft ?? SavedPath).Where(assigned.Contains).Distinct().ToList();
            Available = Assigned.Where(c => !Path.Contains(c.Id)).ToList();
            Enrolments = Data.Enrollments.Count(x => assigned.Contains(x.CourseId));
            Cover = Covers[Rows.IndexOf(info) % Covers.Length];
            DraftState = draft == null ? "No unsaved path changes." : "Unsaved path changes kept for this session. Save path to apply them.";
            PathItems = Path.Select(id => Data.Course(id)).Select(c => new { c.Id, c.Title, c.Status, Instructor = Data.User(c.InstructorId)?.Name ?? "Unassigned instructor" }).ToList();
            AvailableItems = Available.Select(c => new { c.Id, c.Title, c.Status }).ToList();
            AssignedItems = Assigned.Select(c => new { c.Id, c.Title, c.Status }).ToList();
        }

        /// <summary>The path being edited is kept in the session; every change reloads the editor so a refresh never repeats it.</summary>

        // ---- forms --------------------------------------------------------------------------------

        void OpenForms()
        {
            var query = Request.QueryString;
            if (query["edit"] != null)
            {
                FormProgram = P ?? Rows.FirstOrDefault(r => r.P.Id == QueryInt("edit"))?.P;
                ShowProgramForm = query["edit"] == "new" || FormProgram != null;
            }
            MenuProgram = Rows.FirstOrDefault(r => r.P.Id == QueryInt("menu"))?.P;
            ShowMenu = MenuProgram != null;
            DeleteId = P?.Id ?? QueryInt("delete");
            ShowDelete = query["delete"] != null && (P != null || Rows.Any(r => r.P.Id == QueryInt("delete")));
            ShowHelp = query["help"] == "1";
            if (P == null || (query["add"] != "1" && query["move"] == null)) return;

            adding = query["add"] == "1";
            moving = adding ? null : Assigned.FirstOrDefault(c => c.Id == QueryInt("move"));
            if (!adding && moving == null) return;
            ShowMove = true;
            MoveTitle = adding ? "Add existing course" : "Move " + moving.Title;
            MoveLabel = adding ? "Course to move" : "Move to program";
            MoveAdding = adding;
            MovingId = moving?.Id;
            var targets = Rows.Where(r => r.P.Id != P.Id).ToList();
            var choices = adding
                ? targets.SelectMany(r => r.Courses.Select(c => new { Text = c.Title + " · " + r.P.Name, Value = c.Id.ToString(CultureInfo.InvariantCulture) })).ToList()
                : targets.Select(r => new { Text = r.P.Name, Value = r.P.Id.ToString(CultureInfo.InvariantCulture) }).ToList();
            if (ReadDraft(Ctx, P.Id) != null) MoveMessage = "Save or discard path changes before moving courses.";
            else if (choices.Count == 0)
            {
                MoveMessage = adding ? "No courses are available in your other managed programs." : "Create another program before moving this course.";
                MoveAction = adding ? "<a class=\"button\" href=\"/CourseEditor?program=" + P.Id + "\">Create a course</a>" : "<a class=\"button\" href=\"/Programs?edit=new\">Create program</a>";
            }
            MoveBlocked = MoveMessage != null;
            if (!MoveBlocked) MoveChoices = choices;
        }

    }
}
