using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json.Linq;

namespace SKILL_UP.Backend
{
    /// <summary>A command result that needs the reloaded data, for example a newly created record's DTO.</summary>
    public delegate object Deferred(Model reloaded);

    /// <summary>
    /// POST /api/commands/{action}. Each command validates against the loaded Model, writes with SQL inside the
    /// request's transaction, and returns the "data" part of { data, state }. The rules follow the prototype's
    /// mock adapter (Frontend/js/api.js) and docs/API-CONTRACT.md, but every check runs on the server.
    /// Commands are split by area across Commands*.cs.
    /// </summary>
    public sealed partial class Commands
    {
        static readonly string[] Roles = { "Learner", "Instructor", "Program Leader", "Admin" };
        static readonly string[] Statuses = { "Active", "Inactive", "Pending" };
        static readonly Regex EmailPattern = new Regex(@"^\S+@\S+\.\S+$");

        readonly Db db;
        readonly Model m;
        JObject d;
        bool logged;

        public UserRow Me { get; private set; }
        /// <summary>The companion's reply from Gemini (Companion.Reply), fetched before the command gate; null uses the sample replies.</summary>
        public string AiReply { get; set; }
        /// <summary>Set by login/demo: the handler issues the sign-in cookie after the transaction commits.</summary>
        public int? SignIn { get; private set; }
        public bool SignOut { get; private set; }
        /// <summary>The caller's network address, used only to limit repeated sign-in and reset attempts.</summary>
        public string ClientIp { get; set; }

        /// <summary>The PC's current local time. Stored as is, so the database shows the same clock as the device.</summary>
        static DateTime Now => DateTime.Now;

        /// <summary>Random key from this browser's HttpOnly cookie (VisitorCookie). ENQUIRY_ACCESS links it to the enquiries sent from here.</summary>
        public string VisitorKey { get; set; }
        /// <summary>Set when a command created a visitor key; the handler sends it as a cookie after the transaction commits.</summary>
        public string NewVisitorKey { get; private set; }

        public Commands(Db db, Model m, UserRow me)
        {
            this.db = db;
            this.m = m;
            Me = me;
        }

        public object Run(string action, JObject payload)
        {
            d = payload ?? new JObject();
            switch (action)
            {
                case "login": return Login();
                case "logout": SignOut = true; Me = null; return null;
                case "register": return Register();
                case "forgot": return Forgot();
                case "resetPassword": return ResetPassword();
                case "enquirySave": return EnquirySave();
                case "enquiryDelete": return EnquiryDelete();
                case "enquiryList": return EnquiryList();
            }
            Must();
            object result;
            switch (action)
            {
                case "preferences": result = Preferences(); break;
                case "profile": result = Profile(); break;
                case "password": result = Password(); break;
                case "enrol": result = Enrol(); break;
                case "complete": result = Complete(); break;
                case "quizSubmit": result = QuizSubmit(); break;
                case "courseSave": result = CourseSave(); break;
                case "courseAction": result = CourseAction(Str("id"), Str("action"), Str("comment")); break;
                case "courseArt": result = CourseArt(); break;
                case "lessonSave": result = LessonSave(); break;
                case "lessonDelete": result = LessonDelete(); break;
                case "quizSave": result = QuizSave(); break;
                case "quizDelete": result = QuizDelete(); break;
                case "resourceSave": result = ResourceSave(); break;
                case "resourceDelete": result = ResourceDelete(); break;
                case "review": result = Review(); break;
                case "courseRequest": result = CourseRequest(); break;
                case "courseRequestWithdraw": result = CourseRequestWithdraw(); break;
                case "courseRequestSeen": result = CourseRequestSeen(); break;
                case "courseRequestReview": result = CourseRequestReview(); break;
                case "programSave": result = ProgramSave(); break;
                case "programDelete": result = ProgramDelete(); break;
                case "pathSave": result = PathSave(); break;
                case "programCourseMove": result = ProgramCourseMove(); break;
                case "assign": result = Assign(); break;
                case "enrollmentSave": result = EnrollmentSave(); break;
                case "announcementSave": result = AnnouncementSave(); break;
                case "announcementDelete": result = AnnouncementDelete(); break;
                case "announcementComment": result = AnnouncementComment(); break;
                case "announcementCommentDelete": result = AnnouncementCommentDelete(); break;
                case "post": result = Post(); break;
                case "reply": result = Reply(); break;
                case "moderate": result = Moderate(); break;
                case "courseReview": result = CourseReview(); break;
                case "feedback": result = Feedback(); break;
                case "globalMessage": result = GlobalMessage(); break;
                case "message": result = Message(); break;
                case "readMessages": result = ReadMessages(); break;
                case "sessionSave": result = SessionSave(); break;
                case "sessionJoin": result = SessionJoin(); break;
                case "ai": result = Ai(); break;
                case "aiRead": result = AiRead(); break;
                case "announcementRead": result = AnnouncementRead(); break;
                case "userSave": result = UserSave(); break;
                case "userDelete": result = UserDelete(); break;
                case "staffTransfer": result = StaffTransfer(); break;
                default: throw new ApiException(404, "Unknown action: " + action);
            }
            var inboxClick = action == "preferences" && Str("section") == "leaderInbox";   // marking an alert read is not audit-worthy
            if (!logged && action != "ai" && action != "aiRead" && action != "announcementRead" && action != "readMessages" && !inboxClick) Log(action, Str("title") ?? Str("courseId") ?? Str("id") ?? Me.Name);
            return result;
        }

