using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace SKILL_UP.Backend
{
    // Enrolment, activity completion and grading, reviews, discussions, messages, live sessions and the AI companion.
    public sealed partial class Commands
    {
        static readonly Regex TagPattern = new Regex(@"#([\p{L}\p{N}_][\p{L}\p{N}_+#.-]{0,48})");

        void Touch(EnrollmentRow e) => db.Exec("UPDATE ENROLLMENT SET last_active = @p1 WHERE enrollment_id = @p0", e.Id, Now);

        /// <summary>Activities unlock in order: the previous one must be complete.</summary>
        void RequirePrevious(List<LessonRow> lessons, int index, EnrollmentRow e)
        {
            if (index > 0 && !m.CompletedLessons(e.UserId, e.CourseId).Contains(lessons[index - 1].Id)) Fail("Complete the previous activity first.");
        }

        object Enrol()
        {
            Must("Learner");
            var c = CourseFor(Str("courseId"));
            if (c.Status != "Published") Fail("Course is not open for enrolment.");
            if (m.Enrollments.Any(e => e.UserId == Me.Id && e.CourseId == c.Id)) Fail("Already enrolled.", 409);
            db.Exec(@"INSERT INTO ENROLLMENT (user_id, course_id, enrolled_by, enroll_date, status, progress_percent, last_active)
                      VALUES (@p0, @p1, @p0, @p2, N'In progress', 0, @p2)", Me.Id, c.Id, Now);
            RecalculateProgress(c.Id);
            return null;
        }

        object Complete()
        {
            CourseRow c;
            var e = Enrolled(Str("courseId"), out c);
            var lessons = m.LessonsOf(c.Id);
            int lessonId;
            var index = int.TryParse(Str("lessonId"), out lessonId) ? lessons.FindIndex(l => l.Id == lessonId) : -1;
            if (index < 0) Fail("Activity not found.");
            var lesson = lessons[index];
            RequirePrevious(lessons, index, e);
            if (lesson.Type == "quiz") Fail("Submit the quiz to complete this activity.");
            if (lesson.Type == "lab")
            {
                // A lab that is already passed is final: its answer cannot be submitted again.
                if (m.CompletedLessons(Me.Id, c.Id).Contains(lesson.Id)) Fail("This lab is already completed and its answer can no longer be changed.", 409);
                // Graded here, never in the browser. Every lab answer is recorded as a submission.
                var answer = (Str("answer") ?? "").Trim();
                var correct = string.Equals(answer, (lesson.Answer ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
                var submission = db.Insert("INSERT INTO LAB_SUBMISSION (user_id, lab_id, score, result_status, submitted_at) VALUES (@p0, @p1, @p2, @p3, @p4)",
                    Me.Id, lesson.Id, correct ? 100m : 0m, correct ? "Passed" : "Failed", Now);
                if (lesson.TaskId != null)
                    db.Exec("INSERT INTO TASK_ANSWER (submission_id, task_id, user_answer, is_correct) VALUES (@p0, @p1, @p2, @p3)", submission, lesson.TaskId, Limit(answer, 4000), correct);
                if (!correct) return new JObject { ["correct"] = false };
            }
            else if (!m.CompletedLessons(Me.Id, c.Id).Contains(lesson.Id))
                db.Exec("INSERT INTO LAB_SUBMISSION (user_id, lab_id, score, result_status, submitted_at) VALUES (@p0, @p1, NULL, N'Passed', @p2)", Me.Id, lesson.Id, Now);
            Touch(e);
            RecalculateProgress(c.Id);
            return new JObject { ["correct"] = true };
        }

        object QuizSubmit()
        {
            CourseRow c;
            var e = Enrolled(Str("courseId"), out c);
            var quiz = m.QuizFor(c.Id);
            if (quiz == null || quiz.Status != "Published" || quiz.Questions.Count == 0) Fail("Quiz is not published.");
            var lessons = m.LessonsOf(c.Id);
            int lessonId;
            var index = int.TryParse(Str("lessonId"), out lessonId) ? lessons.FindIndex(l => l.Id == lessonId) : -1;
            if (index < 0 || lessons[index].Type != "quiz") Fail("Quiz activity not found.");
            RequirePrevious(lessons, index, e);
            var questions = quiz.Questions;
            var list = d["answers"] as JArray;
            if (list == null || list.Count != questions.Count || list.Select((a, i) => a.Type != JTokenType.Integer || (int)a < 0 || (int)a >= questions[i].Options.Count).Any(bad => bad))
                Fail("Answer every question before submitting.");
            var answers = list.Select(a => (int)a).ToList();
            var correct = questions.Select((q, i) => q.AnswerIndex == answers[i]).ToList();
            var score = (int)Math.Round(correct.Count(x => x) * 100.0 / questions.Count, MidpointRounding.AwayFromZero);
            // The quiz page reports when the learner opened the quiz; the leaderboard ranks on the time taken (submitted_at - started_at).
            DateTime opened;
            var started = DateTime.TryParse(Str("startedAt"), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out opened) && opened <= Now && opened > Now.AddHours(-3) ? opened : Now;
            var attempt = db.Insert("INSERT INTO QUIZ_ATTEMPT (quiz_id, user_id, score, started_at, submitted_at) VALUES (@p0, @p1, @p2, @p3, @p4)", quiz.Id, Me.Id, (decimal)score, started, Now);
            for (int i = 0; i < questions.Count; i++)
                db.Exec("INSERT INTO ATTEMPT_ANSWER (attempt_id, question_id, selected_option_id, is_correct) VALUES (@p0, @p1, @p2, @p3)",
                    attempt, questions[i].Id, questions[i].Options[answers[i]].Id, correct[i]);
            // 70% passes the quiz activity, which unlocks the next one.
            if (score >= 70 && !m.CompletedLessons(Me.Id, c.Id).Contains(lessons[index].Id))
                db.Exec("INSERT INTO LAB_SUBMISSION (user_id, lab_id, score, result_status, submitted_at) VALUES (@p0, @p1, @p2, N'Passed', @p3)", Me.Id, lessons[index].Id, (decimal)score, Now);
            Touch(e);
            RecalculateProgress(c.Id);
            return new JObject
            {
                ["id"] = "quiz-" + attempt,
                ["userId"] = Me.Id.ToString(),
                ["courseId"] = c.Id.ToString(),
                ["lessonId"] = lessons[index].Id.ToString(),
                ["type"] = "quiz",
                ["answers"] = new JArray(answers),
                ["score"] = score,
                ["createdAt"] = Workspace.Iso(Now),
                ["wrongTopics"] = new JArray(questions.Where((q, i) => !correct[i]).Select(q => q.Topic ?? "")),
                // The answer key is only revealed after submission, never in the workspace.
                ["feedback"] = new JArray(questions.Select((q, i) => new JObject
                {
                    ["text"] = q.Text,
                    ["selectedText"] = q.Options[answers[i]].Text,
                    ["correctText"] = q.AnswerIndex >= 0 ? q.Options[q.AnswerIndex].Text : "",
                    ["correct"] = correct[i]
                }))
            };
        }

        object CourseReview()
        {
            CourseRow c;
            Enrolled(Str("courseId"), out c);
            var rating = Num("rating");
            if (rating == null || rating < 1 || rating > 5) Fail("Choose 1 to 5 stars.");
            var text = Text("text", "Review", 2000);
            var existing = m.Feedback.FirstOrDefault(f => f.UserId == Me.Id && f.CourseId == c.Id);
            if (existing != null)
                db.Exec("UPDATE WEBSITE_FEEDBACK SET rating = @p1, comment = @p2 WHERE feedback_id = @p0", existing.Id, (byte)rating.Value, text);
            else
                db.Exec("INSERT INTO WEBSITE_FEEDBACK (user_id, course_id, rating, comment, is_public, created_at) VALUES (@p0, @p1, @p2, @p3, 1, @p4)", Me.Id, c.Id, (byte)rating.Value, text, Now);
            return null;
        }

        object Feedback()
        {
            var text = Text("text", "Feedback", 2000);
            var rating = Num("rating");
            if (rating == null || rating < 1 || rating > 5) Fail("Choose 1 to 5 stars.");
            db.Exec("INSERT INTO WEBSITE_FEEDBACK (user_id, course_id, rating, comment, is_public, created_at) VALUES (@p0, NULL, @p1, @p2, 1, @p3)", Me.Id, (byte)rating.Value, text, Now);
            return null;
        }

        void SaveTags(int postId, string text)
        {
            foreach (var name in TagPattern.Matches(text).Cast<Match>().Select(x => x.Groups[1].Value).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var tag = db.Scalar("SELECT tag_id FROM TAG WHERE tag_name = @p0", name);
                var tagId = tag == null ? db.Insert("INSERT INTO TAG (tag_name) VALUES (@p0)", name) : (int)tag;
                db.Exec("INSERT INTO POST_TAG (post_id, tag_id) VALUES (@p0, @p1)", postId, tagId);
            }
        }

        object Post()
        {
            int courseId = ParseId(Str("courseId"), "Course not found.");
            CourseAccess(courseId);
            var text = Text("text", "Message", 4000);
            var id = db.Insert("INSERT INTO DISCUSSION_POST (user_id, course_id, content, status, created_at) VALUES (@p0, @p1, @p2, N'Visible', @p3)", Me.Id, courseId, text, Now);
            SaveTags(id, text);
            return null;
        }

        object Reply()
        {
            int id;
            int.TryParse(Str("id"), out id);
            var text = Text("text", "Reply", 2000);
            if (Str("kind") == "review")
            {
                var review = m.Feedback.FirstOrDefault(f => f.Id == id && f.CourseId != null);
                if (review == null) Fail("Discussion not found.");
                CourseAccess(review.CourseId);
                db.Exec("INSERT INTO FEEDBACK_REPLY (feedback_id, user_id, content, created_at) VALUES (@p0, @p1, @p2, @p3)", review.Id, Me.Id, text, Now);
                return null;
            }
            var post = m.Posts.FirstOrDefault(p => p.Id == id && p.ParentId == null && p.CourseId != null);
            if (post == null) Fail("Discussion not found.");
            CourseAccess(post.CourseId);
            var replyId = db.Insert("INSERT INTO DISCUSSION_POST (user_id, course_id, parent_post_id, content, status, created_at) VALUES (@p0, @p1, @p2, @p3, N'Visible', @p4)",
                Me.Id, post.CourseId, post.Id, text, Now);
            SaveTags(replyId, text);
            return null;
        }

        object Moderate()
        {
            int id;
            int.TryParse(Str("id"), out id);
            var post = m.Posts.FirstOrDefault(p => p.Id == id && p.CourseId != null);
            if (post == null) Fail("Post not found.");
            CourseFor(post.CourseId.ToString(), true);
            var reason = Text("reason", "Moderation reason", 500);
            var status = Str("status");
            if (status != "Visible" && status != "Hidden" && status != "Removed" && status != "Flagged") Fail("Invalid moderation status.");
            db.Exec("UPDATE DISCUSSION_POST SET status = @p1 WHERE post_id = @p0", post.Id, status);
            db.Exec("INSERT INTO MODERATION_LOG (post_id, moderator_id, action, reason, created_at) VALUES (@p0, @p1, @p2, @p3, @p4)", post.Id, Me.Id, status, reason, Now);
            Log("Discussion " + status, reason);
            return null;
        }

        object GlobalMessage()
        {
            var text = Text("text", "Message", 500);
            db.Exec("INSERT INTO DISCUSSION_POST (user_id, content, status, created_at) VALUES (@p0, @p1, N'Visible', @p2)", Me.Id, text, Now);
            return null;
        }

        object Message()
        {
            int to;
            if (!int.TryParse(Str("to"), out to) || to == Me.Id || !m.Users.Any(u => u.Id == to && u.Status == "Active")) Fail("Choose a recipient.");
            var text = Text("text", "Message", 4000);
            db.Exec("INSERT INTO CHAT_MESSAGE (sender_id, receiver_id, content, is_read, sent_at) VALUES (@p0, @p1, @p2, 0, @p3)", Me.Id, to, text, Now);
            return null;
        }

        object ReadMessages()
        {
            int from;
            if (int.TryParse(Str("from"), out from))
                db.Exec("UPDATE CHAT_MESSAGE SET is_read = 1 WHERE receiver_id = @p0 AND sender_id = @p1 AND is_read = 0", Me.Id, from);
            return null;
        }

        object SessionSave()
        {
            Must("Instructor");
            var c = CourseFor(Str("courseId"), true);
            var title = Text("title", "Title", 100);
            DateTime startsAt;
            // The form sends local Asia/Kuala_Lumpur time as YYYY-MM-DDTHH:mm; it is stored and returned as entered.
            if (!DateTime.TryParse(Str("startsAt"), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out startsAt))
                Fail("Choose a session time.");
            db.Exec("INSERT INTO VOICE_CHANNEL (course_id, created_by, channel_name, status, created_at, starts_at) VALUES (@p0, @p1, @p2, N'Open', @p3, @p4)",
                c.Id, Me.Id, title, Now, DateTime.SpecifyKind(startsAt, DateTimeKind.Unspecified));
            return null;
        }

        object SessionJoin()
        {
            int id;
            int.TryParse(Str("id"), out id);
            var session = m.Channels.FirstOrDefault(s => s.Id == id);
            if (session == null) Fail("Session not found.");
            CourseAccess(session.CourseId);
            db.Exec("UPDATE CHANNEL_PARTICIPANT SET left_at = @p2 WHERE channel_id = @p0 AND user_id = @p1 AND left_at IS NULL", session.Id, Me.Id, Now);
            if (!Bool("leave"))
                db.Exec("INSERT INTO CHANNEL_PARTICIPANT (channel_id, user_id, camera_enabled, joined_at) VALUES (@p0, @p1, 0, @p2)", session.Id, Me.Id, Now);
            return null;
        }

        /// <summary>
        /// Saves the question and the companion's reply: Gemini's (AiReply) or, with no API key, a rule-based sample. Each activity
        /// (lessonId) has its own conversation; without a lessonId it is the general one on the AI Assistant page.
        /// </summary>
        object Ai()
        {
            var prompt = Text("text", "Question", 4000);
            var lessonId = Companion.LessonOf(d);
            if (lessonId != null)
            {
                var lesson = m.Lessons.FirstOrDefault(l => l.Id == lessonId.Value);
                if (lesson == null) Fail("Activity not found.", 404);
                CourseAccess(lesson.CourseId);
            }
            var response = AiReply;
            if (response != null) { }
            else if (Regex.IsMatch(prompt, "aggregation|match|group", RegexOptions.IgnoreCase))
                response = "Try a small pipeline first: $match filters documents, $group summarises them, and $sort orders the result. Which stage does your question need?";
            else if (Regex.IsMatch(prompt, "progress|complete", RegexOptions.IgnoreCase))
                response = "Open My learning to find your next unlocked activity. Complete the reading and lab before taking the quiz. A score of 70% unlocks completion.";
            else
                response = "Break the problem into one small step. Review the course explanation, try an example, then compare the output with what you expected. Tell me the concept you want to practise.";
            var session = lessonId == null
                ? db.Scalar("SELECT TOP 1 session_id FROM AI_CHAT_SESSION WHERE user_id = @p0 AND lesson_id IS NULL ORDER BY session_id DESC", Me.Id)
                : db.Scalar("SELECT TOP 1 session_id FROM AI_CHAT_SESSION WHERE user_id = @p0 AND lesson_id = @p1 ORDER BY session_id DESC", Me.Id, lessonId.Value);
            var sessionId = session == null
                ? db.Insert("INSERT INTO AI_CHAT_SESSION (user_id, started_at, lesson_id) VALUES (@p0, @p1, @p2)", Me.Id, Now, lessonId.HasValue ? (object)lessonId.Value : null)
                : (int)session;
            db.Exec("INSERT INTO AI_CHAT_MESSAGE (session_id, sender_type, content, created_at) VALUES (@p0, N'User', @p1, @p2)", sessionId, prompt, Now);
            // The reply stays "unread" (its badge in the AI Assistant list) until the person's page shows it and says so (aiRead).
            db.Exec("INSERT INTO AI_CHAT_MESSAGE (session_id, sender_type, content, created_at, is_read) VALUES (@p0, N'AI', @p1, @p2, 0)", sessionId, response, Now);
            return new JObject { ["response"] = response };
        }

        /// <summary>The conversation (lessonId, or the general one) has been opened: its AI replies are read, so no badge shows for them.</summary>
        object AiRead()
        {
            var lessonId = Companion.LessonOf(d);
            if (lessonId == null)
                db.Exec("UPDATE AI_CHAT_MESSAGE SET is_read = 1 WHERE is_read = 0 AND session_id IN (SELECT session_id FROM AI_CHAT_SESSION WHERE user_id = @p0 AND lesson_id IS NULL)", Me.Id);
            else
                db.Exec("UPDATE AI_CHAT_MESSAGE SET is_read = 1 WHERE is_read = 0 AND session_id IN (SELECT session_id FROM AI_CHAT_SESSION WHERE user_id = @p0 AND lesson_id = @p1)", Me.Id, lessonId.Value);
            return null;
        }

        // ---- Admin: user accounts -------------------------------------------------------

        object UserSave()
        {
            Must("Admin");
            var name = (Str("name") ?? "").Trim();
            var email = (Str("email") ?? "").Trim();
            var username = (Str("username") ?? "").Trim();
            UserRow x = null;
            if (!string.IsNullOrEmpty(Str("id")))
            {
                int xid;
                x = int.TryParse(Str("id"), out xid) ? m.User(xid) : null;
                if (x == null) Fail("User not found.", 404);
            }
            UniqueUser(name, username, email, x?.Id);
            var role = Str("role");
            var status = Str("status");
            if (!Roles.Contains(role) || !Statuses.Contains(status)) Fail("Choose a valid role and status.");
            if (x != null && x.Id == Me.Id && (status != "Active" || role != "Admin")) Fail("You cannot remove your own administrator access.");
            // Courses and programs need an active owner: only their instructor or Program Leader can manage them, so the owner
            // keeps the role and stays active until an admin transfers them (staffTransfer, the Responsibilities panel).
            if (x != null)
            {
                var teaches = m.Courses.Count(c => c.InstructorId == x.Id);
                var leads = m.Programs.Count(p => p.LeaderId == x.Id);
                if (teaches > 0 && (role != x.Role && role != "Instructor" || status != "Active" && x.Status == "Active"))
                    Fail(x.Name + " still teaches " + (teaches == 1 ? "1 course" : teaches + " courses") + ". Transfer them to another instructor under Responsibilities before changing the role or account status.");
                if (leads > 0 && (role != x.Role && role != "Program Leader" || status != "Active" && x.Status == "Active"))
                    Fail(x.Name + " still leads " + (leads == 1 ? "1 program" : leads + " programs") + ". Transfer them to another Program Leader under Responsibilities before changing the role or account status.");
            }
            var parts = SplitName(name);
            var bio = Limit(Str("bio") ?? "", 1000);
            int id;
            if (x != null)
            {
                id = x.Id;
                db.Exec("UPDATE [USER] SET role_id = @p1, first_name = @p2, last_name = @p3, email = @p4, username = @p5, account_status = @p6, bio = @p7 WHERE user_id = @p0",
                    id, RoleId(role), parts[0], parts[1], email.ToLowerInvariant(), username, status, bio);
            }
            else
            {
                var password = Str("password") ?? "";
                var problem = Passwords.Problem(password);
                if (problem != null) Fail(problem);
                id = db.Insert(@"INSERT INTO [USER] (role_id, first_name, last_name, username, password, email, bio, preferred_language, account_status, theme, created_at)
                                 VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, N'en', @p7, N'dark', @p8)",
                    RoleId(role), parts[0], parts[1], username, Passwords.Hash(password), email.ToLowerInvariant(), bio, status, Now);
            }
            if (role == "Instructor")
            {
                // Approving a pending instructor (status Active) also verifies their instructor profile.
                db.Exec(@"IF NOT EXISTS (SELECT 1 FROM INSTRUCTOR_PROFILE WHERE user_id = @p0)
                              INSERT INTO INSTRUCTOR_PROFILE (user_id, verification_status) VALUES (@p0, N'Pending');
                          IF @p1 = N'Active'
                              UPDATE INSTRUCTOR_PROFILE SET verification_status = N'Approved', verified_by = @p2, verified_at = @p3
                              WHERE user_id = @p0 AND verification_status <> N'Approved';", id, status, Me.Id, Now);
            }
            Log("User account saved", email.ToLowerInvariant(), "User", id);
            return new JObject { ["id"] = id.ToString() };
        }

        object UserDelete()
        {
            Must("Admin");
            int id;
            int.TryParse(Str("id"), out id);
            if (id == Me.Id) Fail("You cannot delete your own account.");
            var x = m.User(id);
            if (x == null) Fail("User not found.");
            if (m.Courses.Any(c => c.InstructorId == id) || m.Programs.Any(p => p.LeaderId == id)) Fail("Transfer this user’s courses or programs under Responsibilities before deleting the account.");
            // Content they authored as staff moves to the admin; their own learning and messages are removed.
            db.Exec(@"DECLARE @u INT = @p0, @admin INT = @p1;
                      UPDATE COURSE SET created_by = @admin WHERE created_by = @u;
                      UPDATE QUIZ SET created_by = @admin WHERE created_by = @u;
                      UPDATE LEARNING_PATH SET created_by = @admin WHERE created_by = @u;
                      UPDATE VOICE_CHANNEL SET created_by = @admin WHERE created_by = @u;
                      UPDATE ANNOUNCEMENT SET posted_by = @admin WHERE posted_by = @u;
                      UPDATE COURSE_RESOURCE SET uploaded_by = @admin WHERE uploaded_by = @u;
                      UPDATE COURSE_INSTRUCTOR SET assigned_by = NULL WHERE assigned_by = @u;
                      UPDATE ENROLLMENT SET enrolled_by = NULL WHERE enrolled_by = @u;
                      UPDATE INSTRUCTOR_PROFILE SET verified_by = NULL WHERE verified_by = @u;
                      UPDATE ACTIVITY_LOG SET user_id = NULL WHERE user_id = @u;
                      UPDATE ENQUIRY SET user_id = NULL WHERE user_id = @u;
                      UPDATE CONTENT_REVIEW SET reviewer_id = @admin WHERE reviewer_id = @u;
                      UPDATE MODERATION_LOG SET moderator_id = @admin WHERE moderator_id = @u;
                      DECLARE @posts TABLE (post_id INT PRIMARY KEY);
                      INSERT INTO @posts SELECT post_id FROM DISCUSSION_POST WHERE user_id = @u;
                      INSERT INTO @posts SELECT post_id FROM DISCUSSION_POST WHERE parent_post_id IN (SELECT post_id FROM @posts) AND post_id NOT IN (SELECT post_id FROM @posts);
                      DELETE FROM MODERATION_LOG WHERE post_id IN (SELECT post_id FROM @posts);
                      DELETE FROM DISCUSSION_POST WHERE post_id IN (SELECT post_id FROM @posts) AND parent_post_id IS NOT NULL;
                      DELETE FROM DISCUSSION_POST WHERE post_id IN (SELECT post_id FROM @posts);
                      DELETE FROM FEEDBACK_REPLY WHERE user_id = @u;
                      DELETE FROM WEBSITE_FEEDBACK WHERE user_id = @u;
                      DELETE FROM ANNOUNCEMENT_COMMENT WHERE user_id = @u;
                      DELETE FROM CHAT_MESSAGE WHERE sender_id = @u OR receiver_id = @u;
                      DELETE FROM CHANNEL_PARTICIPANT WHERE user_id = @u;
                      DELETE FROM QUIZ_ATTEMPT WHERE user_id = @u;
                      DELETE FROM LAB_SUBMISSION WHERE user_id = @u;
                      DELETE FROM ENROLLMENT WHERE user_id = @u;
                      DELETE FROM INSTRUCTOR_PROFILE WHERE user_id = @u;
                      DELETE FROM [USER] WHERE user_id = @u;", id, Me.Id);
            Log("User deleted", x.Name, "User", id);
            return null;
        }

        /// <summary>
        /// Admin: moves everything a staff member is responsible for to another active account of the same role, so their role
        /// can change or the account can be deactivated or deleted. what = "courses" (the instructor of each course) or
        /// "programs" (the Program Leader of each program, who also reviews its course requests).
        /// </summary>
        object StaffTransfer()
        {
            Must("Admin");
            int fromId, toId;
            int.TryParse(Str("fromId"), out fromId);
            int.TryParse(Str("toId"), out toId);
            var from = m.User(fromId);
            if (from == null) Fail("User not found.", 404);
            var what = Str("what");
            if (what != "courses" && what != "programs") Fail("Choose what to transfer.");
            var role = what == "courses" ? "Instructor" : "Program Leader";
            var to = m.User(toId);
            if (to == null || to.Id == from.Id || to.Role != role || to.Status != "Active")
                Fail("Choose another active " + (what == "courses" ? "instructor." : "Program Leader."));
            int moved;
            if (what == "courses")
            {
                var courses = m.Courses.Where(c => c.InstructorId == from.Id).ToList();
                if (courses.Count == 0) Fail(from.Name + " has no courses to transfer.");
                foreach (var c in courses)
                {
                    db.Exec("DELETE FROM COURSE_INSTRUCTOR WHERE course_id = @p0", c.Id);
                    db.Exec("INSERT INTO COURSE_INSTRUCTOR (course_id, instructor_id, assigned_by, assigned_at) VALUES (@p0, @p1, @p2, @p3)", c.Id, to.Id, Me.Id, Now);
                }
                moved = courses.Count;
            }
            else
            {
                moved = m.Programs.Count(p => p.LeaderId == from.Id);
                if (moved == 0) Fail(from.Name + " has no programs to transfer.");
                db.Exec("UPDATE PROGRAM SET program_leader_id = @p1 WHERE program_leader_id = @p0", from.Id, to.Id);
            }
            Log(what == "courses" ? "Courses transferred" : "Programs transferred",
                moved + " " + (what == "courses" ? (moved == 1 ? "course" : "courses") : (moved == 1 ? "program" : "programs")) + " from " + from.Name + " to " + to.Name, "User", from.Id);
            return new JObject { ["moved"] = moved, ["to"] = to.Name };
        }
    }
}
