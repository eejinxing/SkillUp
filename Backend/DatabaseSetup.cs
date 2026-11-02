using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json.Linq;

namespace SKILL_UP.Backend
{
    /// <summary>
    /// Runs once per app start, on the first API request: applies Database/backend-extensions.sql (idempotent) and,
    /// when SkillUp:SeedDemoData is true and there are no accounts yet, loads Database/demo-seed.json.
    /// </summary>
    public static class DatabaseSetup
    {
        static readonly object Gate = new object();
        static bool ready;

        public static void EnsureReady(HttpContext context)
        {
            if (ready) return;
            lock (Gate)
            {
                if (ready) return;
                try
                {
                    using (var db = new Db())
                    {
                        if (db.Scalar("SELECT OBJECT_ID(N'ROLE')") is DBNull)
                            throw new ApiException(503, "The database has no tables yet. Run Database/setup-db.ps1 (see README).");
                        RunScript(db, File.ReadAllText(context.Server.MapPath("~/Database/backend-extensions.sql")));
                        if (Settings.SeedDemoData && (int)db.Scalar("SELECT COUNT(*) FROM [USER]") == 0)
                            DemoSeed.Import(db, JObject.Parse(File.ReadAllText(context.Server.MapPath("~/Database/demo-seed.json"))));
                    }
                }
                catch (SqlException e)
                {
                    throw new ApiException(503, "Cannot open the database App_Data\\SkillUp.mdf. Run Database/setup-db.ps1 (see README). " + e.Message);
                }
                ready = true;
            }
        }

        /// <summary>Executes a script whose batches are separated by GO lines, like sqlcmd does.</summary>
        public static void RunScript(Db db, string script)
        {
            foreach (var batch in Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                if (!string.IsNullOrWhiteSpace(batch)) db.Exec(batch);
        }
    }

    /// <summary>Loads the demo workspace (exported from the frontend fixtures) into the ERD tables.</summary>
    static class DemoSeed
    {
        static DateTime When(JToken token)
        {
            DateTime value;
            var text = (string)token;
            // The seed file holds UTC times; they are stored as the PC's local time like every other row.
            return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out value) ? value.ToLocalTime() : DateTime.Now;
        }

        static string S(JToken token, string key) => token[key]?.Type == JTokenType.String ? (string)token[key] : null;

        public static void Import(Db db, JObject seed)
        {
            db.Begin();
            var roles = db.Query("SELECT role_id, role_name FROM ROLE", r => new { Id = r.Int("role_id"), Name = r.Str("role_name") }).ToDictionary(r => r.Name, r => r.Id);
            var users = new Dictionary<string, int>();
            var programs = new Dictionary<string, int>();
            var courses = new Dictionary<string, int>();
            var lessons = new Dictionary<string, int>();
            var lessonTypes = new Dictionary<string, string>();
            var quizzes = new Dictionary<string, int>();
            var hashes = new Dictionary<string, string>();
            var now = DateTime.Now;

            foreach (var u in seed["users"])
            {
                var password = S(u, "password") ?? "Demo1234!";
                if (!hashes.ContainsKey(password)) hashes[password] = Passwords.Hash(password);
                var name = (S(u, "name") ?? "").Trim();
                var space = name.IndexOf(' ');
                var id = db.Insert(@"INSERT INTO [USER] (role_id, first_name, last_name, username, password, email, bio, preferred_language, account_status, created_at, location, theme, profile_picture)
                                     VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12)",
                    roles[S(u, "role")], space < 0 ? name : name.Substring(0, space), space < 0 ? "" : name.Substring(space + 1), S(u, "username"), hashes[password],
                    S(u, "email"), S(u, "bio"), S(u, "language") ?? "en", S(u, "status") ?? "Active", now, S(u, "location"), S(u, "theme") ?? "dark", S(u, "avatar"));
                users[S(u, "id")] = id;
                if (S(u, "role") == "Instructor")
                    db.Exec("INSERT INTO INSTRUCTOR_PROFILE (user_id, verification_status, verified_at) VALUES (@p0, @p1, @p2)",
                        id, S(u, "status") == "Active" ? "Approved" : "Pending", S(u, "status") == "Active" ? (object)now : null);
            }
            // Fallback owner for records whose author is missing in the fixtures.
            var admin = users[S(seed["users"].FirstOrDefault(u => S(u, "role") == "Admin") ?? seed["users"].First(), "id")];