        // ---- payload helpers -------------------------------------------------------------

        string Str(string key) => Str(d, key);

        static string Str(JObject source, string key)
        {
            var token = source?[key];
            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined) return null;
            if (token.Type == JTokenType.String) return (string)token;
            var value = token as JValue;
            return value == null ? null : Convert.ToString(value.Value, CultureInfo.InvariantCulture);
        }

        /// <summary>JavaScript-style truthiness, so checkbox values such as "on" count as true.</summary>
        bool Bool(string key)
        {
            var token = d[key];
            if (token == null) return false;
            switch (token.Type)
            {
                case JTokenType.Boolean: return (bool)token;
                case JTokenType.Integer: return (long)token != 0;
                case JTokenType.String: var s = (string)token; return s != "" && s != "false";
                default: return false;
            }
        }

        double? Num(string key)
        {
            double value;
            return double.TryParse(Str(key), NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !double.IsNaN(value) ? value : (double?)null;
        }

        /// <summary>A required, trimmed text field.</summary>
        string Text(string key, string label, int max = 4000) => Required(Str(key), label, max);

        static string Required(string value, string label, int max)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ApiException(400, label + " is required.");
            value = value.Trim();
            if (value.Length > max) throw new ApiException(400, label + " must be " + max + " characters or fewer.");
            return value;
        }

        static string Limit(string value, int max) => value == null ? null : (value.Length > max ? value.Substring(0, max) : value);

