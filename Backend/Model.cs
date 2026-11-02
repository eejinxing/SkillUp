using System;
using System.Collections.Generic;
using System.Linq;

namespace SKILL_UP.Backend
{
    // One class per database row the API reads. Column names are in Model.Load.

    public sealed class UserRow
    {
        public int Id, RoleId;
        public string Role, FirstName, LastName, Username, Password, Email, Avatar, Bio, Language, Status, Location, Theme, Preferences;
        public DateTime CreatedAt;
        public DateTime? LastLogin;
        public string Name => ((FirstName ?? "") + " " + (LastName ?? "")).Trim();
    }

    public sealed class ProgramRow
    {
        public int Id;
        public int? LeaderId;
        public string Name, Description, Status;
        public bool Published => Status == "Published";
    }

    public sealed class CourseRow
    {
        public int Id, ProgramId, CreatedBy, Duration;
        public string Title, Description, Difficulty, Outcomes, Thumbnail, Visibility, Approval, Color, Icon;
        public DateTime? StartDate, Deadline;
        public int? InstructorId;
        /// <summary>The status the UI uses: Hidden, or the approval status (Draft, Pending review, Published, ...).</summary>
        public string Status => Visibility == "Hidden" ? "Hidden" : Approval;
    }

    /// <summary>A LAB row: one ordered learning activity of a course (article, lab or quiz).</summary>
    public sealed class LessonRow
    {
        public int Id, CourseId, Sequence, Minutes;
        public string Title, Body, Type, Status;
        public bool Preview;
        public int? TaskId;
        public string Answer;
        /// <summary>"none" or "javascript": whether the activity page shows the in-browser JavaScript terminal.</summary>
        public string Workspace = "none";
        /// <summary>The terminal's starting code; for a lab without a terminal, the code the learner is asked to complete.</summary>
        public string StarterCode;
    }

    public sealed class QuizRow
    {
        public int Id, CourseId;
        public int? TimeLimit;
        public string Status;
        public List<QuestionRow> Questions = new List<QuestionRow>();
    }

    public sealed class QuestionRow
    {
        public int Id, QuizId, Sequence;
        public string Text, Topic;
        public List<OptionRow> Options = new List<OptionRow>();
        public int AnswerIndex => Options.FindIndex(o => o.Correct);
    }

    public sealed class OptionRow
    {
        public int Id, QuestionId;
        public string Text;
        public bool Correct;
    }

    public sealed class EnrollmentRow
    {
        public int Id, UserId, CourseId;
        public DateTime EnrolledAt;
        public DateTime? LastActive;
        /// <summary>ENROLLMENT.completed_at: when the last activity of the course was passed.</summary>
        public DateTime? CompletedAt;
    }

    /// <summary>LAB_SUBMISSION: a Passed submission marks the activity complete; lab-type rows are also lab attempts.</summary>
    public sealed class SubmissionRow
    {
        public int Id, UserId, LabId;
        public decimal? Score;
        public string Result;
        public DateTime SubmittedAt;
    }

    public sealed class QuizAttemptRow
    {
        public int Id, QuizId, UserId;
        public decimal? Score;
        public DateTime CreatedAt;
        /// <summary>QUIZ_ATTEMPT.started_at: when the learner opened the quiz (equal to CreatedAt for older attempts that did not record it).</summary>
        public DateTime? StartedAt;
        public List<AttemptAnswerRow> Answers = new List<AttemptAnswerRow>();
    }

    public sealed class AttemptAnswerRow
    {
        public int AttemptId, QuestionId;
        public int? OptionId;
        public bool Correct;
    }

    /// <summary>WEBSITE_FEEDBACK: a course review when CourseId is set, otherwise platform feedback.</summary>
    public sealed class FeedbackRow
    {
        public int Id, UserId, Rating;
        public int? CourseId;
        public string Comment;
        public bool Public;
        public DateTime CreatedAt;
    }

    public sealed class ReplyRow
    {
        public int Id, ParentId, UserId;
        public string Text;
        public DateTime CreatedAt;
    }

    /// <summary>DISCUSSION_POST: a course post, a reply (ParentId) or a community message (no course).</summary>
    public sealed class PostRow
    {
        public int Id, UserId;
        public int? CourseId, LabId, ParentId;
        public string Content, Status, Reason;
        public DateTime CreatedAt;
    }

