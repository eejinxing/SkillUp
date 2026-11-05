using Microsoft.AspNetCore.Mvc;

namespace SkillUp.Controllers;

/// <summary>Instructor and Program Leader pages: the courses they manage, their learning resources and analytics.</summary>
public sealed class TeachingController : SkillController
{
    [HttpGet("Courses"), Page("courses", Roles = "Instructor,Program Leader", Secondary = true)]
    public IActionResult Courses()
    {
        ViewData["Title"] = "My courses";
        var model = new CoursesModel(Ctx);
        model.Load();
        return View(model);
    }

    // ---- Course editor -----------------------------------------------------------------------

    [HttpGet("CourseEditor"), Page("course-edit", Roles = "Instructor,Program Leader")]
    public IActionResult CourseEditor()
    {
        ViewData["Title"] = "Course editor";
        var model = new CourseEditorModel(Ctx);
        model.Load();
        model.Draw();
        return View(model);
    }

    static string EditorPage(int id) => "/CourseEditor?id=" + id;

    [HttpPost("CourseEditor/Save"), Page("course-edit", Roles = "Instructor,Program Leader")]
    public async Task<IActionResult> CourseSave(int? id, string title, string programId, string deadline, string difficulty, string description, string outcomes, string duration, string startDate)
    {
        var result = await TryRun("courseSave", new { id = id?.ToString() ?? "", title, programId, deadline, difficulty, description, outcomes, duration, startDate });
        if (result.Failed) return Back(fallback: "/Courses");
        return Go("/CourseEditor?id=" + ((Newtonsoft.Json.Linq.JObject)result.Data)["id"], id == null ? "Course created" : "Course saved");
    }

    [HttpPost("CourseEditor/Action"), Page("course-edit", Roles = "Instructor,Program Leader")]
    public async Task<IActionResult> CourseAction(int id, string op)
    {
        var result = await TryRun("courseAction", new { id = id.ToString(), action = op });
        if (result.Failed) return Redirect(EditorPage(id));
        if (op == "delete") return Go("/Courses", "Course deleted");
        return Go(EditorPage(id), op == "publish" ? "Course published" : op == "submit" ? "Course submitted for review" : op == "withdraw" ? "Submission withdrawn" : "Course hidden");
    }

    [HttpPost("CourseEditor/Assign"), Page("course-edit", Roles = "Program Leader")]
    public async Task<IActionResult> CourseAssign(int id, string instructorId)
    {
        var result = await TryRun("assign", new { courseId = id.ToString(), instructorId });
        return result.Failed ? Redirect(EditorPage(id)) : Go(EditorPage(id), "Instructor assignment saved");
    }

    [HttpPost("CourseEditor/Lesson"), Page("course-edit", Roles = "Instructor,Program Leader")]
    public async Task<IActionResult> CourseLesson(int courseId, int? id, string title, string type, string minutes, string body, string answer, string workspace, string starterCode, string preview)
    {
        var result = await TryRun("lessonSave", new
        {
            courseId = courseId.ToString(), id = id?.ToString() ?? "", title, type, minutes, body, answer, workspace, starterCode,
            preview = preview == "true" && type == "article"
        });
        return result.Failed ? Back(fallback: EditorPage(courseId)) : Go(EditorPage(courseId), "Activity saved");
    }

    [HttpPost("CourseEditor/LessonDelete"), Page("course-edit", Roles = "Instructor,Program Leader")]
    public async Task<IActionResult> CourseLessonDelete(int courseId, int id)
    {
        var result = await TryRun("lessonDelete", new { courseId = courseId.ToString(), id = id.ToString() });
        return result.Failed ? Redirect(EditorPage(courseId)) : Go(EditorPage(courseId), "Activity deleted");
    }

    [HttpPost("CourseEditor/Deletion"), Page("course-edit", Roles = "Instructor")]
    public async Task<IActionResult> CourseDeletion(int id, string reason)
    {
        var result = await TryRun("courseRequest", new { kind = "Delete", courseId = id.ToString(), reason });
        return result.Failed ? Back(fallback: EditorPage(id)) : Go("/CourseRequests?id=" + ((Newtonsoft.Json.Linq.JObject)result.Data)["id"], "Deletion request submitted");
    }

