using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using SkillUp.Services;

namespace SkillUp.Models
{
    /// <summary>
    /// Create or edit a course: ?id= (none for a new course, ?program= presets its program). The Information tab holds
    /// the course fields, its activities (?lesson=id|new opens the activity form), the assessment link, the review
    /// submission and the danger actions; ?tab=learners|resources opens the other tabs.
    /// </summary>
    public sealed class CourseEditorModel : PageModel
    {
        public CourseEditorModel(SkillContext ctx) : base(ctx) { }
        public bool IsDenied, ShowLessonEditor, ShowAssignToggle, ShowWithdraw, ShowSubmit, ShowSavedParts, ShowDeleteCourse, ShowAskDeletion, AssignOpen, DeletionOpen;
        public int TabIndex;
        public string SubmitText, SubmitArg, SubmitConfirm, ProgramValue = "", CourseTitleValue = "", DeadlineValue = "", DifficultyValue = "Beginner", DescriptionValue = "", OutcomesValue = "", DurationValue = "4", StartDateValue = "";
        public IEnumerable<dynamic> ProgramOptions = new List<object>(), LessonItems = new List<object>();
        public List<object> AssignOptions = new List<object>();
        public List<EnrollmentRow> LearnerEnrollments = new List<EnrollmentRow>();

        static readonly string[] TabNames = { "information", "learners", "resources" };


        public CourseRow C;
        public bool Fresh, Locked, Deleting;
        public int TotalMinutes;
        public List<LessonRow> Lessons = new List<LessonRow>();
        public LessonRow EditingLesson;
        public string Mark, ProgramName, InstructorName, Status, QuizSummary, History;

        public bool IsLeader => Me.Role == "Program Leader";

        public void Load()
        {
            // A Program Leader has no one to ask for approval, so the review button publishes directly.
            SubmitText = IsLeader ? "Publish" : "Submit for approval";
            SubmitArg = IsLeader ? "publish" : "submit";
            SubmitConfirm = IsLeader ? "Publish this course and its current materials? Learners will be able to open it." : "Submit this course and its current materials to the Program Leader for review?";
            var id = QueryInt("id");
            C = id == null ? null : OwnCourses.FirstOrDefault(c => c.Id == id);
            if (id != null && C == null)
            {
                IsDenied = true;
                return;
            }
            Fresh = C == null;
            Deleting = !Fresh && Data.Requests.Any(r => r.CourseId == C.Id && r.Kind == "Delete" && r.Status == "Pending review");
            Locked = !Fresh && (C.Status == "Pending review" || Deleting);
            Status = C?.Status ?? "Draft";
            Mark = Fresh ? "+" : (C.Icon ?? "UP").Substring(0, Math.Min(2, (C.Icon ?? "UP").Length)).ToUpperInvariant();
            var instructor = Data.User(C?.InstructorId) ?? (Me.Role == "Instructor" ? Me : null);
            InstructorName = instructor?.Name ?? "Unassigned";

            ProgramOptions = Programs().Select(p => new { p.Id, p.Name }).ToList();
            var tab = Array.IndexOf(TabNames, Request.QueryString["tab"]);
            if (!Fresh && tab > 0) TabIndex = tab;
            FillCourse();

            if (Fresh) return;
            Lessons = Data.LessonsOf(C.Id);
            TotalMinutes = Lessons.Sum(l => l.Minutes);
            var quiz = Data.QuizFor(C.Id);
            QuizSummary = Ui.Plural(quiz?.Questions.Count ?? 0, "question") + " &middot; " + (quiz?.TimeLimit ?? 10) + " min &middot; " + (quiz?.Status == "Published" ? "Enabled" : "Draft");
            History = ReviewHistory();
            OpenLessonEditor();
            LearnerEnrollments = Data.Enrollments.Where(x => x.CourseId == C.Id).ToList();
            LessonItems = Lessons.Select(l => new { l.Id, l.Title, l.Type, l.Minutes, l.Preview }).ToList();
        }

        /// <summary>Programs a course can belong to: every program for an instructor, the leader's own for a program leader.</summary>
        IEnumerable<ProgramRow> Programs() => Data.Programs.Where(p => Me.Role == "Instructor" || p.LeaderId == Me.Id);