    public sealed class AnnouncementRow
    {
        public int Id, PostedBy;
        public int? CourseId, ProgramId;
        public string Title, Message;
        public DateTime CreatedAt;
    }

    public sealed class ChatRow
    {
        public int Id, SenderId, ReceiverId;
        public string Content;
        public bool Read;
        public DateTime SentAt;
    }

    public sealed class AiMessageRow
    {
        public int Id, UserId;
        /// <summary>The activity this conversation belongs to; null for the general conversation on the AI Assistant page.</summary>
        public int? LessonId;
        /// <summary>False for an AI reply the person has not seen yet (their badge shows until they open the conversation).</summary>
        public bool Read;
        public string Sender, Content;
        public DateTime CreatedAt;
    }

    /// <summary>VOICE_CHANNEL: a scheduled live session for a course.</summary>
    public sealed class ChannelRow
    {
        public int Id, CourseId, CreatedBy;
        public string Name, Status;
        public DateTime? StartsAt;
        public List<int> Attendees = new List<int>();
    }

    public sealed class ResourceRow
    {
        public int Id, CourseId;
        public string Name, Size, DataUrl;
        public DateTime CreatedAt;
    }

    public sealed class RequestRow
    {
        public int Id, CourseId;
        public int? ProgramId, RequesterId;
        public string Kind, CourseTitle, Status, Reason;
        public DateTime CreatedAt;
        public List<RequestEventRow> Events = new List<RequestEventRow>();
    }

    public sealed class RequestEventRow
    {
        public int Id, RequestId;
        public int? ActorId;
        public string Action, Comment;
        public DateTime CreatedAt;
    }

    public sealed class AuditRow
    {
        public int Id;
        public int? UserId, TargetId;
        public string Action, TargetType, Detail;
        public DateTime CreatedAt;
    }

    public sealed class EnquiryRow
    {
        public int Id;
        public int? UserId;
        public string Name, Email, Subject, Message, Status;
        public DateTime CreatedAt;
    }

    /// <summary>
    /// Everything the API reads, loaded in one go. The dataset of a class project is small, so commands
    /// validate against this snapshot and the workspace endpoint filters it per role.
    /// </summary>
    public sealed class Model
    {
        public List<UserRow> Users;
        public List<ProgramRow> Programs;
        public Dictionary<int, List<int>> PathCourses = new Dictionary<int, List<int>>();
        public List<CourseRow> Courses;
        public List<LessonRow> Lessons;
        public List<QuizRow> Quizzes;
        public List<EnrollmentRow> Enrollments;
        public List<SubmissionRow> Submissions;
        public List<QuizAttemptRow> QuizAttempts;
        public List<FeedbackRow> Feedback;
        public List<ReplyRow> FeedbackReplies;
        public List<PostRow> Posts;
        public List<AnnouncementRow> Announcements;
        public List<ReplyRow> AnnouncementComments;
        /// <summary>"userId:announcementId" for each announcement a person has opened (ANNOUNCEMENT_READ).</summary>
        public HashSet<string> AnnouncementReads;
        public List<ChatRow> Messages;
        public List<AiMessageRow> AiMessages;
        public List<ChannelRow> Channels;
        public List<ResourceRow> Resources;
        public List<RequestRow> Requests;
        public List<AuditRow> Audit;
        public List<EnquiryRow> Enquiries;

        public UserRow User(int? id) => id == null ? null : Users.FirstOrDefault(u => u.Id == id);
        public bool HasRead(int userId, int announcementId) => AnnouncementReads.Contains(userId + ":" + announcementId);
        public CourseRow Course(int id) => Courses.FirstOrDefault(c => c.Id == id);
        public ProgramRow Program(int id) => Programs.FirstOrDefault(p => p.Id == id);
        public QuizRow QuizFor(int courseId) => Quizzes.FirstOrDefault(q => q.CourseId == courseId);
        public List<LessonRow> LessonsOf(int courseId) => Lessons.Where(l => l.CourseId == courseId).OrderBy(l => l.Sequence).ThenBy(l => l.Id).ToList();

        public List<int> PathOf(int programId)
        {
            List<int> ids;
            return PathCourses.TryGetValue(programId, out ids) ? ids : new List<int>();
        }

        /// <summary>IDs of the course's activities this learner has completed (a Passed submission exists).</summary>
        public List<int> CompletedLessons(int userId, int courseId)
        {
            var lessons = LessonsOf(courseId);
            var passed = new HashSet<int>(Submissions.Where(s => s.UserId == userId && s.Result == "Passed").Select(s => s.LabId));
            return lessons.Where(l => passed.Contains(l.Id)).Select(l => l.Id).ToList();
        }

