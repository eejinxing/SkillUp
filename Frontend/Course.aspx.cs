using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend
{
    /// <summary>
    /// Course overview and enrolment (?id=). Visitors may open the first two activities as a preview;
    /// learners enrol and then open activities in order; staff who manage the course get a link to edit or review it.
    /// </summary>
    public partial class CoursePage : SkillPage
    {
        /// <summary>Visitors who are not signed in may look through this many opening activities of a course.</summary>
        public const int GuestPreviewCount = 2;

        public override string Route => "course";

        protected CourseRow C;
        protected List<LessonRow> Lessons;
        protected EnrollmentRow Enrollment;
        protected List<int> Completed = new List<int>();
        protected UserRow Instructor;
        protected string ProgramName, InstructorName, Rating, ReviewAverage;
        protected int TotalMinutes, Percent, LearnerCount;
        protected bool CanManage, AllReviews;
        protected List<FeedbackRow> Reviews;

        protected void Page_Load(object sender, EventArgs e)
        {
            var id = QueryInt("id");
            C = id == null ? null : Data.Course(id.Value);
            if (C == null || C.Status != "Published")
            {
                Unavailable.Visible = true;
                Detail.Visible = false;
                return;
            }
            Title = C.Title;
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

            Outcomes.DataSource = (C.Outcomes ?? "").Split('\n').Select(o => o.Trim()).Where(o => o.Length > 0);
            Outcomes.DataBind();
            BindCurriculum();
            NoLessons.Visible = Lessons.Count == 0;
            BindAction();

            // Course files: for enrolled learners and the staff who manage it, once there is a file or something to upload.
            Files.CourseId = C.Id;
            Files.Visible = (Enrollment != null || CanManage) && (CanManage || Data.Resources.Any(r => r.CourseId == C.Id));

            AllReviews = ReviewScope.SelectedValue == "all";
            BindReviews();
        }

        void BindCurriculum()
        {
            Curriculum.DataSource = Lessons.Select((lesson, i) =>
            {
                var state = LessonState(lesson, i);
                var url = "Activity?course=" + C.Id + "&lesson=" + lesson.Id;
                string action = "";
                if (state == "guest") action = Link(url, "Preview activity");
                else if (state == "done" || state == "next") action = Link(url, state == "done" ? "Review activity" : "Open activity");
                else if (state == "managed" && Me.Role == "Instructor") action = Link("CourseEditor?id=" + C.Id, "Edit in course editor");
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
            });
            Curriculum.DataBind();
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
            EnrolButton.Visible = false;
            if (Enrollment != null)
            {
                var next = Lessons.FirstOrDefault(l => !Completed.Contains(l.Id));
                ActionLink.Text = next != null ? "Continue learning" : "View certificate";
                ActionLink.NavigateUrl = next != null ? "Activity?course=" + C.Id + "&lesson=" + next.Id : "Certificates?id=" + C.Id;
            }
            else if (Me?.Role == "Learner")
            {
                ActionLink.Visible = false;
                EnrolButton.Visible = true;
            }
            else if (Me == null)
            {
                ActionLink.Text = "Sign in to enrol";
                ActionLink.NavigateUrl = "Login?return=" + HttpUtility.UrlEncode(Request.RawUrl);
            }
            else if (CanManage)
            {
                ActionLink.Text = "Manage course";
                ActionLink.NavigateUrl = Me.Role == "Instructor" ? "CourseEditor?id=" + C.Id : "CourseReview?id=" + C.Id;
            }
            else
            {
                ActionLink.Text = "Explore courses";
                ActionLink.NavigateUrl = "Catalog";
            }
        }

        void BindReviews()
        {
            Reviews = Data.Feedback.Where(f => f.CourseId != null && (AllReviews || f.CourseId == C.Id))
                .Where(f => VisibleCourses.Any(c => c.Id == f.CourseId)).OrderByDescending(f => f.CreatedAt).ToList();
            ReviewAverage = Reviews.Count == 0 ? "—" : Reviews.Average(f => f.Rating).ToString("0.0");
            RatingBars.DataSource = new[] { 5, 4, 3, 2, 1 }.Select(n =>
            {
                var percent = Reviews.Count == 0 ? 0 : 100.0 * Reviews.Count(r => r.Rating == n) / Reviews.Count;
                return new { Label = n + " star" + (n == 1 ? "" : "s"), Percent = percent.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture), Aria = n + " stars: " + percent.ToString("0.0") + " percent", Rounded = Math.Round(percent) };
            });
            RatingBars.DataBind();
            WriteReview.Visible = Me?.Role == "Learner" && Enrollment != null;
            var mine = Me == null ? null : Data.Feedback.FirstOrDefault(f => f.UserId == Me.Id && f.CourseId == C.Id);
            if (!IsPostBack && mine != null)
            {
                ReviewRating.SelectedValue = mine.Rating.ToString();
                ReviewText.Text = mine.Comment;
            }
            ReviewList.DataSource = Reviews.Select(r =>
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
            });
            ReviewList.DataBind();
            NoReviews.Visible = Reviews.Count == 0;
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

        protected void Enrol_Click(object sender, EventArgs e)
        {
            if (TryRun("enrol", new { courseId = C.Id.ToString() })) ReloadPage("You’re enrolled. Your first activity is ready.");
        }

        protected void SaveReview_Click(object sender, EventArgs e)
        {
            if (IsValid && TryRun("courseReview", new { courseId = C.Id.ToString(), rating = ReviewRating.SelectedValue, text = ReviewText.Text.Trim() })) ReloadPage("Review saved.");
        }

        protected void Reply_Command(object sender, CommandEventArgs e)
        {
            var text = ((TextBox)((Control)sender).NamingContainer.FindControl("ReplyText")).Text.Trim();
            if (TryRun("reply", new { id = (string)e.CommandArgument, kind = "review", text })) ReloadPage("Reply posted.");
        }

        protected void ReviewScope_Changed(object sender, EventArgs e)
        {
            // The scope is read in Page_Load; nothing else to do.
        }
    }
}