    // ---- course image (a file under wwwroot/uploads/course-art) --------------------------------

    string CoverPath(string name) => Path.Combine(HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootPath, CourseEditorModel.CoverFolder.Replace('/', Path.DirectorySeparatorChar), name);

    /// <summary>Deletes a cover file this page saved earlier. Anything else in the database value is left alone.</summary>
    void DeleteCover(string path)
    {
        if (string.IsNullOrEmpty(path) || !path.StartsWith(CourseEditorModel.CoverFolder, StringComparison.Ordinal)) return;
        try { System.IO.File.Delete(CoverPath(Path.GetFileName(path))); } catch (IOException) { }
    }

    bool CourseLocked(CourseRow c) => c.Status == "Pending review" || Data.Requests.Any(r => r.CourseId == c.Id && r.Kind == "Delete" && r.Status == "Pending review");

    [HttpPost("CourseEditor/Cover"), Page("course-edit", Roles = "Instructor,Program Leader"), RequestSizeLimit(4 * 1024 * 1024)]
    public async Task<IActionResult> CourseCover(int id, IFormFile file)
    {
        var course = Ctx.OwnCourses.FirstOrDefault(c => c.Id == id);
        if (course == null || CourseLocked(course)) return Redirect(EditorPage(id));
        if (file == null || file.Length == 0) return Go(EditorPage(id), "Choose a PNG, JPG or WebP image.");
        if (file.Length > 2 * 1024 * 1024) return Go(EditorPage(id), "Course images must be smaller than 2 MB.");
        using var memory = new MemoryStream();
        await file.CopyToAsync(memory);
        var bytes = memory.ToArray();
        var extension = CourseEditorModel.CoverExtension(bytes);
        if (extension == null) return Go(EditorPage(id), "Choose a PNG, JPG or WebP image.");
        var name = Tokens.New() + extension;
        Directory.CreateDirectory(Path.GetDirectoryName(CoverPath(name)));
        await System.IO.File.WriteAllBytesAsync(CoverPath(name), bytes);
        var result = await TryRun("courseArt", new { courseId = id.ToString(), path = CourseEditorModel.CoverFolder + name });
        if (result.Failed)
        {
            System.IO.File.Delete(CoverPath(name));
            return Redirect(EditorPage(id));
        }
        DeleteCover((string)((Newtonsoft.Json.Linq.JObject)result.Data)?["previous"]);
        return Go(EditorPage(id), "Course image updated");
    }

    [HttpPost("CourseEditor/CoverRemove"), Page("course-edit", Roles = "Instructor,Program Leader")]
    public async Task<IActionResult> CourseCoverRemove(int id)
    {
        var course = Ctx.OwnCourses.FirstOrDefault(c => c.Id == id);
        if (course == null || CourseLocked(course)) return Redirect(EditorPage(id));
        var result = await TryRun("courseArt", new { courseId = id.ToString(), path = "" });
        if (result.Failed) return Redirect(EditorPage(id));
        DeleteCover((string)((Newtonsoft.Json.Linq.JObject)result.Data)?["previous"]);
        return Go(EditorPage(id), "Course image removed");
    }

    [HttpPost("Teaching/RemoveEnrolment"), Page("learners", Roles = "Instructor")]
    public async Task<IActionResult> RemoveEnrolment(int userId, int courseId, string returnTo)
    {
        var result = await TryRun("enrollmentSave", new { courseId = courseId.ToString(), userId = userId.ToString(), remove = true });
        var back = SafeLocal(returnTo, "/Learners");
        return result.Failed ? Redirect(back) : Go(back, "Enrolment removed");
    }

    // ---- Learners ----------------------------------------------------------------------------

    [HttpGet("Learners"), Page("learners", Roles = "Instructor,Program Leader")]
    public IActionResult Learners()
    {
        ViewData["Title"] = "Learners";
        var model = new LearnersModel(Ctx);
        model.Load();
        model.Draw();
        return View(model);
    }

    [HttpPost("Learners/Toggle"), Page("learners", Roles = "Instructor,Program Leader")]
    public IActionResult LearnerToggle(int id, string returnTo)
    {
        var collapsed = new LearnersModel(Ctx).Collapsed();
        if (!collapsed.Remove(id)) collapsed.Add(id);
        HttpContext.Session.SetString(LearnersModel.CollapsedKey, string.Join(",", collapsed));
        return Redirect(SafeLocal(returnTo, "/Learners") + "#sp-card-" + id);
    }