        static int ParseId(string id, string notFound)
        {
            int value;
            if (!int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)) throw new ApiException(404, notFound);
            return value;
        }

        static bool TryDay(string text, out DateTime day) =>
            DateTime.TryParseExact(text ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day);

        static string Day(DateTime? date) => date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";

        static void Fail(string message, int status = 400)
        {
            throw new ApiException(status, message);
        }

        // ---- shared rules -----------------------------------------------------------------

        UserRow Must(params string[] roles)
        {
            if (Me == null) Fail("Please sign in to continue.", 401);
            if (roles.Length > 0 && !roles.Contains(Me.Role)) Fail("You do not have permission for this action.", 403);
            return Me;
        }

        /// <param name="manage">Require staff who manage the course (assigned instructor, program leader or admin).</param>
        CourseRow CourseFor(string courseId, bool manage = false, bool allowDeletionReview = false)
        {
            var c = m.Course(ParseId(courseId, "Course not found."));
            if (c == null) Fail("Course not found.", 404);
            if (!manage) return c;
            var u = Must("Instructor", "Program Leader", "Admin");
            if (u.Role == "Instructor" && c.InstructorId != u.Id) Fail("This course is assigned to another instructor.", 403);
            if (u.Role == "Program Leader" && !m.Programs.Any(p => p.Id == c.ProgramId && p.LeaderId == u.Id)) Fail("This program is not assigned to you.", 403);
            if (!allowDeletionReview && m.Requests.Any(r => r.CourseId == c.Id && r.Kind == "Delete" && r.Status == "Pending review"))
                Fail("A deletion request is pending. Withdraw it before editing this course.", 409);
            return c;
        }

        EnrollmentRow Enrolled(string courseId, out CourseRow course)
        {
            course = CourseFor(courseId);
            var u = Must("Learner");
            if (course.Status != "Published") Fail("This course is not available.", 403);
            var id = course.Id;
            var e = m.Enrollments.FirstOrDefault(x => x.UserId == u.Id && x.CourseId == id);
            if (e == null) Fail("Enrol in this course first.", 403);
            return e;
        }

        void UniqueUser(string name, string username, string email, int? except)
        {
            Required(name, "Name", 100);
            Required(username, "Username", 50);
            if (!EmailPattern.IsMatch(email ?? "") || email.Length > 255) Fail("Enter a valid email address.");
            if (m.Users.Any(u => u.Id != except && (string.Equals(u.Email, email.Trim(), StringComparison.OrdinalIgnoreCase)
                                                    || string.Equals(u.Username, username.Trim(), StringComparison.OrdinalIgnoreCase))))
                Fail("This email or username is already in use.", 409);
        }

        /// <summary>USER keeps first_name / last_name; the UI uses one display name.</summary>
        static string[] SplitName(string name)
        {
            var clean = Regex.Replace(name.Trim(), @"\s+", " ");
            int space = clean.IndexOf(' ');
            var parts = space < 0 ? new[] { clean, "" } : new[] { clean.Substring(0, space), clean.Substring(space + 1) };
            if (parts[0].Length > 50 || parts[1].Length > 50) Fail("Name is too long.");
            return parts;
        }

        int RoleId(string role)
        {
            var id = db.Scalar("SELECT role_id FROM ROLE WHERE role_name = @p0", role);
            if (id == null) Fail("Choose a valid role.");
            return (int)id;
        }

        void Log(string action, string detail, string targetType = null, int? targetId = null)
        {
            db.Exec("INSERT INTO ACTIVITY_LOG (user_id, action, target_type, target_id, created_at, detail) VALUES (@p0, @p1, @p2, @p3, @p4, @p5)",
                Me?.Id, Limit(action, 100), targetType, targetId, Now, Limit(detail, 500));
            logged = true;
        }

        // ---- public commands: sign-in, registration, password reset, enquiries ---------------

        object Login()
        {
            var login = (Str("email") ?? "").Trim();
            // Failed attempts are counted per typed name and per address, whether or not the account exists.
            var nameKey = "login:" + login.ToLowerInvariant();
            var addressKey = "login-ip:" + (ClientIp ?? "unknown");
            AttemptGuard.Check(nameKey, addressKey);
            var u = m.Users.FirstOrDefault(x => string.Equals(x.Email, login, StringComparison.OrdinalIgnoreCase) || string.Equals(x.Username, login, StringComparison.OrdinalIgnoreCase));
            // An unknown account is checked against a dummy hash, so the response time does not reveal whether it exists.
            var correct = Passwords.Verify(Str("password") ?? "", u?.Password ?? Passwords.DummyHash);
            if (u == null || !correct)
            {
                AttemptGuard.Fail(nameKey, 5);
                AttemptGuard.Fail(addressKey, 30);
                Fail("Email or password is incorrect.", 401);
            }
            if (u.Status != "Active") Fail(u.Status == "Pending" ? "Your instructor application is awaiting administrator approval." : "This account has been deactivated.", 403);
            AttemptGuard.Clear(nameKey);
            if (Passwords.NeedsRehash(u.Password)) db.Exec("UPDATE [USER] SET password = @p1 WHERE user_id = @p0", u.Id, Passwords.Hash(Str("password")));
            db.Exec("UPDATE [USER] SET last_login = @p1 WHERE user_id = @p0", u.Id, Now);
            Me = u;
            SignIn = u.Id;
            Log("Signed in", u.Name);
            return null;
        }

        object Register()
        {
            var name = Str("name") ?? "";
            var username = (Str("username") ?? "").Trim();
            var email = (Str("email") ?? "").Trim();
            UniqueUser(name, username, email, null);
            var role = Str("role");
            if (role != "Learner" && role != "Instructor") Fail("Select a valid registration role.");
            var password = Str("password") ?? "";
            CheckNewPassword(password, Str("confirm"));
            var parts = SplitName(name);
            var status = role == "Instructor" ? "Pending" : "Active";
            var id = db.Insert(@"INSERT INTO [USER] (role_id, first_name, last_name, username, password, email, preferred_language, account_status, theme, created_at)
                                 VALUES (@p0, @p1, @p2, @p3, @p4, @p5, N'en', @p6, N'dark', @p7)",
                RoleId(role), parts[0], parts[1], username, Passwords.Hash(password), email.ToLowerInvariant(), status, Now);
            if (role == "Instructor") db.Exec("INSERT INTO INSTRUCTOR_PROFILE (user_id, verification_status) VALUES (@p0, N'Pending')", id);
            Log("Account registered", name.Trim(), "User", id);
            return new JObject { ["pending"] = status == "Pending" };
        }

        object Forgot()
        {
            var email = (Str("email") ?? "").Trim();
            var addressKey = "forgot-ip:" + (ClientIp ?? "unknown");
            AttemptGuard.Check(addressKey);
            AttemptGuard.Fail(addressKey, 10);
            var u = m.Users.FirstOrDefault(x => string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase));
            // The answer is the same whether or not the email has an account, so this form cannot be used to find accounts.
            if (u == null) return new JObject { ["sent"] = true };
            var token = Tokens.New();
            db.Exec("UPDATE PASSWORD_RESET SET is_used = 1 WHERE user_id = @p0 AND is_used = 0", u.Id);
            db.Exec("INSERT INTO PASSWORD_RESET (user_id, token_hash, created_at, expires_at) VALUES (@p0, @p1, @p2, @p3)", u.Id, Tokens.Sha256(token), Now, Now.AddMinutes(15));
            Log("Password reset requested", u.Email, "User", u.Id);
            // No email service is connected yet: in development the link is shown on screen.
            return Settings.ShowResetLink ? new JObject { ["token"] = token } : new JObject { ["sent"] = true };
        }

        object ResetPassword()
        {
            var reset = db.Query("SELECT TOP 1 reset_id, user_id FROM PASSWORD_RESET WHERE token_hash = @p0 AND is_used = 0 AND expires_at > @p1",
                r => new { Id = r.Int("reset_id"), User = r.Int("user_id") }, Tokens.Sha256(Str("token") ?? ""), Now).FirstOrDefault();
            if (reset == null) Fail("Reset link is invalid or expired.");
            var password = Str("password") ?? "";
            CheckNewPassword(password, Str("confirm"));
            var owner = m.User(reset.User);
            if (owner != null && Passwords.Verify(password, owner.Password)) Fail("Your new password must be different from your current password.");
            db.Exec("UPDATE [USER] SET password = @p1 WHERE user_id = @p0", reset.User, Passwords.Hash(password));
            db.Exec("UPDATE PASSWORD_RESET SET is_used = 1 WHERE reset_id = @p0", reset.Id);
            Log("Password reset", "", "User", reset.User);
            return null;
        }

        // ---- enquiries ---------------------------------------------------------------------
        // An enquiry opens in the browser that sent it (ENQUIRY_ACCESS, keyed by the hashed visitor cookie) and for the
        // signed-in account that sent it.

        EnquiryRow FindEnquiry()
        {
            int id;
            return int.TryParse(Str("id"), out id) ? m.Enquiries.FirstOrDefault(x => x.Id == id) : null;
        }

        bool LinkedToBrowser(EnquiryRow q)
        {
            // A signed-in account sees only its own enquiries; a guest sees only the guest enquiries sent from this browser.
            if (Me != null) return q.UserId == Me.Id;
            return q.UserId == null && VisitorKey != null && (int)db.Scalar("SELECT COUNT(*) FROM ENQUIRY_ACCESS WHERE visitor_key_hash = @p0 AND enquiry_id = @p1",
                Tokens.Sha256(VisitorKey), q.Id) > 0;
        }

        /// <summary>Records in the database that this browser may open the enquiry. Creates the browser's key on first use.</summary>
        void LinkToBrowser(int enquiryId)
        {
            if (VisitorKey == null) VisitorKey = NewVisitorKey = Tokens.New();
            db.Exec(@"IF NOT EXISTS (SELECT 1 FROM ENQUIRY_ACCESS WHERE visitor_key_hash = @p0 AND enquiry_id = @p1)
                      INSERT INTO ENQUIRY_ACCESS (visitor_key_hash, enquiry_id, granted_at) VALUES (@p0, @p1, @p2)", Tokens.Sha256(VisitorKey), enquiryId, Now);
        }

        object EnquirySave()
        {
            var name = Text("name", "Name", 100);
            var subject = Text("subject", "Subject", 200);
            var message = Text("message", "Message", 4000);
            var email = (Str("email") ?? "").Trim();
            if (!EmailPattern.IsMatch(email) || email.Length > 255) Fail("Enter a valid email address.");
            var existing = FindEnquiry();
            var row = new EnquiryRow { Name = name, Email = email, Subject = subject, Message = message, Status = "Open", CreatedAt = Now };
            if (existing != null)
            {
                if (!LinkedToBrowser(existing)) Fail("Enquiry not found.", 404);
                db.Exec("UPDATE ENQUIRY SET name = @p1, email = @p2, subject = @p3, message = @p4, updated_at = @p5 WHERE enquiry_id = @p0", existing.Id, name, email, subject, message, Now);
                row.Id = existing.Id;
                row.Status = existing.Status;
                row.CreatedAt = existing.CreatedAt;
            }
            else
            {
                row.Id = db.Insert(@"INSERT INTO ENQUIRY (user_id, name, email, subject, message, status, submitted_at)
                                     VALUES (@p0, @p1, @p2, @p3, @p4, N'Open', @p5)", Me?.Id, name, email, subject, message, Now);
                LinkToBrowser(row.Id);
            }
            return Workspace.EnquiryDto(row);
        }

        object EnquiryDelete()
        {
            var q = FindEnquiry();
            if (q == null || !LinkedToBrowser(q)) Fail("Enquiry not found.", 404);
            db.Exec("DELETE FROM ENQUIRY WHERE enquiry_id = @p0", q.Id);   // ENQUIRY_ACCESS rows go with it (ON DELETE CASCADE)
            return null;
        }

        /// <summary>"My enquiries": a signed-in account's own, or, for a guest, the guest enquiries sent from this browser. The two are never mixed.</summary>
        object EnquiryList()
        {
            var linked = VisitorKey == null ? new HashSet<int>() : new HashSet<int>(db.Query(
                "SELECT enquiry_id FROM ENQUIRY_ACCESS WHERE visitor_key_hash = @p0", r => r.Int("enquiry_id"), Tokens.Sha256(VisitorKey)));
            return m.Enquiries.Where(q => Me != null ? q.UserId == Me.Id : q.UserId == null && linked.Contains(q.Id))
                .OrderByDescending(q => q.CreatedAt).ThenByDescending(q => q.Id)
                .Select(q => Workspace.EnquiryDto(q)).ToList();
        }

        // ---- own account -----------------------------------------------------------------

        object Preferences()
        {
            JObject preferences;
            try { preferences = string.IsNullOrEmpty(Me.Preferences) ? new JObject() : JObject.Parse(Me.Preferences); }
            catch (Exception) { preferences = new JObject(); }
            var section = Str("section");
            if (section == "address")
                foreach (var key in new[] { "address", "city", "country", "postcode" }) preferences[key] = Limit((Str(key) ?? "").Trim(), 300);
            else if (section == "notifications")
                foreach (var key in new[] { "courseUpdates", "deadlines", "messages", "reviewUpdates" }) preferences[key] = d[key]?.Type == JTokenType.Boolean && (bool)d[key];
            else if (section == "leaderInbox")
            {
                // Program Leader dashboard: which alerts are read or archived (item IDs such as "msg-12").
                var inbox = new JObject();
                foreach (var key in new[] { "read", "archived" })
                    inbox[key] = JArray.FromObject(((d[key] as JArray) ?? new JArray()).Where(t => t.Type == JTokenType.String)
                        .Select(t => Limit((string)t, 80)).Distinct().Take(500).ToList());
                preferences["leaderInbox"] = inbox;
            }
            else Fail("Unknown preferences section.");
            db.Exec("UPDATE [USER] SET preferences = @p1 WHERE user_id = @p0", Me.Id, preferences.ToString(Newtonsoft.Json.Formatting.None));
            if (section != "leaderInbox") Log("Preferences updated", section);
            return null;
        }

        static readonly Regex AvatarPattern = new Regex(@"^(data:image/(png|jpeg|webp|gif);base64,|https?://|assets/)", RegexOptions.IgnoreCase);

        object Profile()
        {
            var name = Str("name") ?? Me.Name;
            var username = (Str("username") ?? Me.Username).Trim();
            var email = (Str("email") ?? Me.Email).Trim();
            UniqueUser(name, username, email, Me.Id);
            var parts = SplitName(Text("name", "Name", 100));
            var avatar = Me.Avatar;
            var token = d["avatar"];
            if (token != null && token.Type != JTokenType.Null)
            {
                avatar = Str("avatar");
                if (avatar == "") avatar = null;
                else if (!AvatarPattern.IsMatch(avatar) || avatar.Length > 2800000) Fail("Profile images must be PNG, JPG, WebP or GIF and smaller than 2 MB.");
            }
            var theme = Str("theme") == "light" ? "light" : "dark";
            db.Exec(@"UPDATE [USER] SET first_name = @p1, last_name = @p2, email = @p3, username = @p4, location = @p5, bio = @p6, profile_picture = @p7,
                      preferred_language = @p8, theme = @p9 WHERE user_id = @p0",
                Me.Id, parts[0], parts[1], email.ToLowerInvariant(), username, Limit((Str("location") ?? "").Trim(), 100), Limit(Str("bio") ?? "", 1000), avatar,
                Limit(string.IsNullOrEmpty(Str("language")) ? "en" : Str("language"), 10), theme);
            return null;
        }

        object Password()
        {
            // A stolen session cannot be used to guess the current password.
            var key = "password:" + Me.Id;
            AttemptGuard.Check(key);
            if (!Passwords.Verify(Str("current") ?? "", Me.Password))
            {
                AttemptGuard.Fail(key, 5);
                Fail("Current password is incorrect.");
            }
            AttemptGuard.Clear(key);
            var password = Str("password") ?? "";
            CheckNewPassword(password, Str("confirm"));
            if (password == (Str("current") ?? "")) Fail("Your new password must be different from your current password.");
            db.Exec("UPDATE [USER] SET password = @p1 WHERE user_id = @p0", Me.Id, Passwords.Hash(password));
            Log("Password changed", Me.Name, "User", Me.Id);
            // Every other session of this account ends (the cookie is tied to the password); this browser gets a fresh one.
            SignIn = Me.Id;
            return null;
        }

        static void CheckNewPassword(string password, string confirm)
        {
            if (password != confirm) Fail("The two passwords do not match.");
            var problem = Passwords.Problem(password);
            if (problem != null) Fail(problem);
        }
    }
}
