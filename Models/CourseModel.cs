using System;
using System.Collections.Generic;
using System.Linq;
using SkillUp.Services;

namespace SkillUp.Models
{
    /// <summary>
    /// Course overview and enrolment (?id=). Visitors may open the first two activities as a preview;
    /// learners enrol and then open activities in order; staff who manage the course get a link to edit or review it.
    /// </summary>
    public sealed class CourseModel : PageModel
    {
        public CourseModel(SkillContext ctx) : base(ctx) { }
        public string PageTitle, ActionText, ActionUrl, MyRating = "5", MyText = "";
        public bool IsUnavailable, ShowFiles, ShowEnrol, NoLessons, WriteReview, NoReviews;
        public IEnumerable<string> Outcomes;
        public IEnumerable<dynamic> Curriculum, RatingBars, ReviewList;

        /// <summary>Visitors who are not signed in may look through this many opening activities of a course.</summary>
        public const int GuestPreviewCount = 2;


        public CourseRow C;

        /// <summary>
        /// The breadcrumb's back link. A learner who opened the course from My courses (?from=mycourses) goes back there, to
        /// the program the course belongs to; everyone else goes to the catalogue (visitors to the landing page's courses).
        /// </summary>
        public string BackUrl => Me?.Role == "Learner" && Request.QueryString["from"] == "mycourses" && C != null ? "/MyCourses?program=" + C.ProgramId
            : Me == null ? Routes.HomeCourses : StaffBack().Item1;
        public string BackLabel => Me?.Role == "Learner" && Request.QueryString["from"] == "mycourses" ? "My courses" : Me == null ? "Our Courses" : StaffBack().Item2;

        /// <summary>Instructors and other staff go back to the page they opened the course from; everyone else to the catalogue.</summary>
        Tuple<string, string> StaffBack()
        {
            string url = "/Catalog", label = "All courses";
            if (Me != null && Me.Role != "Learner") BackTo("/Catalog", "All courses", out url, out label, "Activity", "Certificates");
            return Tuple.Create(url, label);
        }
        public List<LessonRow> Lessons;
        public EnrollmentRow Enrollment;
        public List<int> Completed = new List<int>();
        public UserRow Instructor;
        public string ProgramName, InstructorName, Rating, ReviewAverage;
        public int TotalMinutes, Percent, LearnerCount;
        public bool CanManage, AllReviews;
        public List<FeedbackRow> Reviews;

        public void Load()
        {
            var id = QueryInt("id");
            C = id == null ? null : Data.Course(id.Value);
            if (C == null || C.Status != "Published")
            {
                IsUnavailable = true;
                return;
            }
            PageTitle = C.Title;
            Lessons = Data.LessonsOf(C.Id);
            TotalMinutes = Lessons.Sum(l => l.Minutes);
            var program = Data.Program(C.ProgramId);
            ProgramName = program?.Name ?? "Skill UP";
            Instructor = Data.User(C.InstructorId);
            InstructorName = Instructor?.Name ?? "Former user";
            Enrollment = Me == null ? null : Data.Enrollments.FirstOrDefault(x => x.UserId == Me.Id && x.CourseId == C.Id);
            if (Enrollment != null)
            {
                Completed = Data.CompletedLessons(Me.Id, C.Id);
                Percent = Progress(Me.Id, C.Id);
            }
            CanManage = Me != null && (Me.Role == "Admin" || (Me.Role == "Instructor" && C.InstructorId == Me.Id) || (Me.Role == "Program Leader" && program?.LeaderId == Me.Id));
            LearnerCount = Data.Enrollments.Count(x => x.CourseId == C.Id);
            var own = Data.Feedback.Where(f => f.CourseId == C.Id).ToList();
            Rating = own.Count == 0 ? null : own.Average(f => f.Rating).ToString("0.0");

            Outcomes = (C.Outcomes ?? "").Split('\n').Select(o => o.Trim()).Where(o => o.Length > 0).ToList();
            BindCurriculum();
            NoLessons = Lessons.Count == 0;
            BindAction();

            // Course files: for enrolled learners and the staff who manage it, once there is a file or something to upload.
            ShowFiles = (Enrollment != null || CanManage) && (CanManage || Data.Resources.Any(r => r.CourseId == C.Id));

            AllReviews = Request.QueryString["scope"] == "all";
            BindReviews();
        }

        void BindCurriculum()
        {
            Curriculum = Lessons.Select((lesson, i) =>
            {
                var state = LessonState(lesson, i);
                var url = "/Activity?course=" + C.Id + "&lesson=" + lesson.Id;
                string action = "";
                if (state == "guest") action = Link(url, "Preview activity");
                else if (state == "done" || state == "next") action = Link(url, state == "done" ? "Review activity" : "Open activity");
                else if (state == "managed" && Me.Role == "Instructor") action = Link("/CourseEditor?id=" + C.Id, "Edit in course editor");
                return new
                {
                    lesson.Id,
                    State = state,
                    Number = (i + 1).ToString("00"),
                    lesson.Title,
                    lesson.Type,
                    lesson.Minutes,
                    Label = StateLabel(state, lesson),
                    Note = StateNote(state, lesson),
                    // Signed-in learners who have not enrolled can read a free-preview article here.
                    PreviewHtml = state == "preview" ? "<div class=\"article ce-preview\"><h4>" + Ui.H(lesson.Title) + "</h4><p>" + Ui.H(lesson.Body) + "</p></div>" : "",
                    ActionHtml = action
                };
            }).ToList();
        }

        static string Link(string url, string text) => "<a class=\"ce-activity-button\" href=\"" + url + "\">" + text + "</a>";