    [HttpPost("Learners/Enrol"), Page("learners", Roles = "Instructor")]
    public async Task<IActionResult> LearnerEnrol(string courseId, string userId)
    {
        var result = await TryRun("enrollmentSave", new { courseId, userId });
        return result.Failed ? Back(fallback: "/Learners") : Go("/Learners?course=" + courseId, "Learner enrolled");
    }

    // ---- My requests -------------------------------------------------------------------------

    [HttpGet("CourseRequests"), Page("requests", Roles = "Instructor")]
    public IActionResult CourseRequests()
    {
        ViewData["Title"] = "My requests";
        var model = new CourseRequestsModel(Ctx);
        model.Load();
        return View(model);
    }

    /// <summary>Exports the ticked rows, or every request that matches the filters when none is ticked.</summary>
    [HttpGet("CourseRequests/Export"), Page("requests", Roles = "Instructor")]
    public IActionResult CourseRequestsExport()
    {
        var model = new CourseRequestsModel(Ctx);
        model.Load();
        return Csv("skillup-course-requests.csv", model.ExportRows(Request.Query["rq-select"].ToArray()));
    }

    [HttpPost("CourseRequests/Send"), Page("requests", Roles = "Instructor")]
    public async Task<IActionResult> CourseRequestSend(string kind, string courseId, string reason)
    {
        var result = await TryRun("courseRequest", new { kind, courseId, reason });
        return result.Failed ? Redirect("/CourseRequests?new=1") : Go("/CourseRequests?id=" + ((Newtonsoft.Json.Linq.JObject)result.Data)["id"], "Request submitted");
    }

    [HttpPost("CourseRequests/Withdraw"), Page("requests", Roles = "Instructor")]
    public async Task<IActionResult> CourseRequestWithdraw(int id)
    {
        var result = await TryRun("courseRequestWithdraw", new { id = id.ToString() });
        return result.Failed ? Redirect("/CourseRequests?id=" + id) : Go("/CourseRequests?id=" + id, "Request withdrawn");
    }

    // ---- Question editor ---------------------------------------------------------------------

    [HttpGet("QuizEditor"), Page("quiz-editor", Roles = "Instructor,Program Leader", Group = "course-edit")]
    public IActionResult QuizEditor()
    {
        ViewData["Title"] = "Question editor";
        var model = new QuizEditorModel(Ctx);
        model.Load();
        model.Draw();
        return View(model);
    }

    /// <summary>The quiz as the browser posted it: settings, each question's text, options, topic and chosen answer.</summary>
    QuizEditorModel.QuizDraft PostedDraft()
    {
        var form = Request.Form;
        var draft = new QuizEditorModel.QuizDraft { TimeLimit = form["timeLimit"].ToString().Trim(), Published = form["published"] == "true" };
        for (var i = 0; form.ContainsKey("q" + i + ".id") || form.ContainsKey("q" + i + ".text"); i++)
        {
            var question = new QuizEditorModel.QuestionDraft
            {
                Id = form["q" + i + ".id"], Text = form["q" + i + ".text"], Topic = form["q" + i + ".topic"],
                Answer = int.TryParse(form["q" + i + ".answer"], out var answer) ? answer : -1
            };
            for (var j = 0; form.ContainsKey("q" + i + ".option" + j); j++) question.Options.Add(form["q" + i + ".option" + j]);
            draft.Questions.Add(question);
        }
        return draft;
    }

