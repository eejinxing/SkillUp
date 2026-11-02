using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using Newtonsoft.Json.Linq;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend
{
    /// <summary>
    /// Create or edit a course: ?id= (none for a new course, ?program= presets its program). The Information tab holds
    /// the course fields, its activities (?lesson=id|new opens the activity form), the assessment link, the review
    /// submission and the danger actions; ?tab=learners|resources opens the other tabs.
    /// </summary>
    public partial class CourseEditorPage : SkillPage
    {
        static readonly string[] TabNames = { "information", "learners", "resources" };

        public override string Route => "course-edit";
        protected override string[] Roles => new[] { "Instructor", "Program Leader" };

        protected CourseRow C;
        protected bool Fresh, Locked, Deleting;
        protected int TotalMinutes;
        protected List<LessonRow> Lessons = new List<LessonRow>();
        protected LessonRow EditingLesson;
        protected string Mark, ProgramName, InstructorName, Status, QuizSummary, History;

        protected void Page_Load(object sender, EventArgs e)
        {
            var id = QueryInt("id");
            C = id == null ? null : OwnCourses.FirstOrDefault(c => c.Id == id);
            if (id != null && C == null)
            {
                Denied.Visible = true;
                Editor.Visible = false;
                return;
            }
            Fresh = C == null;
            Deleting = !Fresh && Data.Requests.Any(r => r.CourseId == C.Id && r.Kind == "Delete" && r.Status == "Pending review");
            Locked = !Fresh && (C.Status == "Pending review" || Deleting);
            Status = C?.Status ?? "Draft";
            Mark = Fresh ? "+" : (C.Icon ?? "UP").Substring(0, Math.Min(2, (C.Icon ?? "UP").Length)).ToUpperInvariant();
            var instructor = Data.User(C?.InstructorId) ?? (Me.Role == "Instructor" ? Me : null);
            InstructorName = instructor?.Name ?? "Unassigned";
            Files.CourseId = C?.Id;

            if (!IsPostBack)
            {
                ProgramList.DataSource = Programs().Select(p => new { p.Id, p.Name }).ToList();
                ProgramList.DataBind();
                var tab = Array.IndexOf(TabNames, Request.QueryString["tab"]);
                if (!Fresh && tab > 0) Panels.ActiveViewIndex = tab;
            }
            if (!IsPostBack || Locked) FillCourse();

            if (Fresh) return;
            Lessons = Data.LessonsOf(C.Id);
            TotalMinutes = Lessons.Sum(l => l.Minutes);
            var quiz = Data.QuizFor(C.Id);
            QuizSummary = Ui.Plural(quiz?.Questions.Count ?? 0, "question") + " &middot; " + (quiz?.TimeLimit ?? 10) + " min &middot; " + (quiz?.Status == "Published" ? "Enabled" : "Draft");
            History = ReviewHistory();
            OpenLessonEditor();
            LearnerTable.Enrollments = Data.Enrollments.Where(x => x.CourseId == C.Id);
            if (!IsPostBack)
            {
                LessonList.DataSource = Lessons.Select(l => new { l.Id, l.Title, l.Type, l.Minutes, l.Preview }).ToList();
                LessonList.DataBind();
                LessonList.Visible = Lessons.Count > 0;
            }
        }

        /// <summary>Programs a course can belong to: every program for an instructor, the leader's own for a program leader.</summary>
        IEnumerable<ProgramRow> Programs() => Data.Programs.Where(p => Me.Role == "Instructor" || p.LeaderId == Me.Id);

        void FillCourse()
        {
            var preset = QueryInt("program");
            var program = C?.ProgramId ?? (Programs().Any(p => p.Id == preset) ? preset : Programs().FirstOrDefault()?.Id);
            Select(ProgramList, program?.ToString(CultureInfo.InvariantCulture));
            CourseTitle.Text = C?.Title ?? "";
            Deadline.Text = Day(C?.Deadline);
            Select(Difficulty, C?.Difficulty ?? "Beginner");
            Description.Text = C?.Description ?? "";
            Outcomes.Text = C?.Outcomes ?? "";
            Duration.Text = (C?.Duration ?? 4).ToString(CultureInfo.InvariantCulture);
            StartDate.Text = Day(C?.StartDate);
        }

        static string Day(DateTime? date) => date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";

        static void Select(ListControl list, string value)
        {
            if (value != null && list.Items.FindByValue(value) != null) list.SelectedValue = value;
        }

        /// <summary>?lesson=new or ?lesson=id shows the activity form under the activity list.</summary>
        void OpenLessonEditor()
        {
            var key = Request.QueryString["lesson"];
            if (Locked || key == null) return;
            int lessonId;
            EditingLesson = int.TryParse(key, out lessonId) ? Lessons.FirstOrDefault(l => l.Id == lessonId) : null;
            if (EditingLesson == null && key != "new") return;
            LessonEditor.Visible = true;
            if (IsPostBack) return;
            LessonTitle.Text = EditingLesson?.Title ?? "";
            Select(LessonType, EditingLesson?.Type ?? "article");
            LessonMinutes.Text = (EditingLesson?.Minutes ?? 15).ToString(CultureInfo.InvariantCulture);
            LessonBody.Text = EditingLesson?.Body ?? "";
            LessonAnswer.Text = EditingLesson?.Answer ?? "";
            LessonWorkspace.SelectedValue = EditingLesson?.Workspace ?? "none";
            LessonStarter.Text = EditingLesson?.StarterCode ?? "";
            LessonPreview.Checked = EditingLesson?.Preview ?? false;
        }

        /// <summary>Decisions on this course's Create requests, newest first.</summary>
        string ReviewHistory()
        {
            var events = Data.Requests.Where(r => r.CourseId == C.Id && r.Kind == "Create" && (Me.Role != "Instructor" || r.RequesterId == Me.Id))
                .SelectMany(r => r.Events).Where(x => x.Action != "Review started").OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).ToList();
            if (events.Count == 0) return "<p>No reviews yet.</p>";
            var html = new StringBuilder();
            foreach (var x in events)
                html.Append("<div class=\"list-item\">").Append(Ui.Tag(x.Action)).Append("<p>").Append(Ui.H(x.Comment)).Append("</p><small>")
                    .Append(Ui.H(Data.User(x.ActorId)?.Name ?? "Unknown user")).Append(" &middot; ").Append(Ui.Date(x.CreatedAt)).Append("</small></div>");
            return html.ToString();
        }

        /// <summary>
        /// The top-left link goes back to the page the editor was opened from (My courses, My requests, Learning resources, the
        /// Overview...), not always to My courses. The question editor and the course page are steps inside this course, so they are skipped.
        /// </summary>
        protected string BackUrl
        {
            get { string url, label; BackTo("Courses", "My courses", out url, out label, "QuizEditor", "Activity", "Course"); return url; }
        }

        protected string BackLabel
        {
            get { string url, label; BackTo("Courses", "My courses", out url, out label, "QuizEditor", "Activity", "Course"); return label; }
        }

        /// <summary>This editor's address; the tab is kept so a reload after a save returns to it.</summary>
        protected string EditorUrl(string tab)
        {
            if (Fresh)
            {
                var program = QueryInt("program");
                return "CourseEditor" + (program == null ? "" : "?program=" + program);
            }
            return "CourseEditor?id=" + C.Id + (tab == null || tab == TabNames[0] ? "" : "&tab=" + tab);
        }

        string LessonUrl(string lesson) => EditorUrl(null) + "&lesson=" + lesson + "#lesson-editor";

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            if (!Editor.Visible) return;
            ProgramName = Data.Programs.FirstOrDefault(p => p.Id.ToString(CultureInfo.InvariantCulture) == ProgramList.SelectedValue)?.Name;
            HeaderSave.Enabled = SaveButton.Enabled = !Locked;
            TabLearners.Enabled = TabResources.Enabled = !Fresh;
            var tabs = new[] { TabInformation, TabLearners, TabResources };
            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i].CssClass = Panels.ActiveViewIndex == i ? "active" : "";
                tabs[i].Attributes["aria-selected"] = Panels.ActiveViewIndex == i ? "true" : "false";
            }
            AssignToggle.Visible = !Fresh && Me.Role == "Program Leader" && !AssignForm.Visible;
            WithdrawReview.Visible = !Fresh && !Deleting && Status == "Pending review";
            // Only a course with changes since its last approval (Draft) can be submitted; anything else has nothing new to review.
            SubmitReview.Visible = !Fresh && !Deleting && Status == "Draft";
            SavedParts.Visible = !Fresh;
            CoverRemove.Visible = HasCover;
            DeleteCourse.Visible = Me.Role != "Instructor";
            AskDeletion.Visible = Me.Role == "Instructor" && !DeletionForm.Visible;
        }

        // ---- course image ----------------------------------------------------------------------

        const string CoverFolder = "uploads/course-art/";

        /// <summary>The image the course card shows: the uploaded cover, or the stock one.</summary>
        protected string CoverUrl => C == null ? "" : CourseArt(C);
        protected bool HasCover => C != null && !string.IsNullOrEmpty(C.Thumbnail);

        /// <summary>The file type from the first bytes (never from the file name the browser sent), or null when it is not a PNG, JPEG or WebP.</summary>
        static string CoverExtension(byte[] b)
        {
            if (b.Length > 12 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return ".png";
            if (b.Length > 12 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return ".jpg";
            if (b.Length > 12 && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F' && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P') return ".webp";
            return null;
        }

        string CoverFile_Path(string name) => System.IO.Path.Combine(Server.MapPath("~/Frontend/" + CoverFolder), name);

        /// <summary>Deletes a cover file this page saved earlier. Anything else in the database value is left alone.</summary>
        void DeleteCover(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith(CoverFolder, StringComparison.Ordinal)) return;
            var name = System.IO.Path.GetFileName(path);
            try { System.IO.File.Delete(CoverFile_Path(name)); } catch (System.IO.IOException) { }
        }

        protected void Cover_Click(object sender, EventArgs e)
        {
            if (C == null || Locked) return;
            if (!CoverFile.HasFile)
            {
                ShowError("Choose a PNG, JPG or WebP image.");
                return;
            }
            var bytes = CoverFile.FileBytes;
            if (bytes.Length > 2 * 1024 * 1024)
            {
                ShowError("Course images must be smaller than 2 MB.");
                return;
            }
            var extension = CoverExtension(bytes);
            if (extension == null)
            {
                ShowError("Choose a PNG, JPG or WebP image.");
                return;
            }
            var name = Tokens.New() + extension;
            System.IO.Directory.CreateDirectory(Server.MapPath("~/Frontend/" + CoverFolder));
            System.IO.File.WriteAllBytes(CoverFile_Path(name), bytes);
            object result;
            if (!TryRun("courseArt", new { courseId = C.Id.ToString(CultureInfo.InvariantCulture), path = CoverFolder + name }, out result))
            {
                System.IO.File.Delete(CoverFile_Path(name));
                return;
            }
            DeleteCover((string)((JObject)result)?["previous"]);
            ReloadPage("Course image updated");
        }

        protected void CoverRemove_Click(object sender, EventArgs e)
        {
            if (C == null || Locked) return;
            object result;
            if (!TryRun("courseArt", new { courseId = C.Id.ToString(CultureInfo.InvariantCulture), path = "" }, out result)) return;
            DeleteCover((string)((JObject)result)?["previous"]);
            ReloadPage("Course image removed");
        }

        protected void Tab_Command(object sender, CommandEventArgs e)
        {
            var tab = int.Parse(e.CommandArgument.ToString(), CultureInfo.InvariantCulture);
            if (Fresh && tab > 0) return;
            Panels.ActiveViewIndex = tab;
            // Later saves post to the tab's address, so the reload after them stays on this tab.
            Form.Action = EditorUrl(TabNames[tab]);
        }

        protected void Save_Click(object sender, EventArgs e)
        {
            if (!IsValid) return;
            object result;
            if (!TryRun("courseSave", new
            {
                id = C?.Id.ToString(CultureInfo.InvariantCulture) ?? "",
                title = CourseTitle.Text, programId = ProgramList.SelectedValue, deadline = Deadline.Text, difficulty = Difficulty.SelectedValue,
                description = Description.Text, outcomes = Outcomes.Text, duration = Duration.Text, startDate = StartDate.Text
            }, out result)) return;
            Go("CourseEditor?id=" + ((JObject)result)["id"], Fresh ? "Course created" : "Course saved");
        }

        protected void CourseAction_Command(object sender, CommandEventArgs e)
        {
            var op = e.CommandArgument.ToString();
            if (!TryRun("courseAction", new { id = C.Id.ToString(CultureInfo.InvariantCulture), action = op })) return;
            if (op == "delete") Go("Courses", "Course deleted");
            ReloadPage(op == "submit" ? "Course submitted for review" : op == "withdraw" ? "Submission withdrawn" : "Course hidden");
        }

        protected void AssignToggle_Click(object sender, EventArgs e)
        {
            AssignForm.Visible = true;
            var options = new List<object> { new { Id = "", Name = "Unassigned" } };
            options.AddRange(Data.Users.Where(u => u.Role == "Instructor" && u.Status == "Active").Select(u => new { Id = u.Id.ToString(CultureInfo.InvariantCulture), u.Name }));
            AssignInstructor.DataSource = options;
            AssignInstructor.DataBind();
            AssignInstructor.SelectedValue = C.InstructorId?.ToString(CultureInfo.InvariantCulture) ?? "";
        }

        protected void Assign_Click(object sender, EventArgs e)
        {
            if (TryRun("assign", new { courseId = C.Id.ToString(CultureInfo.InvariantCulture), instructorId = AssignInstructor.SelectedValue }))
                ReloadPage("Instructor assignment saved");
        }

        protected void EditLesson_Command(object sender, CommandEventArgs e) => Go(LessonUrl(e.CommandArgument.ToString()));

        protected void SaveLesson_Click(object sender, EventArgs e)
        {
            if (!IsValid) return;
            var type = LessonType.SelectedValue;
            if (TryRun("lessonSave", new
            {
                courseId = C.Id.ToString(CultureInfo.InvariantCulture), id = EditingLesson?.Id.ToString(CultureInfo.InvariantCulture) ?? "",
                title = LessonTitle.Text, type, minutes = LessonMinutes.Text, body = LessonBody.Text, answer = LessonAnswer.Text,
                workspace = LessonWorkspace.SelectedValue, starterCode = LessonStarter.Text,
                preview = LessonPreview.Checked && type == "article"
            })) Go(EditorUrl(null), "Activity saved");
        }

        protected void DeleteLesson_Command(object sender, CommandEventArgs e)
        {
            if (TryRun("lessonDelete", new { courseId = C.Id.ToString(CultureInfo.InvariantCulture), id = e.CommandArgument.ToString() }))
                ReloadPage("Activity deleted");
        }

        protected void AskDeletion_Click(object sender, EventArgs e) => DeletionForm.Visible = true;

        protected void RequestDeletion_Click(object sender, EventArgs e)
        {
            if (!IsValid) return;
            object result;
            if (TryRun("courseRequest", new { kind = "Delete", courseId = C.Id.ToString(CultureInfo.InvariantCulture), reason = DeleteReason.Text }, out result))
                Go("CourseRequests?id=" + ((JObject)result)["id"], "Deletion request submitted");
        }
    }
}
