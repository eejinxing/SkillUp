using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace SKILL_UP.Backend
{
    /// <summary>
    /// Builds the role-scoped workspace JSON described in docs/API-CONTRACT.md.
    /// The browser only ever receives what the signed-in role may see: no password hashes, no enquiry
    /// tokens, and lab answers / quiz answer keys only for staff who manage that course.
    /// </summary>
    public static class Workspace
    {
        /// <summary>Sent to the browser as UTC with a Z, so it converts the time to the viewer's own clock. Database times are the PC's local time.</summary>
        public static string Iso(DateTime time)
        {
            var utc = time.Kind == DateTimeKind.Utc ? time : DateTime.SpecifyKind(time, DateTimeKind.Local).ToUniversalTime();
            return utc.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
        }
        static string Day(DateTime? date) => date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
        static string Id(int id) => id.ToString(CultureInfo.InvariantCulture);
        static string Id(int? id) => id?.ToString(CultureInfo.InvariantCulture);

        static long Bytes(string size)
        {
            long value;
            return long.TryParse(size, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : 0;
        }

        public static bool IsStaff(UserRow u) => u != null && (u.Role == "Instructor" || u.Role == "Program Leader" || u.Role == "Admin");

        /// <summary>Courses this user may edit: assigned instructor, leader of the course's program, or any for Admin.</summary>
        public static HashSet<int> ManagedCourses(Model m, UserRow u)
        {
            if (u == null) return new HashSet<int>();
            if (u.Role == "Admin") return new HashSet<int>(m.Courses.Select(c => c.Id));
            if (u.Role == "Instructor") return new HashSet<int>(m.Courses.Where(c => c.InstructorId == u.Id).Select(c => c.Id));
            if (u.Role == "Program Leader")
            {
                var owned = new HashSet<int>(m.Programs.Where(p => p.LeaderId == u.Id).Select(p => p.Id));
                return new HashSet<int>(m.Courses.Where(c => owned.Contains(c.ProgramId)).Select(c => c.Id));
            }
            return new HashSet<int>();
        }

        public static JObject Build(Model m, UserRow me)
        {
            var role = me?.Role;
            bool admin = role == "Admin", learner = role == "Learner";
            var managed = ManagedCourses(m, me);
            var enrolled = new HashSet<int>(me == null ? Enumerable.Empty<int>() : m.Enrollments.Where(e => e.UserId == me.Id).Select(e => e.CourseId));

            // Courses the viewer can see at all, and those whose answer keys they may see.
            var visible = new HashSet<int>(m.Courses.Where(c => c.Status == "Published" || managed.Contains(c.Id) || enrolled.Contains(c.Id)).Select(c => c.Id));
            // Course areas (announcements, sessions, files) open to the viewer: managed courses, or enrolled ones for learners.
            var member = new HashSet<int>(managed);
            if (learner) member.UnionWith(enrolled);

            var state = new JObject
            {
                ["version"] = 1,
                ["currentUser"] = me == null ? null : UserDto(me, true, true)
            };

            // Users: admins see every account; signed-in users see active accounts (contacts, names, leaderboard); visitors see course authors.
            IEnumerable<UserRow> users;
            if (admin) users = m.Users;
            else if (me != null)
            {
                var related = new HashSet<int>(m.Enrollments.Where(e => member.Contains(e.CourseId)).Select(e => e.UserId)) { me.Id };
                users = m.Users.Where(u => u.Status == "Active" || related.Contains(u.Id));
            }
            else
            {
                var authors = new HashSet<int>(m.Courses.Where(c => visible.Contains(c.Id) && c.InstructorId != null).Select(c => c.InstructorId.Value));
                authors.UnionWith(m.Feedback.Where(f => f.Public && (f.CourseId == null || visible.Contains(f.CourseId.Value))).Select(f => f.UserId));
                users = m.Users.Where(u => u.Status == "Active" && authors.Contains(u.Id));
            }
            state["users"] = new JArray(users.Select(u => UserDto(u, admin || u.Id == me?.Id, IsStaff(me))));

            var programIds = new HashSet<int>(m.Courses.Where(c => visible.Contains(c.Id)).Select(c => c.ProgramId));
            state["programs"] = new JArray(m.Programs.Where(p => IsStaff(me) || p.Published || programIds.Contains(p.Id)).Select(p => ProgramDto(m, p)));
            state["courses"] = new JArray(m.Courses.Where(c => visible.Contains(c.Id)).Select(c => CourseDto(m, c, managed.Contains(c.Id), me == null)));

            // Learners also receive other learners' progress and quiz scores (without answers) so the leaderboard can rank them.
            var enrollments = m.Enrollments.Where(e => admin || managed.Contains(e.CourseId) || e.UserId == me?.Id || (learner && visible.Contains(e.CourseId)));
            state["enrollments"] = new JArray(enrollments.Select(e => EnrollmentDto(m, e)));
            state["attempts"] = new JArray(Attempts(m, me, managed, visible));

            state["reviews"] = new JArray(m.Feedback.Where(f => f.CourseId != null && visible.Contains(f.CourseId.Value)).Select(f => ReviewDto(m, f)));
            state["posts"] = new JArray(m.Posts.Where(p => p.CourseId != null && p.ParentId == null && visible.Contains(p.CourseId.Value) && me != null
                    && (managed.Contains(p.CourseId.Value) || p.Status == "Visible" || p.UserId == me.Id))
                .OrderByDescending(p => p.CreatedAt).Select(p => PostDto(m, p)));
            state["announcements"] = new JArray(m.Announcements.Where(a => admin || (a.CourseId != null && member.Contains(a.CourseId.Value))).Select(a => AnnouncementDto(m, a)));
            state["messages"] = new JArray(me == null ? Enumerable.Empty<JObject>() : m.Messages.Where(x => x.SenderId == me.Id || x.ReceiverId == me.Id).Select(x => new JObject
            {
                ["id"] = Id(x.Id), ["from"] = Id(x.SenderId), ["to"] = Id(x.ReceiverId), ["text"] = x.Content, ["createdAt"] = Iso(x.SentAt), ["read"] = x.Read
            }));
            state["globalMessages"] = new JArray(me == null ? Enumerable.Empty<JObject>() : m.Posts.Where(p => p.CourseId == null && p.LabId == null && p.ParentId == null && p.Status == "Visible").Select(p => new JObject
            {
                ["id"] = Id(p.Id), ["userId"] = Id(p.UserId), ["text"] = p.Content, ["createdAt"] = Iso(p.CreatedAt)
            }));
            state["sessions"] = new JArray(m.Channels.Where(s => admin || member.Contains(s.CourseId)).Select(s => new JObject
            {
                ["id"] = Id(s.Id), ["courseId"] = Id(s.CourseId), ["hostId"] = Id(s.CreatedBy), ["title"] = s.Name,
                ["startsAt"] = s.StartsAt?.ToString("yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture) ?? "", ["attendees"] = new JArray(s.Attendees.Select(Id))
            }));
            state["ai"] = new JArray(me == null ? Enumerable.Empty<JObject>() : AiHistory(m, me.Id));
            state["resources"] = new JArray(m.Resources.Where(r => admin || member.Contains(r.CourseId)).Select(r => new JObject
            {
                ["id"] = Id(r.Id), ["courseId"] = Id(r.CourseId), ["name"] = r.Name, ["size"] = Bytes(r.Size), ["dataUrl"] = r.DataUrl, ["createdAt"] = Iso(r.CreatedAt)
            }));
            state["feedback"] = new JArray(m.Feedback.Where(f => f.CourseId == null && (admin || f.Public || f.UserId == me?.Id)).Select(f => new JObject
            {
                ["id"] = Id(f.Id), ["userId"] = Id(f.UserId), ["text"] = f.Comment ?? "", ["rating"] = f.Rating, ["createdAt"] = Iso(f.CreatedAt)
            }));
            // Enquiries are only reachable from the browser or account that sent them (enquiryList); only Admin gets a list.
            state["enquiries"] = new JArray(admin ? m.Enquiries.Select(q => EnquiryDto(q)) : Enumerable.Empty<JObject>());

            var requests = m.Requests.Where(r => admin
                || (role == "Instructor" && r.RequesterId == me.Id)
                || (role == "Program Leader" && (managed.Contains(r.CourseId) || m.Programs.Any(p => p.Id == r.ProgramId && p.LeaderId == me.Id)))).ToList();
            state["courseRequests"] = new JArray(requests.Select(RequestDto));
            state["approvals"] = new JArray(requests.Where(r => r.Kind == "Create").SelectMany(r => r.Events.Where(e => e.Action != "Review started").Select(e => new { r, e }))
                .OrderByDescending(x => x.e.CreatedAt).ThenByDescending(x => x.e.Id).Select(x => new JObject
                {
                    ["id"] = Id(x.e.Id), ["courseId"] = Id(x.r.CourseId), ["actorId"] = Id(x.e.ActorId), ["decision"] = x.e.Action, ["comment"] = x.e.Comment ?? "", ["createdAt"] = Iso(x.e.CreatedAt)
                }));
            state["audit"] = new JArray(admin ? m.Audit.Select(a => AuditDto(a)) : Enumerable.Empty<JObject>());
            state["xpLog"] = new JArray();
            return state;
        }

        public static JObject UserDto(UserRow u, bool full, bool staffViewer)
        {
            var dto = new JObject
            {
                ["id"] = Id(u.Id), ["name"] = u.Name, ["username"] = u.Username, ["role"] = u.Role, ["status"] = u.Status, ["bio"] = u.Bio ?? "",
                ["location"] = u.Location ?? "", ["avatar"] = u.Avatar, ["theme"] = u.Theme ?? "dark", ["language"] = u.Language ?? "en"
            };
            if (full || staffViewer) dto["email"] = u.Email;
            if (full)
            {
                dto["createdAt"] = Iso(u.CreatedAt);
                dto["lastActiveAt"] = u.LastLogin == null ? null : Iso(u.LastLogin.Value);
                JObject preferences = null;
                try { preferences = string.IsNullOrEmpty(u.Preferences) ? null : JObject.Parse(u.Preferences); } catch (Exception) { }
                dto["preferences"] = preferences ?? new JObject();
            }
            return dto;
        }

        public static JObject ProgramDto(Model m, ProgramRow p) => new JObject
        {
            ["id"] = Id(p.Id), ["name"] = p.Name, ["description"] = p.Description ?? "", ["leaderId"] = Id(p.LeaderId),
            ["courseIds"] = new JArray(m.PathOf(p.Id).Select(Id)), ["published"] = p.Published
        };

        /// <param name="full">Include lab answers and the quiz answer key (staff who manage the course).</param>
        /// <param name="visitor">Signed-out visitors only see the body of activities marked as a public preview.</param>
        public static JObject CourseDto(Model m, CourseRow c, bool full, bool visitor)
        {
            var quiz = m.QuizFor(c.Id);
            var dto = new JObject
            {
                ["id"] = Id(c.Id), ["title"] = c.Title, ["description"] = c.Description ?? "", ["outcomes"] = c.Outcomes ?? "", ["programId"] = Id(c.ProgramId),
                ["instructorId"] = Id(c.InstructorId), ["difficulty"] = c.Difficulty, ["status"] = c.Status, ["deadline"] = Day(c.Deadline), ["startDate"] = Day(c.StartDate),
                ["duration"] = c.Duration, ["color"] = c.Color ?? "mint", ["icon"] = c.Icon ?? "UP",
                ["lessons"] = new JArray(m.LessonsOf(c.Id).Select(l =>
                {
                    var lesson = new JObject
                    {
                        ["id"] = Id(l.Id), ["title"] = l.Title, ["type"] = l.Type, ["body"] = visitor && !l.Preview ? "" : (l.Body ?? ""), ["minutes"] = l.Minutes, ["preview"] = l.Preview
                    };
                    if (full) lesson["answer"] = l.Type == "lab" ? (l.Answer ?? "") : "";
                    return lesson;
                })),
                ["quiz"] = new JObject
                {
                    ["published"] = quiz?.Status == "Published",
                    ["timeLimit"] = quiz?.TimeLimit ?? 10,
                    ["questions"] = new JArray((quiz?.Questions ?? new List<QuestionRow>()).Select(q =>
                    {
                        var question = new JObject { ["id"] = Id(q.Id), ["text"] = q.Text, ["options"] = new JArray(q.Options.Select(o => o.Text)), ["topic"] = q.Topic ?? "" };
                        if (full) question["answer"] = q.AnswerIndex;
                        return question;
                    }))
                }
            };
            if (!string.IsNullOrEmpty(c.Thumbnail)) dto["artwork"] = c.Thumbnail;
            return dto;
        }

        public static JObject EnrollmentDto(Model m, EnrollmentRow e)
        {
            var completed = m.CompletedLessons(e.UserId, e.CourseId);
            var minutes = m.Lessons.Where(l => completed.Contains(l.Id)).Sum(l => l.Minutes);
            return new JObject
            {
                ["id"] = Id(e.Id), ["userId"] = Id(e.UserId), ["courseId"] = Id(e.CourseId), ["completed"] = new JArray(completed.Select(Id)), ["minutes"] = minutes,
                ["joinedAt"] = Iso(e.EnrolledAt), ["lastActive"] = Iso(e.LastActive ?? e.EnrolledAt)
            };
        }

        static IEnumerable<JObject> Attempts(Model m, UserRow me, HashSet<int> managed, HashSet<int> visible)
        {
            if (me == null) yield break;
            bool admin = me.Role == "Admin", learner = me.Role == "Learner";
            var rows = new List<Tuple<DateTime, JObject>>();
            foreach (var a in m.QuizAttempts)
            {
                var quiz = m.Quizzes.FirstOrDefault(q => q.Id == a.QuizId);
                if (quiz == null) continue;
                bool own = a.UserId == me.Id, full = admin || own || managed.Contains(quiz.CourseId);
                if (!full && !(learner && visible.Contains(quiz.CourseId))) continue;
                rows.Add(Tuple.Create(a.CreatedAt, QuizAttemptDto(m, a, quiz, full)));
            }
            foreach (var s in m.Submissions)
            {
                var lesson = m.Lessons.FirstOrDefault(l => l.Id == s.LabId);
                if (lesson == null || lesson.Type != "lab") continue;
                if (!(admin || s.UserId == me.Id || managed.Contains(lesson.CourseId))) continue;
                rows.Add(Tuple.Create(s.SubmittedAt, new JObject
                {
                    ["id"] = "lab-" + Id(s.Id), ["userId"] = Id(s.UserId), ["courseId"] = Id(lesson.CourseId), ["lessonId"] = Id(lesson.Id), ["type"] = "lab",
                    ["score"] = (int)Math.Round(s.Score ?? 0), ["wrongTopics"] = s.Result == "Passed" ? new JArray() : new JArray(lesson.Title), ["createdAt"] = Iso(s.SubmittedAt)
                }));
            }
            foreach (var row in rows.OrderBy(r => r.Item1)) yield return row.Item2;
        }

        public static JObject QuizAttemptDto(Model m, QuizAttemptRow a, QuizRow quiz, bool full)
        {
            var quizLesson = m.LessonsOf(quiz.CourseId).FirstOrDefault(l => l.Type == "quiz");
            var dto = new JObject
            {
                ["id"] = "quiz-" + Id(a.Id), ["userId"] = Id(a.UserId), ["courseId"] = Id(quiz.CourseId), ["type"] = "quiz",
                ["score"] = (int)Math.Round(a.Score ?? 0), ["createdAt"] = Iso(a.CreatedAt), ["answers"] = new JArray(), ["wrongTopics"] = new JArray()
            };
            if (quizLesson != null) dto["lessonId"] = Id(quizLesson.Id);
            if (!full) return dto;
            var answers = new JArray();
            var wrong = new JArray();
            foreach (var q in quiz.Questions)
            {
                var answer = a.Answers.FirstOrDefault(x => x.QuestionId == q.Id);
                if (answer == null) continue;
                answers.Add(answer.OptionId == null ? -1 : q.Options.FindIndex(o => o.Id == answer.OptionId));
                if (!answer.Correct) wrong.Add(q.Topic ?? "");
            }
            dto["answers"] = answers;
            dto["wrongTopics"] = wrong;
            return dto;
        }

        static JArray Replies(IEnumerable<ReplyRow> rows) => new JArray(rows.Select(r => new JObject
        {
            ["id"] = Id(r.Id), ["userId"] = Id(r.UserId), ["text"] = r.Text, ["createdAt"] = Iso(r.CreatedAt)
        }));

        static JObject ReviewDto(Model m, FeedbackRow f) => new JObject
        {
            ["id"] = Id(f.Id), ["courseId"] = Id(f.CourseId), ["userId"] = Id(f.UserId), ["rating"] = f.Rating, ["text"] = f.Comment ?? "", ["createdAt"] = Iso(f.CreatedAt),
            ["replies"] = Replies(m.FeedbackReplies.Where(r => r.ParentId == f.Id))
        };

        static JObject PostDto(Model m, PostRow p)
        {
            var dto = new JObject
            {
                ["id"] = Id(p.Id), ["courseId"] = Id(p.CourseId), ["userId"] = Id(p.UserId), ["text"] = p.Content, ["status"] = p.Status, ["createdAt"] = Iso(p.CreatedAt),
                ["replies"] = new JArray(m.Posts.Where(r => r.ParentId == p.Id).Select(r => new JObject
                {
                    ["id"] = Id(r.Id), ["userId"] = Id(r.UserId), ["text"] = r.Content, ["createdAt"] = Iso(r.CreatedAt)
                }))
            };
            if (p.Reason != null) dto["reason"] = p.Reason;
            return dto;
        }

        static JObject AnnouncementDto(Model m, AnnouncementRow a) => new JObject
        {
            ["id"] = Id(a.Id), ["courseId"] = Id(a.CourseId), ["userId"] = Id(a.PostedBy), ["title"] = a.Title, ["body"] = a.Message, ["createdAt"] = Iso(a.CreatedAt),
            ["comments"] = Replies(m.AnnouncementComments.Where(c => c.ParentId == a.Id))
        };

        /// <summary>AI_CHAT_MESSAGE rows come in pairs: the user's prompt, then the AI reply.</summary>
        static IEnumerable<JObject> AiHistory(Model m, int userId)
        {
            AiMessageRow prompt = null;
            foreach (var row in m.AiMessages.Where(x => x.UserId == userId))
            {
                if (row.Sender == "User") { prompt = row; continue; }
                if (prompt == null) continue;
                yield return new JObject { ["id"] = Id(prompt.Id), ["userId"] = Id(userId), ["prompt"] = prompt.Content, ["response"] = row.Content, ["createdAt"] = Iso(prompt.CreatedAt) };
                prompt = null;
            }
        }

        public static JObject EnquiryDto(EnquiryRow q)
        {
            return new JObject
            {
                ["id"] = Id(q.Id), ["name"] = q.Name, ["email"] = q.Email, ["subject"] = q.Subject, ["message"] = q.Message, ["status"] = q.Status, ["createdAt"] = Iso(q.CreatedAt)
            };
        }

        public static JObject RequestDto(RequestRow r)
        {
            var first = r.Events.FirstOrDefault(e => e.Action == "Submitted");
            return new JObject
            {
                ["id"] = Id(r.Id), ["submissionId"] = first == null ? null : Id(first.Id), ["kind"] = r.Kind, ["courseId"] = Id(r.CourseId), ["courseTitle"] = r.CourseTitle,
                ["programId"] = Id(r.ProgramId), ["requesterId"] = Id(r.RequesterId), ["status"] = r.Status, ["reason"] = r.Reason ?? "", ["createdAt"] = Iso(r.CreatedAt),
                ["events"] = new JArray(r.Events.Select(e => new JObject
                {
                    ["id"] = Id(e.Id), ["actorId"] = Id(e.ActorId), ["action"] = e.Action, ["comment"] = e.Comment ?? "", ["createdAt"] = Iso(e.CreatedAt)
                }))
            };
        }

        static JObject AuditDto(AuditRow a)
        {
            var dto = new JObject { ["id"] = Id(a.Id), ["actorId"] = Id(a.UserId), ["action"] = a.Action, ["detail"] = a.Detail ?? "", ["createdAt"] = Iso(a.CreatedAt) };
            if (a.TargetType == "User" && a.TargetId != null) dto["targetUserId"] = Id(a.TargetId);
            return dto;
        }
    }
}
