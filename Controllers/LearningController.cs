using System.Globalization;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc;

namespace SkillUp.Controllers;

/// <summary>A learner's own pages: their courses, certificates and ranking, then the course and activity pages.</summary>
public sealed class LearningController : SkillController
{
    // ---- My courses ---------------------------------------------------------------------------

    [HttpGet("MyCourses"), Page("learning", Roles = "Learner")]
    public IActionResult MyCourses()
    {
        ViewData["Title"] = "My courses";
        var model = new MyCoursesModel(Ctx);
        model.Load();
        if (model.JustOpened != null) Ctx.Page = new PageAttribute("learning") { Roles = "Learner", BodyClass = "mc-kit-arrival" };
        return View(model);
    }

    /// <summary>
    /// Enroll button on a program pouch: enrols the learner in every course of the program that they are not in yet, and opens that
    /// program. js/pack-opening.js presses this button when a pouch has been torn open.
    /// </summary>
    [HttpPost("MyCourses/Enroll"), Page("learning", Roles = "Learner")]
    public async Task<IActionResult> EnrolProgram(int program)
    {
        var chosen = Data.Programs.FirstOrDefault(p => p.Id == program && p.Published);
        var courses = chosen == null ? new List<CourseRow>() : new MyCoursesModel(Ctx).CoursesOf(chosen);
        if (courses.Count == 0) return Go("/MyCourses", "This program has no open courses yet.");
        var joined = new List<CourseRow>();
        foreach (var course in courses)
        {
            if (Ctx.MyEnrollments.Any(x => x.CourseId == course.Id)) continue;
            if ((await TryRun("enrol", new { courseId = course.Id.ToString() })).Failed) return Redirect("/MyCourses?program=" + chosen.Id);
            joined.Add(course);
        }
        HttpContext.Session.SetInt32(MyCoursesModel.OpenedKey, chosen.Id);
        return Go("/MyCourses?program=" + chosen.Id, joined.Count == 0 ? "You’re already enrolled in " + chosen.Name + "."
            : joined.Count == 1 ? "You’re enrolled in " + joined[0].Title + "."
            : "You’re enrolled in " + joined.Count + " courses of " + chosen.Name + ".");
    }

    // ---- Certificates and leaderboard ---------------------------------------------------------

    [HttpGet("Certificates"), Page("certificates", Roles = "Learner", Secondary = true)]
    public IActionResult Certificates()
    {
        var model = new CertificatesModel(Ctx);
        model.Load();
        ViewData["Title"] = model.PageTitle ?? "Certificates";
        return View(model);
    }

    [HttpGet("Leaderboard"), Page("leaderboard", Roles = "Learner")]
    public IActionResult Leaderboard()
    {
        ViewData["Title"] = "Leaderboard";
        var model = new LeaderboardModel(Ctx);
        model.Load();
        return View(model);
    }

    // ---- Course page -------------------------------------------------------------------------

    [HttpGet("Course"), Page("course", StayAfterSignIn = true)]
    public IActionResult Course()
    {
        var model = new CourseModel(Ctx);
        model.Load();
        ViewData["Title"] = model.PageTitle ?? "Course";
        return View(model);
    }

    static string CoursePage(int id) => "/Course?id=" + id;

    [HttpPost("Course/Enrol"), Page("course", StayAfterSignIn = true)]
    public async Task<IActionResult> Enrol(int id)
    {
        var result = await TryRun("enrol", new { courseId = id.ToString() });
        return result.Failed ? Redirect(CoursePage(id)) : Go(CoursePage(id), "You’re enrolled. Your first activity is ready.");
    }

    [HttpPost("Course/Review"), Page("course", StayAfterSignIn = true)]
    public async Task<IActionResult> Review(int id, string rating, string text)
    {
        var result = await TryRun("courseReview", new { courseId = id.ToString(), rating, text = (text ?? "").Trim() });
        return result.Failed ? Redirect(CoursePage(id)) : Go(CoursePage(id), "Review saved.");
    }

    [HttpPost("Course/Reply"), Page("course", StayAfterSignIn = true)]
    public async Task<IActionResult> Reply(int id, int reviewId, string text)
    {
        var result = await TryRun("reply", new { id = reviewId.ToString(), kind = "review", text = (text ?? "").Trim() });
        return result.Failed ? Redirect(CoursePage(id)) : Go(CoursePage(id), "Reply posted.");
    }

    // ---- Activity page -----------------------------------------------------------------------

    [HttpGet("Activity"), Page("lesson", StayAfterSignIn = true)]
    public async Task<IActionResult> Activity()
    {
        var lessonId = Ctx.QueryInt("lesson");
        // Showing the conversation reads it (the badge in the AI Assistant list goes away).
        if (Me != null && lessonId != null && Data.AiMessages.Any(x => x.UserId == Me.Id && x.LessonId == lessonId && x.Sender == "AI" && !x.Read))
        {
            try { await Run("aiRead", new { lessonId = lessonId.ToString() }); }
            catch (ApiException) { /* the badge simply stays */ }
        }
        var model = new ActivityModel(Ctx);
        model.Load();
        return View(model);
    }