        public static Model Load(Db db)
        {
            var m = new Model();
            m.Users = db.Query(@"SELECT u.*, r.role_name FROM [USER] u JOIN ROLE r ON r.role_id = u.role_id ORDER BY u.user_id", r => new UserRow
            {
                Id = r.Int("user_id"), RoleId = r.Int("role_id"), Role = r.Str("role_name"), FirstName = r.Str("first_name"), LastName = r.Str("last_name"),
                Username = r.Str("username"), Password = r.Str("password"), Email = r.Str("email"), Avatar = r.Str("profile_picture"), Bio = r.Str("bio"),
                Language = r.Str("preferred_language"), Status = r.Str("account_status"), Location = r.Str("location"), Theme = r.Str("theme"),
                Preferences = r.Str("preferences"), CreatedAt = r.Date("created_at"), LastLogin = r.DateN("last_login")
            });
            m.Programs = db.Query("SELECT * FROM PROGRAM ORDER BY program_id", r => new ProgramRow
            {
                Id = r.Int("program_id"), LeaderId = r.IntN("program_leader_id"), Name = r.Str("program_name"), Description = r.Str("description"), Status = r.Str("status")
            });
            foreach (var row in db.Query(@"SELECT p.program_id, c.course_id FROM LEARNING_PATH_COURSE c JOIN LEARNING_PATH p ON p.path_id = c.path_id
                                           ORDER BY p.program_id, c.sequence_no", r => new { Program = r.Int("program_id"), Course = r.Int("course_id") }))
            {
                if (!m.PathCourses.ContainsKey(row.Program)) m.PathCourses[row.Program] = new List<int>();
                if (!m.PathCourses[row.Program].Contains(row.Course)) m.PathCourses[row.Program].Add(row.Course);
            }
            m.Courses = db.Query(@"SELECT c.*, (SELECT TOP 1 instructor_id FROM COURSE_INSTRUCTOR i WHERE i.course_id = c.course_id ORDER BY i.course_instructor_id) AS instructor_id
                                   FROM COURSE c ORDER BY c.course_id", r => new CourseRow
            {
                Id = r.Int("course_id"), ProgramId = r.Int("program_id"), CreatedBy = r.Int("created_by"), Title = r.Str("title"), Description = r.Str("description"),
                Difficulty = r.Str("difficulty_level"), Outcomes = r.Str("skill_gained"), Thumbnail = r.Str("thumbnail"), Visibility = r.Str("visibility_status"),
                Approval = r.Str("approval_status"), StartDate = r.DateN("start_date"), Deadline = r.DateN("deadline"), Duration = r.Int("duration_weeks"),
                Color = r.Str("color"), Icon = r.Str("icon"), InstructorId = r.IntN("instructor_id")
            });
            m.Lessons = db.Query(@"SELECT l.*, t.task_id, t.correct_answer FROM LAB l
                                   OUTER APPLY (SELECT TOP 1 task_id, correct_answer FROM LAB_TASK WHERE lab_id = l.lab_id ORDER BY sequence_no, task_id) t", r => new LessonRow
            {
                Id = r.Int("lab_id"), CourseId = r.Int("course_id"), Sequence = r.Int("sequence_no"), Minutes = r.Int("minutes"), Title = r.Str("title"),
                Body = r.Str("article_content"), Type = r.Str("lesson_type"), Status = r.Str("status"), Preview = r.Bool("is_preview"),
                TaskId = r.IntN("task_id"), Answer = r.Str("correct_answer"), Workspace = r.Str("workspace") ?? "none", StarterCode = r.Str("starter_code")
            });
            m.Quizzes = db.Query("SELECT * FROM QUIZ ORDER BY quiz_id", r => new QuizRow
            {
                Id = r.Int("quiz_id"), CourseId = r.Int("course_id"), TimeLimit = r.IntN("time_limit_minutes"), Status = r.Str("status")
            });
            var questions = db.Query("SELECT * FROM QUIZ_QUESTION ORDER BY quiz_id, sequence_no, question_id", r => new QuestionRow
            {
                Id = r.Int("question_id"), QuizId = r.Int("quiz_id"), Sequence = r.Int("sequence_no"), Text = r.Str("question_text"), Topic = r.Str("topic")
            });
            var options = db.Query("SELECT * FROM QUESTION_OPTION ORDER BY question_id, option_id", r => new OptionRow
            {
                Id = r.Int("option_id"), QuestionId = r.Int("question_id"), Text = r.Str("option_text"), Correct = r.Bool("is_correct")
            });
            foreach (var q in questions) q.Options = options.Where(o => o.QuestionId == q.Id).ToList();
            foreach (var quiz in m.Quizzes) quiz.Questions = questions.Where(q => q.QuizId == quiz.Id).ToList();

            m.Enrollments = db.Query("SELECT * FROM ENROLLMENT ORDER BY enrollment_id", r => new EnrollmentRow
            {
                Id = r.Int("enrollment_id"), UserId = r.Int("user_id"), CourseId = r.Int("course_id"), EnrolledAt = r.Date("enroll_date"), LastActive = r.DateN("last_active"), CompletedAt = r.DateN("completed_at")
            });
            m.Submissions = db.Query("SELECT * FROM LAB_SUBMISSION ORDER BY submission_id", r => new SubmissionRow
            {
                Id = r.Int("submission_id"), UserId = r.Int("user_id"), LabId = r.Int("lab_id"), Score = r.DecN("score"), Result = r.Str("result_status"), SubmittedAt = r.Date("submitted_at")
            });
            m.QuizAttempts = db.Query("SELECT * FROM QUIZ_ATTEMPT WHERE submitted_at IS NOT NULL ORDER BY attempt_id", r => new QuizAttemptRow
            {
                Id = r.Int("attempt_id"), QuizId = r.Int("quiz_id"), UserId = r.Int("user_id"), Score = r.DecN("score"), CreatedAt = r.Date("submitted_at"), StartedAt = r.DateN("started_at")
            });
            var answers = db.Query("SELECT * FROM ATTEMPT_ANSWER ORDER BY attempt_id, attempt_answer_id", r => new AttemptAnswerRow
            {
                AttemptId = r.Int("attempt_id"), QuestionId = r.Int("question_id"), OptionId = r.IntN("selected_option_id"), Correct = r.Bool("is_correct")
            }).ToLookup(a => a.AttemptId);
            foreach (var attempt in m.QuizAttempts) attempt.Answers = answers[attempt.Id].ToList();

            m.Feedback = db.Query("SELECT * FROM WEBSITE_FEEDBACK ORDER BY feedback_id", r => new FeedbackRow
            {
                Id = r.Int("feedback_id"), UserId = r.Int("user_id"), CourseId = r.IntN("course_id"), Rating = r.Int("rating"), Comment = r.Str("comment"),
                Public = r.Bool("is_public"), CreatedAt = r.Date("created_at")
            });
            m.FeedbackReplies = db.Query("SELECT * FROM FEEDBACK_REPLY ORDER BY reply_id", r => new ReplyRow
            {
                Id = r.Int("reply_id"), ParentId = r.Int("feedback_id"), UserId = r.Int("user_id"), Text = r.Str("content"), CreatedAt = r.Date("created_at")
            });
            m.Posts = db.Query(@"SELECT p.*, (SELECT TOP 1 reason FROM MODERATION_LOG l WHERE l.post_id = p.post_id ORDER BY l.moderation_id DESC) AS reason
                                 FROM DISCUSSION_POST p ORDER BY p.post_id", r => new PostRow
            {
                Id = r.Int("post_id"), UserId = r.Int("user_id"), CourseId = r.IntN("course_id"), LabId = r.IntN("lab_id"), ParentId = r.IntN("parent_post_id"),
                Content = r.Str("content"), Status = r.Str("status"), Reason = r.Str("reason"), CreatedAt = r.Date("created_at")
            });
            m.Announcements = db.Query("SELECT * FROM ANNOUNCEMENT ORDER BY created_at DESC, announcement_id DESC", r => new AnnouncementRow
            {
                Id = r.Int("announcement_id"), PostedBy = r.Int("posted_by"), CourseId = r.IntN("course_id"), ProgramId = r.IntN("program_id"),
                Title = r.Str("title"), Message = r.Str("message"), CreatedAt = r.Date("created_at")
            });
            m.AnnouncementComments = db.Query("SELECT * FROM ANNOUNCEMENT_COMMENT ORDER BY comment_id", r => new ReplyRow
            {
                Id = r.Int("comment_id"), ParentId = r.Int("announcement_id"), UserId = r.Int("user_id"), Text = r.Str("content"), CreatedAt = r.Date("created_at")
            });
            m.AnnouncementReads = new HashSet<string>(db.Query("SELECT user_id, announcement_id FROM ANNOUNCEMENT_READ", r => r.Int("user_id") + ":" + r.Int("announcement_id")));
            m.Messages = db.Query("SELECT * FROM CHAT_MESSAGE ORDER BY sent_at, message_id", r => new ChatRow
            {
                Id = r.Int("message_id"), SenderId = r.Int("sender_id"), ReceiverId = r.Int("receiver_id"), Content = r.Str("content"), Read = r.Bool("is_read"), SentAt = r.Date("sent_at")
            });
            m.AiMessages = db.Query(@"SELECT m.*, s.user_id, s.lesson_id FROM AI_CHAT_MESSAGE m JOIN AI_CHAT_SESSION s ON s.session_id = m.session_id
                                      ORDER BY m.ai_message_id", r => new AiMessageRow
            {
                Id = r.Int("ai_message_id"), UserId = r.Int("user_id"), LessonId = r.IntN("lesson_id"), Read = r.Bool("is_read"), Sender = r.Str("sender_type"), Content = r.Str("content"), CreatedAt = r.Date("created_at")
            });
            m.Channels = db.Query("SELECT * FROM VOICE_CHANNEL ORDER BY channel_id", r => new ChannelRow
            {
                Id = r.Int("channel_id"), CourseId = r.Int("course_id"), CreatedBy = r.Int("created_by"), Name = r.Str("channel_name"), Status = r.Str("status"),
                StartsAt = r.IsDBNull(r.GetOrdinal("starts_at")) ? (DateTime?)null : (DateTime)r["starts_at"]
            });
            var attendees = db.Query("SELECT channel_id, user_id FROM CHANNEL_PARTICIPANT WHERE left_at IS NULL ORDER BY joined_at, participant_id",
                r => new { Channel = r.Int("channel_id"), User = r.Int("user_id") }).ToLookup(a => a.Channel);
            foreach (var channel in m.Channels) channel.Attendees = attendees[channel.Id].Select(a => a.User).Distinct().ToList();

            m.Resources = db.Query("SELECT * FROM COURSE_RESOURCE ORDER BY resource_id", r => new ResourceRow
            {
                Id = r.Int("resource_id"), CourseId = r.Int("course_id"), Name = r.Str("file_name"), Size = r.Str("size_label"), DataUrl = r.Str("data_url"), CreatedAt = r.Date("created_at")
            });
            m.Requests = db.Query("SELECT * FROM COURSE_REQUEST ORDER BY request_id", r => new RequestRow
            {
                Id = r.Int("request_id"), Kind = r.Str("kind"), CourseId = r.Int("course_id"), CourseTitle = r.Str("course_title"), ProgramId = r.IntN("program_id"),
                RequesterId = r.IntN("requester_id"), Status = r.Str("status"), Reason = r.Str("reason"), CreatedAt = r.Date("created_at")
            });
            var events = db.Query("SELECT * FROM COURSE_REQUEST_EVENT ORDER BY created_at, event_id", r => new RequestEventRow
            {
                Id = r.Int("event_id"), RequestId = r.Int("request_id"), ActorId = r.IntN("actor_id"), Action = r.Str("action"), Comment = r.Str("comment"), CreatedAt = r.Date("created_at")
            }).ToLookup(e => e.RequestId);
            foreach (var request in m.Requests) request.Events = events[request.Id].ToList();

            m.Audit = db.Query("SELECT TOP 1000 * FROM ACTIVITY_LOG ORDER BY created_at DESC, log_id DESC", r => new AuditRow
            {
                Id = r.Int("log_id"), UserId = r.IntN("user_id"), Action = r.Str("action"), TargetType = r.Str("target_type"), TargetId = r.IntN("target_id"),
                Detail = r.Str("detail"), CreatedAt = r.Date("created_at")
            });
            m.Enquiries = db.Query("SELECT * FROM ENQUIRY ORDER BY enquiry_id", r => new EnquiryRow
            {
                Id = r.Int("enquiry_id"), UserId = r.IntN("user_id"), Name = r.Str("name"), Email = r.Str("email"), Subject = r.Str("subject"), Message = r.Str("message"),
                Status = r.Str("status"), CreatedAt = r.Date("submitted_at")
            });
            return m;
        }
    }
}