    [HttpPost("QuizEditor/Edit"), Page("quiz-editor", Roles = "Instructor,Program Leader", Group = "course-edit")]
    public async Task<IActionResult> QuizEdit(int id, string op, string ask)
    {
        var page = "/QuizEditor?id=" + id;
        var course = Ctx.OwnCourses.FirstOrDefault(c => c.Id == id);
        if (course == null || CourseLocked(course)) return Redirect(page);
        var draft = PostedDraft();
        var parts = (op ?? "").Split(':');
        int Arg(int n) => n < parts.Length && int.TryParse(parts[n], out var v) ? v : -1;
        switch (parts[0])
        {
            case "addQuestion":
                draft.Questions.Add(new QuizEditorModel.QuestionDraft { Options = ["", "", ""] });
                break;
            case "sample":
                draft.Questions.Add(new QuizEditorModel.QuestionDraft
                {
                    Text = "Which approach is best when learning " + course.Title + "?", Topic = "Practical learning", Answer = 0,
                    Options = ["Practise with a small example", "Skip validation", "Memorise without practising"]
                });
                Flash("Sample draft added. Review it before publishing.");
                break;
            case "removeQuestion":
                if (Arg(1) >= 0 && Arg(1) < draft.Questions.Count) draft.Questions.RemoveAt(Arg(1));
                break;
            case "addOption":
                if (Arg(1) >= 0 && Arg(1) < draft.Questions.Count && draft.Questions[Arg(1)].Options.Count < 6) draft.Questions[Arg(1)].Options.Add("");
                break;
            case "removeOption":
                if (Arg(1) >= 0 && Arg(1) < draft.Questions.Count)
                {
                    var q = draft.Questions[Arg(1)];
                    var j = Arg(2);
                    if (q.Options.Count > 2 && j >= 0 && j < q.Options.Count)
                    {
                        q.Options.RemoveAt(j);
                        if (q.Answer == j) q.Answer = -1;
                        else if (q.Answer > j) q.Answer--;
                    }
                }
                break;
            case "hint":
                QuizEditorModel.KeepDraft(Ctx, id, draft);
                return Redirect(page + "&hint=" + Arg(1));
            case "discard":
                QuizEditorModel.ForgetDraft(Ctx, id);
                return Redirect(page);
            case "deleteQuiz":
            {
                var deleted = await TryRun("quizDelete", new { courseId = id.ToString() });
                if (deleted.Failed) break;
                QuizEditorModel.ForgetDraft(Ctx, id);
                return Go(page, "Quiz deleted");
            }
            case "save":
            {
                var problem = QuizEditorModel.Problem(draft);
                if (problem != null) { Flash(problem); break; }
                var saved = await TryRun("quizSave", new
                {
                    courseId = id.ToString(), timeLimit = draft.TimeLimit, published = draft.Published,
                    questions = draft.Questions.Select(q => new { id = q.Id, text = q.Text, options = q.Options, answer = q.Answer, topic = q.Topic })
                });
                if (saved.Failed) break;
                QuizEditorModel.ForgetDraft(Ctx, id);
                return Go(page, "Quiz saved");
            }
            case "useContext":
            {
                var question = draft.Questions.FirstOrDefault(q => !string.IsNullOrWhiteSpace(q.Text));
                var text = "Help me write clear answer options for " + (question?.Text.Trim() ?? course.Title) + ".";
                HttpContext.Session.SetString(QuizEditorModel.AskKey, text.Length > 1000 ? text[..1000] : text);
                break;
            }
            case "ask":
            {
                ask = (ask ?? "").Trim();
                if (ask.Length == 0) { Flash("Type a question first."); break; }
                // The quiz draft is kept in the session, so the reload keeps unsaved questions.
                QuizEditorModel.KeepDraft(Ctx, id, draft);
                await TryRun("ai", new { text = ask });
                return Redirect(page);
            }
        }
        QuizEditorModel.KeepDraft(Ctx, id, draft);
        return Redirect(page);
    }

    [HttpGet("Resources"), Page("resources", Roles = "Instructor", Secondary = true)]
    public IActionResult Resources()
    {
        ViewData["Title"] = "Learning resources";
        var model = new ResourcesModel(Ctx);
        model.Load();
        return View(model);
    }

    [HttpGet("Analytics"), Page("analytics", Roles = "Instructor,Program Leader", Secondary = true)]
    public IActionResult Analytics()
    {
        ViewData["Title"] = "Learning analytics";
        var model = new AnalyticsModel(Ctx);
        model.Load();
        return View(model);
    }

    [HttpGet("Analytics/Export"), Page("analytics", Roles = "Instructor,Program Leader")]
    public IActionResult AnalyticsExport()
    {
        var model = new AnalyticsModel(Ctx);
        model.Load();
        return Csv("skillup-analytics.csv", model.ExportRows());
    }
}