    static string ActivityPage(int course, int lesson) => "/Activity?course=" + course + "&lesson=" + lesson;

    [HttpPost("Activity/Complete"), Page("lesson", Roles = "Learner")]
    public async Task<IActionResult> ActivityComplete(int course, int lesson)
    {
        var result = await TryRun("complete", new { courseId = course.ToString(), lessonId = lesson.ToString() });
        // Stay on this activity: the learner moves on with the Next activity button when ready.
        return result.Failed ? Redirect(ActivityPage(course, lesson)) : Go(ActivityPage(course, lesson), "Activity completed");
    }

    [HttpPost("Activity/Lab"), Page("lesson", Roles = "Learner")]
    public async Task<IActionResult> ActivityLab(int course, int lesson, string answer)
    {
        answer = (answer ?? "").Trim();
        var back = ActivityPage(course, lesson);
        if (answer.Length == 0) return Go(back, "Type your answer first.");
        var result = await TryRun("complete", new { courseId = course.ToString(), lessonId = lesson.ToString(), answer });
        if (result.Failed) return Redirect(back);
        if ((bool?)((JObject)result.Data)?["correct"] == true) return Go(back, "Correct — lab completed!");
        return Go(back, "Not quite. Check the method name and try again. Your attempt has been recorded.");
    }