            foreach (var p in seed["programs"])
            {
                var leader = S(p, "leaderId") != null && users.ContainsKey(S(p, "leaderId")) ? users[S(p, "leaderId")] : (int?)null;
                var id = db.Insert("INSERT INTO PROGRAM (program_leader_id, program_name, description, status, created_at) VALUES (@p0, @p1, @p2, @p3, @p4)",
                    leader, S(p, "name"), S(p, "description"), (bool?)p["published"] == true ? "Published" : "Draft", now);
                db.Exec("INSERT INTO LEARNING_PATH (program_id, created_by, path_name, description, created_at) VALUES (@p0, @p1, @p2, @p3, @p4)",
                    id, leader ?? admin, S(p, "name"), S(p, "description"), now);
                programs[S(p, "id")] = id;
            }

            foreach (var c in seed["courses"])
            {
                var status = S(c, "status") ?? "Draft";
                var instructor = S(c, "instructorId") != null && users.ContainsKey(S(c, "instructorId")) ? users[S(c, "instructorId")] : (int?)null;
                DateTime day;
                var id = db.Insert(@"INSERT INTO COURSE (program_id, created_by, title, description, difficulty_level, skill_gained, visibility_status, approval_status, created_at,
                                     start_date, deadline, duration_weeks, color, icon, thumbnail)
                                     VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13, @p14)",
                    programs[S(c, "programId")], instructor ?? admin, S(c, "title"), S(c, "description"), S(c, "difficulty") ?? "Beginner", S(c, "outcomes"),
                    status == "Hidden" ? "Hidden" : "Visible", status == "Hidden" ? "Published" : status, now,
                    DateTime.TryParseExact(S(c, "startDate") ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day) ? (object)day : null,
                    DateTime.TryParseExact(S(c, "deadline") ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day) ? (object)day : null,
                    (int?)c["duration"] ?? 1, S(c, "color"), S(c, "icon"), S(c, "artwork"));
                courses[S(c, "id")] = id;
                if (instructor != null) db.Exec("INSERT INTO COURSE_INSTRUCTOR (course_id, instructor_id, assigned_at) VALUES (@p0, @p1, @p2)", id, instructor, now);