        string LessonState(LessonRow lesson, int i)
        {
            if (Me == null) return i < GuestPreviewCount ? "guest" : "locked";
            if (Completed.Contains(lesson.Id)) return "done";
            if (Enrollment != null) return i > 0 && !Completed.Contains(Lessons[i - 1].Id) ? "locked" : "next";
            if (CanManage) return "managed";
            return lesson.Preview ? "preview" : "closed";
        }

        static string StateLabel(string state, LessonRow lesson)
        {
            switch (state)
            {
                case "done": return "Completed";
                case "locked": return "Locked";
                case "next": return "Up next";
                case "preview":
                case "guest": return "Free preview";
                case "managed": return lesson.Preview ? "Free preview" : "Learners only";
                default: return "Enrol to unlock";
            }
        }

        string StateNote(string state, LessonRow lesson)
        {
            switch (state)
            {
                case "done": return "Completed. You can revisit this activity at any time.";
                case "locked": return Me != null ? "Complete the previous activity to unlock this one." : "Log in and enrol to unlock this activity.";
                case "guest": return "Look through this activity before you sign up. Answering, hints and saved progress need an account.";
                case "next": return "This is your next step. Open the activity when you are ready.";
                case "managed": return lesson.Preview ? "Visitors can preview this reading before enrolling." : "Enrolled learners open this activity in order.";
                case "preview": return "Preview the reading before joining this course.";
                default: return "Enrol in this course to open this activity and save your progress.";
            }
        }

        /// <summary>The enrol card's main button: continue, certificate, enrol, sign in, manage, or explore.</summary>
        void BindAction()
        {
            ShowEnrol = false;
            if (Enrollment != null)
            {
                var next = Lessons.FirstOrDefault(l => !Completed.Contains(l.Id));
                ActionText = next != null ? "Continue learning" : "View certificate";
                ActionUrl = next != null ? "/Activity?course=" + C.Id + "&lesson=" + next.Id : "/Certificates?id=" + C.Id + "&from=course" + (Request.QueryString["from"] == "mycourses" ? "&via=mycourses" : "");
            }
            else if (Me?.Role == "Learner")
            {
                ActionText = null;
                ShowEnrol = true;
            }
            else if (Me == null)
            {
                ActionText = "Sign in to enrol";
                ActionUrl = "#login";
            }
            else if (CanManage)
            {
                ActionText = "Manage course";
                ActionUrl = Me.Role == "Instructor" ? "/CourseEditor?id=" + C.Id : "/CourseReview?id=" + C.Id;
            }
            else
            {
                ActionText = "Explore courses";
                ActionUrl = "/Catalog";
            }
        }

        void BindReviews()
        {
            Reviews = Data.Feedback.Where(f => f.CourseId != null && (AllReviews || f.CourseId == C.Id))
                .Where(f => VisibleCourses.Any(c => c.Id == f.CourseId)).OrderByDescending(f => f.CreatedAt).ToList();
            ReviewAverage = Reviews.Count == 0 ? "—" : Reviews.Average(f => f.Rating).ToString("0.0");
            RatingBars = new[] { 5, 4, 3, 2, 1 }.Select(n =>
            {
                var percent = Reviews.Count == 0 ? 0 : 100.0 * Reviews.Count(r => r.Rating == n) / Reviews.Count;
                return new { Label = n + " star" + (n == 1 ? "" : "s"), Percent = percent.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture), Aria = n + " stars: " + percent.ToString("0.0") + " percent", Rounded = Math.Round(percent) };
            }).ToList();
            WriteReview = Me?.Role == "Learner" && Enrollment != null;
            var mine = Me == null ? null : Data.Feedback.FirstOrDefault(f => f.UserId == Me.Id && f.CourseId == C.Id);
            if (mine != null)
            {
                MyRating = mine.Rating.ToString();
                MyText = mine.Comment;
            }
            ReviewList = Reviews.Select(r =>
            {
                var author = Data.User(r.UserId);
                return new
                {
                    r.Id,
                    AvatarHtml = Ui.Avatar(author),
                    Author = author?.Name ?? "Former user",
                    Course = Data.Course(r.CourseId.Value)?.Title ?? "Archived course",
                    r.Rating,
                    Stars = new string('★', r.Rating) + new string('☆', 5 - r.Rating),
                    Date = Ui.Date(r.CreatedAt),
                    Text = r.Comment,
                    Replies = Data.FeedbackReplies.Where(x => x.ParentId == r.Id).Select(x =>
                    {
                        var who = Data.User(x.UserId);
                        return new { AvatarHtml = Ui.Avatar(who), Author = who?.Name ?? "Former user", Role = who?.Role ?? "Member", Date = Ui.Date(x.CreatedAt), To = author?.Name ?? "Former user", x.Text };
                    }).ToList(),
                    CanReply = CanReply(r)
                };
            }).ToList();
            NoReviews = Reviews.Count == 0;
        }

        /// <summary>Enrolled learners, the course's instructor, its program leader and admins may reply to a review.</summary>
        bool CanReply(FeedbackRow r)
        {
            if (Me == null) return false;
            if (Me.Role == "Admin") return true;
            if (Me.Role == "Learner") return Data.Enrollments.Any(x => x.UserId == Me.Id && x.CourseId == r.CourseId);
            var course = Data.Course(r.CourseId.Value);
            if (Me.Role == "Instructor") return course?.InstructorId == Me.Id;
            return course != null && Data.Programs.Any(p => p.Id == course.ProgramId && p.LeaderId == Me.Id);
        }

    }
}