    [HttpPost("Activity/Quiz"), Page("lesson", Roles = "Learner")]
    public async Task<IActionResult> ActivityQuiz(int course, int lesson)
    {
        var back = ActivityPage(course, lesson);
        var quiz = Data.QuizFor(course);
        if (quiz == null) return Redirect(back);
        var key = "SkillUp.Quiz." + Me.Id + "." + course + "." + lesson;
        var startText = HttpContext.Session.GetString(key + ".Start");
        DateTime? start = startText == null ? null : DateTime.Parse(startText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        if (start != null && (DateTime.Now - start.Value).TotalMinutes > (quiz.TimeLimit ?? 10)) return Go(back, "Time is up. Restart this quiz to try again.");
        var answers = new JArray();
        for (var n = 0; n < quiz.Questions.Count; n++)
            answers.Add(int.TryParse(Request.Form["q" + n], NumberStyles.Integer, CultureInfo.InvariantCulture, out var choice) ? choice : -1);
        if (answers.Any(a => (int)a < 0)) return Go(back, "Answer every question before submitting.");
        var result = await TryRun("quizSubmit", new JObject { ["courseId"] = course.ToString(), ["lessonId"] = lesson.ToString(), ["answers"] = answers, ["startedAt"] = start?.ToString("o", CultureInfo.InvariantCulture) });
        if (result.Failed) return Redirect(back);
        HttpContext.Session.Remove(key + ".Start");
        HttpContext.Session.SetString(key + ".Result", ((JObject)result.Data).ToString());
        return Redirect(back);
    }

    [HttpPost("Activity/Restart"), Page("lesson", Roles = "Learner")]
    public IActionResult ActivityRestart(int course, int lesson)
    {
        HttpContext.Session.Remove("SkillUp.Quiz." + Me.Id + "." + course + "." + lesson + ".Start");
        return Redirect(ActivityPage(course, lesson));
    }

    [HttpPost("Activity/Ask"), Page("lesson", Roles = "Learner")]
    public async Task<IActionResult> ActivityAsk(int course, int lesson, string text)
    {
        text = (text ?? "").Trim();
        if (text.Length == 0) return Go(ActivityPage(course, lesson), "Type a question first.");
        await TryRun("ai", new { text, lessonId = lesson.ToString() });
        return Redirect(ActivityPage(course, lesson));
    }

    // ---- AI assistant ------------------------------------------------------------------------

    [HttpGet("Assistant"), Page("assistant", Roles = "*", Secondary = true)]
    public async Task<IActionResult> Assistant()
    {
        ViewData["Title"] = "AI Assistant";
        var model = new AssistantModel(Ctx);
        model.Load();
        // Showing the conversation reads it: the badge for its replies goes away and stays away.
        if (Data.AiMessages.Any(x => x.UserId == Me.Id && x.LessonId == model.SelectedLesson && x.Sender == "AI" && !x.Read))
        {
            try { await Run("aiRead", new { lessonId = model.SelectedLesson?.ToString() }); }
            catch (ApiException) { /* the badge simply stays */ }
            model = new AssistantModel(Ctx);
            model.Load();
        }
        return View(model);
    }

    [HttpPost("Assistant/Ask"), Page("assistant", Roles = "*")]
    public async Task<IActionResult> AssistantAsk(int? lesson, string text)
    {
        var page = "/Assistant" + (lesson == null ? "" : "?lesson=" + lesson);
        text = (text ?? "").Trim();
        if (text.Length == 0) return Go(page, "Type a question first.");
        await TryRun("ai", new { text, lessonId = lesson?.ToString() });
        return Redirect(page);
    }

    // ---- Messages: announcements and direct messages -----------------------------------------

    [HttpGet("Messages"), Page("messages", Roles = "*")]
    public async Task<IActionResult> Messages()
    {
        ViewData["Title"] = "Messages";
        var model = new MessagesModel(Ctx);
        model.Load();
        // Showing an announcement or a conversation reads it, so its badge goes away and stays away. Your own announcements are
        // never unread for you.
        var changed = false;
        try
        {
            if (model.Announcement != null && model.Announcement.PostedBy != Me.Id && !Data.HasRead(Me.Id, model.Announcement.Id))
            {
                await Run("announcementRead", new { id = model.Announcement.Id.ToString() });
                changed = true;
            }
            if (model.Selected != null && Ctx.QueryInt("id") != null && Data.Messages.Any(m => m.ReceiverId == Me.Id && m.SenderId == model.Selected.Id && !m.Read))
            {
                await Run("readMessages", new { from = model.SelectedId.ToString() });
                changed = true;
            }
        }
        catch (ApiException) { /* the badges simply stay */ }
        if (changed)
        {
            model = new MessagesModel(Ctx);
            model.Load();
        }
        return View(model);
    }

    [HttpPost("Messages/Send"), Page("messages", Roles = "*")]
    public async Task<IActionResult> MessageSend(int to, string text)
    {
        text = (text ?? "").Trim();
        if (text.Length == 0) return Back(fallback: "/Messages");
        await TryRun("message", new { to = to.ToString(), text });
        return Back(fallback: "/Messages");
    }

    [HttpPost("Messages/MarkRead"), Page("messages", Roles = "*")]
    public async Task<IActionResult> MessageMarkRead(int from)
    {
        var result = await TryRun("readMessages", new { from = from.ToString() });
        return result.Failed ? Back(fallback: "/Messages") : Back("Messages marked as read", "/Messages");
    }

    [HttpPost("Messages/Comment"), Page("messages", Roles = "*")]
    public async Task<IActionResult> MessageComment(int announcement, string comment)
    {
        comment = (comment ?? "").Trim();
        if (comment.Length == 0) return Back(fallback: "/Messages");
        var result = await TryRun("announcementComment", new { id = announcement.ToString(), comment });
        return result.Failed ? Back(fallback: "/Messages") : Back("Comment posted", "/Messages");
    }

    [HttpPost("Messages/DeleteComment"), Page("messages", Roles = "*")]
    public async Task<IActionResult> MessageDeleteComment(int announcement, int commentId)
    {
        var result = await TryRun("announcementCommentDelete", new { id = announcement.ToString(), commentId = commentId.ToString() });
        return result.Failed ? Back(fallback: "/Messages") : Back("Comment deleted", "/Messages");
    }

    [HttpPost("Messages/Publish"), Page("messages", Roles = "Instructor,Program Leader,Admin")]
    public async Task<IActionResult> MessagePublish(string courseId, string title, string body, int id)
    {
        var result = await TryRun("announcementSave", new { courseId, title = (title ?? "").Trim(), body = (body ?? "").Trim() });
        return result.Failed ? Back(fallback: "/Messages") : Go("/Messages?channel=" + courseId + (id != 0 ? "&id=" + id : ""), "Announcement published");
    }

    // ---- Live sessions -----------------------------------------------------------------------

    [HttpGet("LiveSessions"), Page("sessions", Roles = "*", Secondary = true)]
    public IActionResult LiveSessions()
    {
        ViewData["Title"] = "Live sessions";
        var model = new LiveSessionsModel(Ctx);
        model.Load();
        return View(model);
    }

    [HttpPost("LiveSessions/Schedule"), Page("sessions", Roles = "Instructor")]
    public async Task<IActionResult> Schedule(string courseId, string title, string startsAt)
    {
        var result = await TryRun("sessionSave", new { courseId, title = (title ?? "").Trim(), startsAt });
        return result.Failed ? Redirect("/LiveSessions") : Go("/LiveSessions", "Session scheduled");
    }

    [HttpPost("LiveSessions/Join"), Page("sessions", Roles = "*")]
    public async Task<IActionResult> Join(int id)
    {
        var result = await TryRun("sessionJoin", new { id = id.ToString() });
        return result.Failed ? Redirect("/LiveSessions") : Go("/LiveSessions", "Joined the session");
    }

    [HttpPost("LiveSessions/Leave"), Page("sessions", Roles = "*")]
    public async Task<IActionResult> Leave(int id)
    {
        var result = await TryRun("sessionJoin", new { id = id.ToString(), leave = true });
        return result.Failed ? Redirect("/LiveSessions") : Go("/LiveSessions", "Left the session");
    }
}