                var sequence = 1;
                foreach (var l in c["lessons"] ?? new JArray())
                {
                    var type = S(l, "type") ?? "article";
                    var lessonId = db.Insert(@"INSERT INTO LAB (course_id, title, article_content, sequence_no, is_preview, status, created_at, lesson_type, minutes, workspace, starter_code)
                                               VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10)",
                        id, S(l, "title"), S(l, "body"), sequence++, (bool?)l["preview"] == true, status == "Published" ? "Published" : "Draft", now, type, (int?)l["minutes"] ?? 15,
                        S(l, "workspace") ?? "none", S(l, "starterCode"));
                    if (type == "lab")
                        db.Exec("INSERT INTO LAB_TASK (lab_id, question_text, task_type, correct_answer, points, sequence_no) VALUES (@p0, @p1, N'Text', @p2, 1, 1)",
                            lessonId, S(l, "body") ?? "", S(l, "answer") ?? "");
                    lessons[S(l, "id")] = lessonId;
                    lessonTypes[S(l, "id")] = type;
                }

                var quiz = c["quiz"] as JObject ?? new JObject();
                var quizId = db.Insert("INSERT INTO QUIZ (course_id, created_by, title, time_limit_minutes, status, created_at) VALUES (@p0, @p1, @p2, @p3, @p4, @p5)",
                    id, instructor ?? admin, S(c, "title") + " quiz", (int?)quiz["timeLimit"] ?? 10, (bool?)quiz["published"] == true ? "Published" : "Draft", now);
                quizzes[S(c, "id")] = quizId;
                var number = 1;
                foreach (var q in quiz["questions"] ?? new JArray())
                {
                    var questionId = db.Insert("INSERT INTO QUIZ_QUESTION (quiz_id, question_text, question_type, points, sequence_no, topic) VALUES (@p0, @p1, N'Multiple choice', 1, @p2, @p3)",
                        quizId, S(q, "text"), number++, S(q, "topic"));
                    var options = q["options"] as JArray ?? new JArray();
                    for (int i = 0; i < options.Count; i++)
                        db.Exec("INSERT INTO QUESTION_OPTION (question_id, option_text, is_correct) VALUES (@p0, @p1, @p2)", questionId, (string)options[i], (int?)q["answer"] == i);
                }
            }

            foreach (var p in seed["programs"])
            {
                var path = (int)db.Scalar("SELECT path_id FROM LEARNING_PATH WHERE program_id = @p0", programs[S(p, "id")]);
                var order = 1;
                foreach (var cid in (p["courseIds"] ?? new JArray()).Select(x => (string)x).Where(courses.ContainsKey).Distinct())
                    db.Exec("INSERT INTO LEARNING_PATH_COURSE (path_id, course_id, sequence_no, is_starting_course) VALUES (@p0, @p1, @p2, @p3)", path, courses[cid], order, order++ == 1);
            }

            foreach (var e in seed["enrollments"])
            {
                if (!users.ContainsKey(S(e, "userId")) || !courses.ContainsKey(S(e, "courseId"))) continue;
                var joined = When(e["joinedAt"]);
                db.Exec("INSERT INTO ENROLLMENT (user_id, course_id, enroll_date, status, progress_percent, last_active) VALUES (@p0, @p1, @p2, N'In progress', 0, @p3)",
                    users[S(e, "userId")], courses[S(e, "courseId")], joined, When(e["lastActive"]));
                foreach (var lid in (e["completed"] ?? new JArray()).Select(x => (string)x).Where(lessons.ContainsKey))
                    db.Exec("INSERT INTO LAB_SUBMISSION (user_id, lab_id, score, result_status, submitted_at) VALUES (@p0, @p1, @p2, N'Passed', @p3)",
                        users[S(e, "userId")], lessons[lid], lessonTypes[lid] == "article" ? null : (object)100m, joined);
            }

            foreach (var a in seed["attempts"])
            {
                if (!users.ContainsKey(S(a, "userId")) || !courses.ContainsKey(S(a, "courseId"))) continue;
                if (S(a, "type") == "lab" && S(a, "lessonId") != null && lessons.ContainsKey(S(a, "lessonId")))
                {
                    db.Exec("INSERT INTO LAB_SUBMISSION (user_id, lab_id, score, result_status, submitted_at) VALUES (@p0, @p1, @p2, @p3, @p4)", users[S(a, "userId")],
                        lessons[S(a, "lessonId")], (decimal)((int?)a["score"] ?? 0), ((int?)a["score"] ?? 0) >= 100 ? "Passed" : "Failed", When(a["createdAt"]));
                    continue;
                }
                var quizId = quizzes[S(a, "courseId")];
                var attempt = db.Insert("INSERT INTO QUIZ_ATTEMPT (quiz_id, user_id, score, started_at, submitted_at) VALUES (@p0, @p1, @p2, @p3, @p3)",
                    quizId, users[S(a, "userId")], (decimal)((int?)a["score"] ?? 0), When(a["createdAt"]));
                var answers = (a["answers"] as JArray ?? new JArray()).Select(x => (int)x).ToList();
                var questions = db.Query("SELECT question_id FROM QUIZ_QUESTION WHERE quiz_id = @p0 ORDER BY sequence_no", r => r.Int("question_id"), quizId);
                if (answers.Count != questions.Count) continue;
                for (int i = 0; i < questions.Count; i++)
                {
                    var options = db.Query("SELECT option_id, is_correct FROM QUESTION_OPTION WHERE question_id = @p0 ORDER BY option_id",
                        r => new { Id = r.Int("option_id"), Correct = r.Bool("is_correct") }, questions[i]);
                    var chosen = answers[i] >= 0 && answers[i] < options.Count ? options[answers[i]] : null;
                    db.Exec("INSERT INTO ATTEMPT_ANSWER (attempt_id, question_id, selected_option_id, is_correct) VALUES (@p0, @p1, @p2, @p3)",
                        attempt, questions[i], chosen?.Id, chosen?.Correct == true);
                }
            }

            foreach (var r in seed["reviews"])
            {
                if (!users.ContainsKey(S(r, "userId")) || !courses.ContainsKey(S(r, "courseId"))) continue;
                var id = db.Insert("INSERT INTO WEBSITE_FEEDBACK (user_id, course_id, rating, comment, is_public, created_at) VALUES (@p0, @p1, @p2, @p3, 1, @p4)",
                    users[S(r, "userId")], courses[S(r, "courseId")], (byte)((int?)r["rating"] ?? 5), S(r, "text"), When(r["createdAt"]));
                foreach (var reply in r["replies"] ?? new JArray())
                    if (users.ContainsKey(S(reply, "userId")))
                        db.Exec("INSERT INTO FEEDBACK_REPLY (feedback_id, user_id, content, created_at) VALUES (@p0, @p1, @p2, @p3)", id, users[S(reply, "userId")], S(reply, "text"), When(reply["createdAt"]));
            }
            foreach (var f in seed["feedback"])
                if (users.ContainsKey(S(f, "userId")))
                    db.Exec("INSERT INTO WEBSITE_FEEDBACK (user_id, rating, comment, is_public, created_at) VALUES (@p0, @p1, @p2, 1, @p3)",
                        users[S(f, "userId")], (byte)Math.Min(5, Math.Max(1, (int?)f["rating"] ?? 5)), S(f, "text"), When(f["createdAt"]));

            foreach (var p in seed["posts"])
            {
                if (!users.ContainsKey(S(p, "userId")) || !courses.ContainsKey(S(p, "courseId"))) continue;
                var id = db.Insert("INSERT INTO DISCUSSION_POST (user_id, course_id, content, status, created_at) VALUES (@p0, @p1, @p2, @p3, @p4)",
                    users[S(p, "userId")], courses[S(p, "courseId")], S(p, "text"), S(p, "status") ?? "Visible", When(p["createdAt"]));
                foreach (var reply in p["replies"] ?? new JArray())
                    if (users.ContainsKey(S(reply, "userId")))
                        db.Exec("INSERT INTO DISCUSSION_POST (user_id, course_id, parent_post_id, content, status, created_at) VALUES (@p0, @p1, @p2, @p3, N'Visible', @p4)",
                            users[S(reply, "userId")], courses[S(p, "courseId")], id, S(reply, "text"), When(reply["createdAt"]));
            }
            foreach (var g in seed["globalMessages"])
                if (users.ContainsKey(S(g, "userId")))
                    db.Exec("INSERT INTO DISCUSSION_POST (user_id, content, status, created_at) VALUES (@p0, @p1, N'Visible', @p2)", users[S(g, "userId")], S(g, "text"), When(g["createdAt"]));

            foreach (var a in seed["announcements"])
                if (users.ContainsKey(S(a, "userId")) && courses.ContainsKey(S(a, "courseId")))
                    db.Exec("INSERT INTO ANNOUNCEMENT (posted_by, course_id, title, message, created_at) VALUES (@p0, @p1, @p2, @p3, @p4)",
                        users[S(a, "userId")], courses[S(a, "courseId")], S(a, "title"), S(a, "body"), When(a["createdAt"]));
            foreach (var x in seed["messages"])
                if (users.ContainsKey(S(x, "from")) && users.ContainsKey(S(x, "to")))
                    db.Exec("INSERT INTO CHAT_MESSAGE (sender_id, receiver_id, content, is_read, sent_at) VALUES (@p0, @p1, @p2, @p3, @p4)",
                        users[S(x, "from")], users[S(x, "to")], S(x, "text"), (bool?)x["read"] == true, When(x["createdAt"]));
            foreach (var s in seed["sessions"])
            {
                DateTime starts;
                if (!users.ContainsKey(S(s, "hostId")) || !courses.ContainsKey(S(s, "courseId"))) continue;
                DateTime.TryParse(S(s, "startsAt"), CultureInfo.InvariantCulture, DateTimeStyles.None, out starts);
                db.Exec("INSERT INTO VOICE_CHANNEL (course_id, created_by, channel_name, status, created_at, starts_at) VALUES (@p0, @p1, @p2, N'Open', @p3, @p4)",
                    courses[S(s, "courseId")], users[S(s, "hostId")], S(s, "title"), now, starts == default(DateTime) ? (object)null : starts);
            }

            // A "Submitted" approval on a course still pending review becomes an open Create request.
            foreach (var a in seed["approvals"])
            {
                var cid = S(a, "courseId");
                if (S(a, "decision") != "Submitted" || !courses.ContainsKey(cid) || !users.ContainsKey(S(a, "actorId"))) continue;
                var course = seed["courses"].First(c => S(c, "id") == cid);
                if (S(course, "status") != "Pending review") continue;
                var when = When(a["createdAt"]);
                var request = db.Insert(@"INSERT INTO COURSE_REQUEST (kind, course_id, course_title, program_id, requester_id, status, reason, created_at)
                                          VALUES (N'Create', @p0, @p1, @p2, @p3, N'Pending review', @p4, @p5)",
                    courses[cid], S(course, "title"), programs[S(course, "programId")], users[S(a, "actorId")], S(a, "comment"), when);
                db.Exec("INSERT INTO COURSE_REQUEST_EVENT (request_id, actor_id, action, comment, created_at) VALUES (@p0, @p1, N'Submitted', @p2, @p3)",
                    request, users[S(a, "actorId")], S(a, "comment"), when);
            }
            foreach (var a in seed["audit"])
                db.Exec("INSERT INTO ACTIVITY_LOG (user_id, action, created_at, detail) VALUES (@p0, @p1, @p2, @p3)",
                    S(a, "actorId") != null && users.ContainsKey(S(a, "actorId")) ? users[S(a, "actorId")] : (int?)null, S(a, "action"), When(a["createdAt"]), S(a, "detail"));

            foreach (var id in courses.Values)
                db.Exec(@"UPDATE e SET progress_percent = CASE WHEN t.total = 0 THEN 0 ELSE CAST(ROUND(100.0 * x.done / t.total, 2) AS DECIMAL(5,2)) END,
                            status = CASE WHEN t.total > 0 AND x.done >= t.total THEN N'Completed' ELSE N'In progress' END,
                            completed_at = CASE WHEN t.total > 0 AND x.done >= t.total THEN e.last_active END
                          FROM ENROLLMENT e
                          CROSS APPLY (SELECT COUNT(*) AS total FROM LAB WHERE course_id = e.course_id) t
                          CROSS APPLY (SELECT COUNT(DISTINCT s.lab_id) AS done FROM LAB_SUBMISSION s JOIN LAB l ON l.lab_id = s.lab_id
                                       WHERE l.course_id = e.course_id AND s.user_id = e.user_id AND s.result_status = N'Passed') x
                          WHERE e.course_id = @p0", id);
            db.Commit();
        }
    }
}