        void FillCourse()
        {
            var preset = QueryInt("program");
            var program = C?.ProgramId ?? (Programs().Any(p => p.Id == preset) ? preset : Programs().FirstOrDefault()?.Id);
            ProgramValue = program?.ToString(CultureInfo.InvariantCulture) ?? "";
            CourseTitleValue = C?.Title ?? "";
            DeadlineValue = Day(C?.Deadline);
            DifficultyValue = C?.Difficulty ?? "Beginner";
            DescriptionValue = C?.Description ?? "";
            OutcomesValue = C?.Outcomes ?? "";
            DurationValue = (C?.Duration ?? 4).ToString(CultureInfo.InvariantCulture);
            StartDateValue = Day(C?.StartDate);
        }

        static string Day(DateTime? date) => date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";

        /// <summary>?lesson=new or ?lesson=id shows the activity form under the activity list.</summary>
        void OpenLessonEditor()
        {
            var key = Request.QueryString["lesson"];
            if (Locked || key == null) return;
            int lessonId;
            EditingLesson = int.TryParse(key, out lessonId) ? Lessons.FirstOrDefault(l => l.Id == lessonId) : null;
            if (EditingLesson == null && key != "new") return;
            ShowLessonEditor = true;
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
        public string BackUrl
        {
            get { string url, label; BackTo("/Courses", "My courses", out url, out label, "QuizEditor", "Activity", "Course"); return url; }
        }

        public string BackLabel
        {
            get { string url, label; BackTo("/Courses", "My courses", out url, out label, "QuizEditor", "Activity", "Course"); return label; }
        }

        /// <summary>This editor's address; the tab is kept so a reload after a save returns to it.</summary>
        public string EditorUrl(string tab)
        {
            if (Fresh)
            {
                var program = QueryInt("program");
                return "/CourseEditor" + (program == null ? "" : "?program=" + program);
            }
            return "/CourseEditor?id=" + C.Id + (tab == null || tab == TabNames[0] ? "" : "&tab=" + tab);
        }

        public string LessonUrl(string lesson) => EditorUrl(null) + "&lesson=" + lesson + "#lesson-editor";

        public void Draw()
        {
            if (IsDenied) return;
            ProgramName = Data.Programs.FirstOrDefault(p => p.Id.ToString(CultureInfo.InvariantCulture) == ProgramValue)?.Name;
            AssignOpen = !Fresh && Me.Role == "Program Leader" && Request.QueryString["assign"] == "1";
            DeletionOpen = !Fresh && Me.Role == "Instructor" && Request.QueryString["deletion"] == "1";
            if (AssignOpen)
            {
                var options = new List<object> { new { Id = "", Name = "Unassigned" } };
                options.AddRange(Data.Users.Where(u => u.Role == "Instructor" && u.Status == "Active").Select(u => new { Id = u.Id.ToString(CultureInfo.InvariantCulture), u.Name }));
                AssignOptions = options;
            }
            ShowAssignToggle = !Fresh && Me.Role == "Program Leader" && !AssignOpen;
            ShowWithdraw = !Fresh && !Deleting && Status == "Pending review";
            // An instructor can only submit a course with changes since its last approval (Draft): anything else has nothing new to
            // review. A Program Leader publishes directly, so the button stays for any course that is not pending or already published.
            ShowSubmit = !Fresh && !Deleting && (IsLeader ? Status != "Pending review" && Status != "Published" : Status == "Draft");
            ShowSavedParts = !Fresh;
            ShowDeleteCourse = Me.Role != "Instructor";
            ShowAskDeletion = Me.Role == "Instructor" && !DeletionOpen;
        }

        // ---- course image ----------------------------------------------------------------------

        public const string CoverFolder = "uploads/course-art/";

        /// <summary>The image the course card shows: the uploaded cover, or the stock one.</summary>
        public string CoverUrl => C == null ? "" : CourseArt(C);
        public bool HasCover => C != null && !string.IsNullOrEmpty(C.Thumbnail);

        /// <summary>The file type from the first bytes (never from the file name the browser sent), or null when it is not a PNG, JPEG or WebP.</summary>
        public static string CoverExtension(byte[] b)
        {
            if (b.Length > 12 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return ".png";
            if (b.Length > 12 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return ".jpg";
            if (b.Length > 12 && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F' && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P') return ".webp";
            return null;
        }

    }
}
