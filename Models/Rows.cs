using System;
using System.Collections.Generic;
using System.Linq;

namespace SkillUp.Models
{
    // One class per database row the API reads. Column names are in Snapshot.Load.

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

}
