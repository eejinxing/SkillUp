using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace SKILL_UP.Backend
{
    // Course authoring, the Create/Delete request workflow, programs and learning paths, assignment, announcements.
    public sealed partial class Commands
    {
        static readonly string[] Difficulties = { "Beginner", "Intermediate", "Advanced" };
        static readonly Regex ResourcePattern = new Regex(@"^data:(application/pdf|text/plain|image/(png|jpeg|webp));base64,");

        /// <summary>Any content edit sends the course back to Draft so it is reviewed again.</summary>
        void SetCourseDraft(int courseId) =>
            db.Exec("UPDATE COURSE SET approval_status = N'Draft', visibility_status = N'Visible', updated_at = @p1 WHERE course_id = @p0", courseId, Now);

        void RejectPendingReview(CourseRow c, string message)
        {
            if (c.Status == "Pending review") Fail(message);
        }

        /// <summary>Keeps ENROLLMENT.progress_percent / status / completed_at in line with Passed submissions.</summary>
        void RecalculateProgress(int courseId)
        {
            db.Exec(@"UPDATE e SET
                        progress_percent = CASE WHEN t.total = 0 THEN 0 ELSE CAST(ROUND(100.0 * ISNULL(x.done, 0) / t.total, 2) AS DECIMAL(5,2)) END,
                        status = CASE WHEN e.status = N'Dropped' THEN e.status WHEN t.total > 0 AND ISNULL(x.done, 0) >= t.total THEN N'Completed' ELSE N'In progress' END,
                        completed_at = CASE WHEN t.total > 0 AND ISNULL(x.done, 0) >= t.total THEN ISNULL(e.completed_at, @p1) ELSE NULL END
                      FROM ENROLLMENT e
                      CROSS APPLY (SELECT COUNT(*) AS total FROM LAB WHERE course_id = e.course_id) t
                      OUTER APPLY (SELECT COUNT(DISTINCT s.lab_id) AS done FROM LAB_SUBMISSION s JOIN LAB l ON l.lab_id = s.lab_id
                                   WHERE l.course_id = e.course_id AND s.user_id = e.user_id AND s.result_status = N'Passed') x
                      WHERE e.course_id = @p0", courseId, Now);
        }

        // ---- learning paths: one LEARNING_PATH per program, its ordered courses are program.courseIds --------

        int PathId(int programId)
        {
            var id = db.Scalar("SELECT TOP 1 path_id FROM LEARNING_PATH WHERE program_id = @p0 ORDER BY path_id", programId);
            if (id != null) return (int)id;
            var p = m.Program(programId);
            return db.Insert("INSERT INTO LEARNING_PATH (program_id, created_by, path_name, created_at) VALUES (@p0, @p1, @p2, @p3)",
                programId, p?.LeaderId ?? Me.Id, p?.Name ?? "Learning path", Now);
        }

        void SetPath(int programId, IList<int> courseIds)
        {
            var path = PathId(programId);
            db.Exec("DELETE FROM LEARNING_PATH_COURSE WHERE path_id = @p0", path);
            for (int i = 0; i < courseIds.Count; i++)
                db.Exec("INSERT INTO LEARNING_PATH_COURSE (path_id, course_id, sequence_no, is_starting_course) VALUES (@p0, @p1, @p2, @p3)", path, courseIds[i], i + 1, i == 0);
        }

        void RemoveFromPaths(int courseId) => db.Exec("DELETE FROM LEARNING_PATH_COURSE WHERE course_id = @p0", courseId);

        void AppendToPath(int programId, int courseId)
        {
            var ids = db.Query(@"SELECT c.course_id FROM LEARNING_PATH_COURSE c WHERE c.path_id = @p0 ORDER BY c.sequence_no", r => r.Int("course_id"), PathId(programId));
            if (ids.Contains(courseId)) return;
            ids.Add(courseId);
            SetPath(programId, ids);
        }

        // ---- course requests -------------------------------------------------------------

        RequestRow PendingRequest(int courseId) => m.Requests.FirstOrDefault(r => r.CourseId == courseId && r.Status == "Pending review");

        RequestRow FindRequest(string id)
        {
            var rid = ParseId(id, "Request not found.");
            var r = m.Requests.FirstOrDefault(x => x.Id == rid);
            if (r == null) Fail("Request not found.", 404);
            return r;
        }

        int SubmitRequest(CourseRow c, string kind, string reason)
        {
            var id = db.Insert(@"INSERT INTO COURSE_REQUEST (kind, course_id, course_title, program_id, requester_id, status, reason, created_at)
                                 VALUES (@p0, @p1, @p2, @p3, @p4, N'Pending review', @p5, @p6)", kind, c.Id, c.Title, c.ProgramId, Me.Id, Limit(reason, 2000), Now);
            db.Exec("INSERT INTO COURSE_REQUEST_EVENT (request_id, actor_id, action, comment, created_at) VALUES (@p0, @p1, N'Submitted', @p2, @p3)", id, Me.Id, Limit(reason, 2000), Now);
            return id;
        }

        void AppendEvent(RequestRow r, string action, string comment)
        {
            db.Exec("INSERT INTO COURSE_REQUEST_EVENT (request_id, actor_id, action, comment, created_at) VALUES (@p0, @p1, @p2, @p3, @p4)", r.Id, Me.Id, action, Limit(comment, 2000), Now);
            if (action != "Review started") db.Exec("UPDATE COURSE_REQUEST SET status = @p1 WHERE request_id = @p0", r.Id, action);
        }

        static Deferred RequestResult(int id) => reloaded => Workspace.RequestDto(reloaded.Requests.First(r => r.Id == id));

        /// <summary>Deletes an approved course and its materials. Request history (COURSE_REQUEST) is kept.</summary>
        void RemoveCourse(CourseRow c)
        {
            if (m.Enrollments.Any(e => e.CourseId == c.Id)) Fail("This course has learners. Hide it instead.");
            db.Exec(@"DECLARE @c INT = @p0;
                      DELETE FROM COURSE_RESOURCE WHERE course_id = @c;
                      DELETE FROM ANNOUNCEMENT WHERE course_id = @c;
                      DELETE FROM VOICE_CHANNEL WHERE course_id = @c;
                      DELETE FROM WEBSITE_FEEDBACK WHERE course_id = @c;
                      DECLARE @posts TABLE (post_id INT PRIMARY KEY);
                      INSERT INTO @posts SELECT post_id FROM DISCUSSION_POST WHERE course_id = @c OR lab_id IN (SELECT lab_id FROM LAB WHERE course_id = @c);
                      INSERT INTO @posts SELECT post_id FROM DISCUSSION_POST WHERE parent_post_id IN (SELECT post_id FROM @posts) AND post_id NOT IN (SELECT post_id FROM @posts);
                      DELETE FROM MODERATION_LOG WHERE post_id IN (SELECT post_id FROM @posts);
                      DELETE FROM DISCUSSION_POST WHERE post_id IN (SELECT post_id FROM @posts) AND parent_post_id IS NOT NULL;
                      DELETE FROM DISCUSSION_POST WHERE post_id IN (SELECT post_id FROM @posts);
                      DELETE FROM QUIZ_ATTEMPT WHERE quiz_id IN (SELECT quiz_id FROM QUIZ WHERE course_id = @c);
                      DELETE FROM QUIZ WHERE course_id = @c;
                      DELETE FROM LAB_SUBMISSION WHERE lab_id IN (SELECT lab_id FROM LAB WHERE course_id = @c);
                      DELETE FROM LAB WHERE course_id = @c;
                      DELETE FROM LEARNING_PATH_COURSE WHERE course_id = @c;
                      DELETE FROM COURSE WHERE course_id = @c;", c.Id);
        }

        // ---- courses ------------------------------------------------------------------------

        ProgramRow ProgramFor(string id)
        {
            int pid;
            return int.TryParse(id, out pid) ? m.Program(pid) : null;
        }

        object CourseSave()
        {
            Must("Instructor", "Program Leader");
            var c = string.IsNullOrEmpty(Str("id")) ? null : CourseFor(Str("id"), true);
            var p = ProgramFor(Str("programId"));
            if (p == null) Fail("Choose a valid program.");
            if (Me.Role == "Program Leader" && p.LeaderId != Me.Id) Fail("This program is not assigned to you.", 403);
            DateTime parsed;
            DateTime? deadline = null, start = null;
            var deadlineText = Str("deadline") ?? "";
            if (deadlineText != "")
            {
                if (!TryDay(deadlineText, out parsed)) Fail("Choose a valid deadline.");
                deadline = parsed;
            }
            // An omitted startDate keeps the saved value; an empty string clears it.
            var startText = d["startDate"] == null ? Day(c?.StartDate) : (Str("startDate") ?? "");
            if (startText != "")
            {
                if (!TryDay(startText, out parsed)) Fail("Choose a valid start date.");
                start = parsed;
            }
            if (start != null && deadline != null && start > deadline) Fail("Start date must be on or before the deadline.");
            var title = Text("title", "Course title", 200);
            var description = Text("description", "Description", 2000);
            var outcomes = Text("outcomes", "Learning outcomes", 1000);
            var difficulty = Str("difficulty") ?? "Beginner";
            if (!Difficulties.Contains(difficulty)) Fail("Choose a difficulty level.");
            var duration = (int)Math.Min(520, Math.Max(1, Num("duration") ?? 1));
            int id;
            if (c != null)
            {
                RejectPendingReview(c, "Withdraw this submission before editing.");
                id = c.Id;
                db.Exec(@"UPDATE COURSE SET program_id = @p1, title = @p2, description = @p3, skill_gained = @p4, difficulty_level = @p5, deadline = @p6, start_date = @p7,
                          duration_weeks = @p8, approval_status = N'Draft', visibility_status = N'Visible', updated_at = @p9 WHERE course_id = @p0",
                    id, p.Id, title, description, outcomes, difficulty, deadline, start, duration, Now);
                if (c.ProgramId != p.Id) RemoveFromPaths(id);
            }
            else
            {
                id = db.Insert(@"INSERT INTO COURSE (program_id, created_by, title, description, difficulty_level, skill_gained, visibility_status, approval_status, created_at,
                                 start_date, deadline, duration_weeks, color, icon)
                                 VALUES (@p0, @p1, @p2, @p3, @p4, @p5, N'Visible', N'Draft', @p6, @p7, @p8, @p9, N'mint', N'DB')",
                    p.Id, Me.Id, title, description, difficulty, outcomes, Now, start, deadline, duration);
                if (Me.Role == "Instructor")
                    db.Exec("INSERT INTO COURSE_INSTRUCTOR (course_id, instructor_id, assigned_by, assigned_at) VALUES (@p0, @p1, @p1, @p2)", id, Me.Id, Now);
                db.Exec("INSERT INTO QUIZ (course_id, created_by, title, time_limit_minutes, status, created_at) VALUES (@p0, @p1, @p2, 10, N'Draft', @p3)",
                    id, Me.Id, Limit(title + " quiz", 200), Now);
            }
            AppendToPath(p.Id, id);
            Log("Course saved", title);
            return (Deferred)(reloaded => Workspace.CourseDto(reloaded, reloaded.Course(id), true, false));
        }

        object CourseAction(string courseId, string op, string comment)
        {
            var c = CourseFor(courseId, true);
            var pending = PendingRequest(c.Id);
            if (op == "submit")
            {
                if (c.Status == "Pending review" || pending != null) Fail("This course already has a pending request.", 409);
                var lessons = m.LessonsOf(c.Id);
                if (lessons.Count == 0) Fail("Add at least one learning activity.");
                var quiz = m.QuizFor(c.Id);
                if (lessons.Any(l => l.Type == "quiz") && (quiz == null || quiz.Questions.Count == 0 || quiz.Status != "Published"))
                    Fail("Publish the quiz before submitting the course.");
                db.Exec("UPDATE COURSE SET approval_status = N'Pending review', visibility_status = N'Visible', updated_at = @p1 WHERE course_id = @p0", c.Id, Now);
                var request = SubmitRequest(c, "Create", string.IsNullOrWhiteSpace(comment) ? "Submitted for review" : comment.Trim());
                Log("Course submitted", c.Title);
                return RequestResult(request);
            }
            if (op == "hide")
                db.Exec("UPDATE COURSE SET visibility_status = N'Hidden', updated_at = @p1 WHERE course_id = @p0", c.Id, Now);
            else if (op == "withdraw")
            {
                if (c.Status != "Pending review") Fail("This course is not awaiting review.", 409);
                if (pending != null && pending.Kind == "Create") AppendEvent(pending, "Withdrawn", "Submission withdrawn by the course owner.");
                SetCourseDraft(c.Id);
            }
            else if (op == "delete")
            {
                if (pending != null || c.Status == "Pending review") Fail("Resolve or withdraw the pending request first.", 409);
                if (m.Enrollments.Any(e => e.CourseId == c.Id)) Fail("This course has learners. Hide it instead.");
                if (Me.Role == "Instructor")
                {
                    // Instructors cannot delete directly: the Program Leader approves a Delete request.
                    var request = SubmitRequest(c, "Delete", Required(comment, "Reason for deletion", 2000));
                    Log("Course deletion requested", c.Title);
                    return RequestResult(request);
                }
                RemoveCourse(c);
            }
            else Fail("Invalid course action.");
            Log("Course " + op, c.Title);
            return null;
        }

        object LessonSave()
        {
            var c = CourseFor(Str("courseId"), true);
            RejectPendingReview(c, "Withdraw the review before editing content.");
            var type = Str("type");
            var title = Text("title", "Activity title", 200);
            var body = Text("body", "Content", 100000);
            if (type != "article" && type != "lab" && type != "quiz") Fail("Invalid activity type.");
            var answer = type == "lab" ? Text("answer", "Expected answer", 1000) : "";
            var minutes = (int)Math.Min(600, Math.Max(1, Num("minutes") ?? 15));
            var preview = Bool("preview");
            int id;
            var lesson = int.TryParse(Str("id"), out id) ? m.LessonsOf(c.Id).FirstOrDefault(l => l.Id == id) : null;
            if (lesson != null)
                db.Exec("UPDATE LAB SET title = @p1, article_content = @p2, lesson_type = @p3, minutes = @p4, is_preview = @p5 WHERE lab_id = @p0", lesson.Id, title, body, type, minutes, preview);
            else
            {
                var sequence = m.LessonsOf(c.Id).Select(l => l.Sequence).DefaultIfEmpty(0).Max() + 1;
                id = db.Insert(@"INSERT INTO LAB (course_id, title, article_content, sequence_no, is_preview, status, created_at, lesson_type, minutes)
                                 VALUES (@p0, @p1, @p2, @p3, @p4, N'Draft', @p5, @p6, @p7)", c.Id, title, body, sequence, preview, Now, type, minutes);
            }
            if (type == "lab")
            {
                // A lab activity has one text task; its correct_answer is what learners must enter.
                if (lesson?.TaskId != null)
                    db.Exec("UPDATE LAB_TASK SET question_text = @p1, correct_answer = @p2 WHERE task_id = @p0", lesson.TaskId, Limit(body, 2000), answer);
                else
                    db.Exec("INSERT INTO LAB_TASK (lab_id, question_text, task_type, correct_answer, points, sequence_no) VALUES (@p0, @p1, N'Text', @p2, 1, 1)", id, Limit(body, 2000), answer);
            }
            SetCourseDraft(c.Id);
            RecalculateProgress(c.Id);
            return null;
        }

        object LessonDelete()
        {
            var c = CourseFor(Str("courseId"), true);
            RejectPendingReview(c, "Withdraw the review first.");
            int id;
            var lesson = int.TryParse(Str("id"), out id) ? m.LessonsOf(c.Id).FirstOrDefault(l => l.Id == id) : null;
            if (lesson != null)
            {
                db.Exec(@"DELETE FROM LAB_SUBMISSION WHERE lab_id = @p0;
                          UPDATE DISCUSSION_POST SET lab_id = NULL WHERE lab_id = @p0;
                          DELETE FROM LAB WHERE lab_id = @p0;", lesson.Id);
            }
            SetCourseDraft(c.Id);
            RecalculateProgress(c.Id);
            return null;
        }

        sealed class QuestionInput
        {
            public string Id, Text, Topic;
            public List<string> Options;
            public int Answer;
        }

        int EnsureQuiz(CourseRow c)
        {
            var quiz = m.QuizFor(c.Id);
            if (quiz != null) return quiz.Id;
            return db.Insert("INSERT INTO QUIZ (course_id, created_by, title, time_limit_minutes, status, created_at) VALUES (@p0, @p1, @p2, 10, N'Draft', @p3)",
                c.Id, Me.Id, Limit(c.Title + " quiz", 200), Now);
        }

        void DeleteQuestion(int questionId) =>
            db.Exec("DELETE FROM ATTEMPT_ANSWER WHERE question_id = @p0; DELETE FROM QUIZ_QUESTION WHERE question_id = @p0;", questionId);

        object QuizSave()
        {
            var c = CourseFor(Str("courseId"), true);
            RejectPendingReview(c, "Withdraw the review first.");
            var list = d["questions"] as JArray;
            if (list == null || list.Count == 0) Fail("Add at least one question.");
            var inputs = new List<QuestionInput>();
            foreach (var token in list)
            {
                var q = token as JObject;
                var options = (q?["options"] as JArray)?.Select(o => o.Type == JTokenType.String ? ((string)o).Trim() : "").ToList() ?? new List<string>();
                var input = new QuestionInput
                {
                    Id = Str(q, "id"), Text = Required(Str(q, "text"), "Question", 1000), Topic = Limit((Str(q, "topic") ?? "").Trim(), 100), Options = options,
                    Answer = q?["answer"]?.Type == JTokenType.Integer ? (int)q["answer"] : -1
                };
                if (options.Count < 2 || options.Count > 6 || options.Any(o => o == "" || o.Length > 500) || input.Answer < 0 || input.Answer >= options.Count)
                    Fail("Each question needs valid options and a correct answer.");
                inputs.Add(input);
            }
            var timeLimit = (int)Math.Max(1, Num("timeLimit") ?? 10);
            if (timeLimit > 180) Fail("Choose a time limit between 1 and 180 minutes.");

            var quizId = EnsureQuiz(c);
            db.Exec("UPDATE QUIZ SET time_limit_minutes = @p1, status = @p2 WHERE quiz_id = @p0", quizId, timeLimit, Bool("published") ? "Published" : "Draft");
            var existing = m.QuizFor(c.Id)?.Questions ?? new List<QuestionRow>();
            var kept = new HashSet<int>();
            for (int i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                int qid;
                var current = int.TryParse(input.Id, out qid) ? existing.FirstOrDefault(x => x.Id == qid && !kept.Contains(x.Id)) : null;
                if (current == null)
                {
                    qid = db.Insert("INSERT INTO QUIZ_QUESTION (quiz_id, question_text, question_type, points, sequence_no, topic) VALUES (@p0, @p1, N'Multiple choice', 1, @p2, @p3)",
                        quizId, input.Text, i + 1, input.Topic);
                    for (int j = 0; j < input.Options.Count; j++)
                        db.Exec("INSERT INTO QUESTION_OPTION (question_id, option_text, is_correct) VALUES (@p0, @p1, @p2)", qid, input.Options[j], j == input.Answer);
                    continue;
                }
                // Existing question IDs survive edits; options are updated in place so past attempts keep their answers.
                kept.Add(current.Id);
                db.Exec("UPDATE QUIZ_QUESTION SET question_text = @p1, sequence_no = @p2, topic = @p3 WHERE question_id = @p0", current.Id, input.Text, i + 1, input.Topic);
                for (int j = 0; j < input.Options.Count; j++)
                {
                    if (j < current.Options.Count)
                        db.Exec("UPDATE QUESTION_OPTION SET option_text = @p1, is_correct = @p2 WHERE option_id = @p0", current.Options[j].Id, input.Options[j], j == input.Answer);
                    else
                        db.Exec("INSERT INTO QUESTION_OPTION (question_id, option_text, is_correct) VALUES (@p0, @p1, @p2)", current.Id, input.Options[j], j == input.Answer);
                }
                foreach (var extra in current.Options.Skip(input.Options.Count))
                    db.Exec("UPDATE ATTEMPT_ANSWER SET selected_option_id = NULL WHERE selected_option_id = @p0; DELETE FROM QUESTION_OPTION WHERE option_id = @p0;", extra.Id);
            }
            foreach (var removed in existing.Where(x => !kept.Contains(x.Id))) DeleteQuestion(removed.Id);
            SetCourseDraft(c.Id);
            return null;
        }

        object QuizDelete()
        {
            var c = CourseFor(Str("courseId"), true);
            RejectPendingReview(c, "Withdraw the review before editing assessment.");
            var quiz = m.QuizFor(c.Id);
            if (quiz != null)
            {
                foreach (var q in quiz.Questions) DeleteQuestion(q.Id);
                db.Exec("UPDATE QUIZ SET time_limit_minutes = 10, status = N'Draft' WHERE quiz_id = @p0", quiz.Id);
            }
            SetCourseDraft(c.Id);
            return null;
        }

        object ResourceSave()
        {
            var c = CourseFor(Str("courseId"), true);
            RejectPendingReview(c, "Withdraw the review before editing materials.");
            var name = Text("name", "File name", 255);
            var dataUrl = Str("dataUrl") ?? "";
            if (!ResourcePattern.IsMatch(dataUrl)) Fail("Supported files: PDF, TXT, PNG, JPG and WebP.");
            if (dataUrl.Length > 2800000) Fail("Files must be smaller than 2 MB.");
            // size is the file's byte count as sent by the upload form.
            var size = (long)Math.Max(0, Num("size") ?? 0);
            db.Exec("INSERT INTO COURSE_RESOURCE (course_id, uploaded_by, file_name, size_label, data_url, created_at) VALUES (@p0, @p1, @p2, @p3, @p4, @p5)",
                c.Id, Me.Id, name, size.ToString(System.Globalization.CultureInfo.InvariantCulture), dataUrl, Now);
            SetCourseDraft(c.Id);
            return null;
        }

        object ResourceDelete()
        {
            var id = ParseId(Str("id"), "Resource not found.");
            var file = m.Resources.FirstOrDefault(r => r.Id == id);
            if (file == null) Fail("Resource not found.", 404);
            var c = CourseFor(file.CourseId.ToString(), true);
            RejectPendingReview(c, "Withdraw the review before editing materials.");
            db.Exec("DELETE FROM COURSE_RESOURCE WHERE resource_id = @p0", id);
            SetCourseDraft(c.Id);
            return null;
        }

        // ---- Program Leader review of Create requests ---------------------------------------

        object Review()
        {
            Must("Program Leader");
            var c = CourseFor(Str("courseId"), true);
            if (c.Status != "Pending review") Fail("This submission has already been reviewed.", 409);
            var decision = Str("decision");
            if (decision != "Approved" && decision != "Changes requested" && decision != "Rejected") Fail("Choose a review decision.");
            var comment = Text("comment", "Review feedback", 2000);
            var r = PendingRequest(c.Id);
            var requestId = Str("requestId");
            if (!string.IsNullOrEmpty(requestId) && (r == null || r.Id.ToString() != requestId || r.Kind != "Create"))
                Fail("This application is no longer awaiting a decision.", 409);
            db.Exec("UPDATE COURSE SET approval_status = @p1, updated_at = @p2 WHERE course_id = @p0", c.Id, decision == "Approved" ? "Published" : decision, Now);
            db.Exec("INSERT INTO CONTENT_REVIEW (course_id, reviewer_id, decision, comments, reviewed_at) VALUES (@p0, @p1, @p2, @p3, @p4)", c.Id, Me.Id, decision, comment, Now);
            if (decision == "Approved") db.Exec("UPDATE LAB SET status = N'Published' WHERE course_id = @p0", c.Id);
            if (r != null && r.Kind == "Create") AppendEvent(r, decision, comment);
            Log("Course " + decision, c.Title);
            return null;
        }

        object CourseRequest()
        {
            Must("Instructor");
            var kind = Str("kind");
            if (kind != "Create" && kind != "Delete") Fail("Choose a request type.");
            return CourseAction(Str("courseId"), kind == "Delete" ? "delete" : "submit", Text("reason", "Request reason", 2000));
        }

        object CourseRequestWithdraw()
        {
            Must("Instructor");
            var r = FindRequest(Str("id"));
            if (r.RequesterId != Me.Id) Fail("This request belongs to another instructor.", 403);
            if (r.Status != "Pending review") Fail("Only pending requests can be withdrawn.", 409);
            if (r.Kind == "Create") return CourseAction(r.CourseId.ToString(), "withdraw", null);
            AppendEvent(r, "Withdrawn", "Deletion request withdrawn by the instructor.");
            Log("Request withdrawn", r.CourseTitle);
            return RequestResult(r.Id);
        }

        object CourseRequestSeen()
        {
            Must("Program Leader");
            var r = FindRequest(Str("id"));
            CourseFor(r.CourseId.ToString(), true, true);
            if (r.Status != "Pending review") Fail("This request is no longer pending.", 409);
            if (!r.Events.Any(e => e.Action == "Review started")) AppendEvent(r, "Review started", "Program Leader opened the request for review.");
            return RequestResult(r.Id);
        }

        object CourseRequestReview()
        {
            Must("Program Leader");
            var r = FindRequest(Str("id"));
            var c = CourseFor(r.CourseId.ToString(), true, true);
            if (r.Kind != "Delete" || r.Status != "Pending review") Fail("This deletion request is not awaiting a decision.", 409);
            var decision = Str("decision");
            if (decision != "Approved" && decision != "Rejected") Fail("Choose approve or reject.");
            var comment = Text("comment", "Review feedback", 2000);
            if (decision == "Approved") RemoveCourse(c);
            AppendEvent(r, decision, comment);
            Log("Deletion " + decision, r.CourseTitle);
            return RequestResult(r.Id);
        }

        // ---- programs ---------------------------------------------------------------------

        ProgramRow OwnedProgram(string id, int status = 404)
        {
            var p = ProgramFor(id);
            if (p == null || p.LeaderId != Me.Id) Fail("Program not found.", status);
            return p;
        }

        object ProgramSave()
        {
            Must("Program Leader");
            var p = ProgramFor(Str("id"));
            if (!string.IsNullOrEmpty(Str("id")) && p == null) Fail("Program not found.", 404);
            if (p != null && p.LeaderId != Me.Id) Fail("Program access denied.", 403);
            var name = Text("name", "Program name", 150);
            var description = Text("description", "Description", 1000);
            var status = Bool("published") ? "Published" : "Draft";
            int id;
            if (p != null)
            {
                id = p.Id;
                db.Exec("UPDATE PROGRAM SET program_name = @p1, description = @p2, status = @p3 WHERE program_id = @p0", id, name, description, status);
            }
            else
            {
                id = db.Insert("INSERT INTO PROGRAM (program_leader_id, program_name, description, status, created_at) VALUES (@p0, @p1, @p2, @p3, @p4)", Me.Id, name, description, status, Now);
                db.Exec("INSERT INTO LEARNING_PATH (program_id, created_by, path_name, description, created_at) VALUES (@p0, @p1, @p2, @p3, @p4)", id, Me.Id, name, description, Now);
            }
            Log("Program saved", name);
            return new JObject { ["id"] = id.ToString() };
        }

        object ProgramDelete()
        {
            Must("Program Leader");
            var p = OwnedProgram(Str("id"));
            if (m.Courses.Any(c => c.ProgramId == p.Id)) Fail("Move or delete courses before deleting the program.");
            db.Exec(@"DELETE FROM LEARNING_PATH WHERE program_id = @p0;
                      DELETE FROM ANNOUNCEMENT WHERE program_id = @p0;
                      DELETE FROM PROGRAM WHERE program_id = @p0;", p.Id);
            return null;
        }

        object PathSave()
        {
            Must("Program Leader");
            var p = OwnedProgram(Str("id"));
            var list = d["courseIds"] as JArray;
            var ids = new List<int>();
            bool valid = list != null;
            if (valid)
                foreach (var token in list)
                {
                    int cid;
                    if (token.Type != JTokenType.String && token.Type != JTokenType.Integer || !int.TryParse(token.ToString(), out cid)
                        || ids.Contains(cid) || !m.Courses.Any(c => c.Id == cid && c.ProgramId == p.Id)) { valid = false; break; }
                    ids.Add(cid);
                }
            if (!valid) Fail("Select unique courses belonging to this program.");
            SetPath(p.Id, ids);
            return null;
        }

        object ProgramCourseMove()
        {
            Must("Program Leader");
            var c = CourseFor(Str("courseId"), true);
            var target = ProgramFor(Str("programId"));
            if (target == null) Fail("Target program not found.", 404);
            if (target.LeaderId != Me.Id) Fail("Target program is not assigned to you.", 403);
            if (c.ProgramId == target.Id) Fail("Choose a different program.");
            if (c.Status == "Pending review" || PendingRequest(c.Id) != null) Fail("Withdraw the course review before moving this course.", 409);
            if (d["includeInPath"]?.Type != JTokenType.Boolean) Fail("Choose whether to include the course in the target learning path.");
            RemoveFromPaths(c.Id);
            db.Exec("UPDATE COURSE SET program_id = @p1, updated_at = @p2 WHERE course_id = @p0", c.Id, target.Id, Now);
            if ((bool)d["includeInPath"]) AppendToPath(target.Id, c.Id);
            Log("Course moved between programs", c.Title + " → " + target.Name);
            return new JObject { ["id"] = c.Id.ToString(), ["programId"] = target.Id.ToString() };
        }

        object Assign()
        {
            Must("Program Leader");
            var c = CourseFor(Str("courseId"), true);
            var instructorId = Str("instructorId");
            db.Exec("DELETE FROM COURSE_INSTRUCTOR WHERE course_id = @p0", c.Id);
            if (!string.IsNullOrEmpty(instructorId))
            {
                int iid;
                if (!int.TryParse(instructorId, out iid) || !m.Users.Any(u => u.Id == iid && u.Role == "Instructor" && u.Status == "Active")) Fail("Choose an active instructor.");
                db.Exec("INSERT INTO COURSE_INSTRUCTOR (course_id, instructor_id, assigned_by, assigned_at) VALUES (@p0, @p1, @p2, @p3)", c.Id, iid, Me.Id, Now);
            }
            return null;
        }

        object EnrollmentSave()
        {
            var c = CourseFor(Str("courseId"), true);
            int uid;
            if (!int.TryParse(Str("userId"), out uid) || !m.Users.Any(u => u.Id == uid && u.Role == "Learner" && u.Status == "Active")) Fail("Choose an active learner.");
            var existing = m.Enrollments.FirstOrDefault(e => e.CourseId == c.Id && e.UserId == uid);
            if (Bool("remove"))
            {
                if (existing != null) db.Exec("DELETE FROM ENROLLMENT WHERE enrollment_id = @p0", existing.Id);
                return null;
            }
            if (existing != null) Fail("Learner is already enrolled.", 409);
            db.Exec(@"INSERT INTO ENROLLMENT (user_id, course_id, enrolled_by, enroll_date, status, progress_percent, last_active)
                      VALUES (@p0, @p1, @p2, @p3, N'In progress', 0, @p3)", uid, c.Id, Me.Id, Now);
            RecalculateProgress(c.Id);
            return null;
        }

        // ---- announcements ------------------------------------------------------------------

        AnnouncementRow FindAnnouncement(string id)
        {
            var aid = ParseId(id, "Announcement not found.");
            var a = m.Announcements.FirstOrDefault(x => x.Id == aid);
            if (a == null) Fail("Announcement not found.", 404);
            return a;
        }

        /// <summary>Learners must be enrolled; staff must manage the course.</summary>
        void CourseAccess(int? courseId)
        {
            if (courseId == null) Fail("Course not found.", 404);
            CourseRow ignored;
            if (Me.Role == "Learner") Enrolled(courseId.ToString(), out ignored);
            else CourseFor(courseId.ToString(), true);
        }

        object AnnouncementSave()
        {
            var c = CourseFor(Str("courseId"), true);
            AnnouncementRow a = null;
            if (!string.IsNullOrEmpty(Str("id")))
            {
                int aid;
                a = int.TryParse(Str("id"), out aid) ? m.Announcements.FirstOrDefault(x => x.Id == aid) : null;
                if (a != null) CourseFor(a.CourseId.ToString(), true);
            }
            var title = Text("title", "Title", 200);
            var body = Text("body", "Message", 10000);
            if (a != null)
                db.Exec("UPDATE ANNOUNCEMENT SET title = @p1, message = @p2, course_id = @p3, updated_at = @p4 WHERE announcement_id = @p0", a.Id, title, body, c.Id, Now);
            else
                db.Exec("INSERT INTO ANNOUNCEMENT (posted_by, course_id, title, message, created_at) VALUES (@p0, @p1, @p2, @p3, @p4)", Me.Id, c.Id, title, body, Now);
            return null;
        }

        /// <summary>The announcement has been opened by this person: its badge in the course list goes away and stays away.</summary>
        object AnnouncementRead()
        {
            var a = FindAnnouncement(Str("id"));
            CourseAccess(a.CourseId);
            db.Exec("IF NOT EXISTS (SELECT 1 FROM ANNOUNCEMENT_READ WHERE user_id = @p0 AND announcement_id = @p1) INSERT INTO ANNOUNCEMENT_READ (user_id, announcement_id, read_at) VALUES (@p0, @p1, @p2)", Me.Id, a.Id, Now);
            return null;
        }

        object AnnouncementDelete()
        {
            var a = FindAnnouncement(Str("id"));
            CourseFor(a.CourseId.ToString(), true);
            db.Exec("DELETE FROM ANNOUNCEMENT WHERE announcement_id = @p0", a.Id);
            return null;
        }

        object AnnouncementComment()
        {
            var a = FindAnnouncement(Str("id"));
            CourseAccess(a.CourseId);
            var body = Required(Str("comment") ?? Str("text"), "Comment", 1000);
            if (m.AnnouncementComments.Count(x => x.ParentId == a.Id) >= 200) Fail("This announcement has reached its comment limit.");
            var id = db.Insert("INSERT INTO ANNOUNCEMENT_COMMENT (announcement_id, user_id, content, created_at) VALUES (@p0, @p1, @p2, @p3)", a.Id, Me.Id, body, Now);
            return new JObject { ["id"] = id.ToString() };
        }

        object AnnouncementCommentDelete()
        {
            var a = FindAnnouncement(Str("id"));
            int cid;
            var comment = int.TryParse(Str("commentId"), out cid) ? m.AnnouncementComments.FirstOrDefault(x => x.Id == cid && x.ParentId == a.Id) : null;
            if (comment == null) Fail("Comment not found.", 404);
            if (comment.UserId != Me.Id) Fail("You can only delete your own comments.", 403);
            db.Exec("DELETE FROM ANNOUNCEMENT_COMMENT WHERE comment_id = @p0", comment.Id);
            return null;
        }
    }
}
