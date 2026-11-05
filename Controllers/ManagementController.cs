using Microsoft.AspNetCore.Mvc;

namespace SkillUp.Controllers;

/// <summary>Program Leader pages: instructor assignments, and the review of submitted courses and deletion requests.</summary>
public sealed class ManagementController : SkillController
{
    // ---- Instructor assignments --------------------------------------------------------------

    [HttpGet("Assignments"), Page("assignments", Roles = "Program Leader", Secondary = true)]
    public IActionResult Assignments()
    {
        ViewData["Title"] = "Instructor assignments";
        var model = new AssignmentsModel(Ctx);
        model.Load();
        return View(model);
    }

    [HttpPost("Assignments/Assign"), Page("assignments", Roles = "Program Leader")]
    public async Task<IActionResult> Assign(int courseId, string instructorId, string q)
    {
        var list = "/Assignments" + (string.IsNullOrEmpty(q) ? "" : "?q=" + Uri.EscapeDataString(q));
        var result = await TryRun("assign", new { courseId = courseId.ToString(), instructorId });
        return result.Failed ? Back(fallback: list) : Go(list, "Instructor assignment saved");
    }

    // ---- Programs and their learning paths ---------------------------------------------------

    [HttpGet("Programs"), Page("programs", Roles = "Program Leader")]
    public IActionResult Programs()
    {
        ViewData["Title"] = "Program Management";
        var model = new ProgramsModel(Ctx);
        model.Load();
        return View(model);
    }

    [HttpPost("Programs/Save"), Page("programs", Roles = "Program Leader")]
    public async Task<IActionResult> ProgramSave(int? id, string name, string description, string published)
    {
        var result = await TryRun("programSave", new { id = id?.ToString() ?? "", name, description, published = published == "true" });
        return result.Failed ? Back(fallback: "/Programs") : Go("/Programs?id=" + ((Newtonsoft.Json.Linq.JObject)result.Data)["id"], "Program saved");
    }

    [HttpPost("Programs/Delete"), Page("programs", Roles = "Program Leader")]
    public async Task<IActionResult> ProgramDelete(int? id)
    {
        var result = await TryRun("programDelete", new { id = id?.ToString() });
        return result.Failed ? Back(fallback: "/Programs") : Go("/Programs", "Program deleted");
    }

    /// <summary>The path being edited is kept in the session; every change reloads the editor so a refresh never repeats it.</summary>
    [HttpPost("Programs/Path"), Page("programs", Roles = "Program Leader")]
    public IActionResult ProgramPath(int programId, int course, string op)
    {
        var program = Data.Programs.FirstOrDefault(p => p.Id == programId && p.LeaderId == Me.Id);
        if (program == null) return Redirect("/Programs");
        var courses = new HashSet<int>(Data.Courses.Where(c => c.ProgramId == programId).Select(c => c.Id));
        var saved = Data.PathOf(programId).Where(courses.Contains).Distinct().ToList();
        var ids = (ProgramsModel.ReadDraft(Ctx, programId) ?? saved).Where(courses.Contains).Distinct().ToList();
        var i = ids.IndexOf(course);
        switch (op)
        {
            case "up": if (i > 0) { ids[i] = ids[i - 1]; ids[i - 1] = course; } break;
            case "down": if (i >= 0 && i < ids.Count - 1) { ids[i] = ids[i + 1]; ids[i + 1] = course; } break;
            case "remove": ids = ids.Where(x => x != course).ToList(); break;
            case "add": if (courses.Contains(course) && !ids.Contains(course)) ids.Add(course); break;
        }
        ProgramsModel.WriteDraft(Ctx, programId, ids);
        return Redirect("/Programs?id=" + programId);
    }

    [HttpPost("Programs/DiscardPath"), Page("programs", Roles = "Program Leader")]
    public IActionResult ProgramDiscardPath(int programId)
    {
        ProgramsModel.ClearDraft(Ctx, programId);
        return Go("/Programs?id=" + programId, "Path restored to the saved order.");
    }

    [HttpPost("Programs/SavePath"), Page("programs", Roles = "Program Leader")]
    public async Task<IActionResult> ProgramSavePath(int programId)
    {
        var program = Data.Programs.FirstOrDefault(p => p.Id == programId && p.LeaderId == Me.Id);
        if (program == null) return Redirect("/Programs");
        var courses = new HashSet<int>(Data.Courses.Where(c => c.ProgramId == programId).Select(c => c.Id));
        var ids = (ProgramsModel.ReadDraft(Ctx, programId) ?? Data.PathOf(programId)).Where(courses.Contains).Distinct().ToList();
        var result = await TryRun("pathSave", new { id = programId.ToString(), courseIds = ids.Select(x => x.ToString()) });
        if (result.Failed) return Redirect("/Programs?id=" + programId);
        ProgramsModel.ClearDraft(Ctx, programId);
        return Go("/Programs?id=" + programId, "Learning path saved");
    }

    [HttpPost("Programs/Move"), Page("programs", Roles = "Program Leader")]
    public async Task<IActionResult> ProgramMove(int programId, bool adding, int? movingId, string choice, string includeInPath)
    {
        var home = "/Programs?id=" + programId;
        var target = adding ? programId : int.Parse(choice ?? "0");
        if (ProgramsModel.ReadDraft(Ctx, programId) != null || ProgramsModel.ReadDraft(Ctx, target) != null)
            return Go(home, "Save or discard path changes before moving courses.");
        var result = await TryRun("programCourseMove", new
        {
            courseId = adding ? choice : movingId?.ToString(), programId = target.ToString(), includeInPath = includeInPath == "true"
        });
        return result.Failed ? Redirect(home) : Go(home, "Course moved");
    }

    // ---- Approvals board ---------------------------------------------------------------------

    [HttpGet("Approvals"), Page("approvals", Roles = "Program Leader")]
    public IActionResult Approvals()
    {
        ViewData["Title"] = "Course approvals";
        var model = new ApprovalsModel(Ctx);
        model.Load();
        return View(model);
    }

    [HttpPost("Approvals/Start"), Page("approvals", Roles = "Program Leader")]
    public async Task<IActionResult> ApprovalsStart(int id)
    {
        var result = await TryRun("courseRequestSeen", new { id = id.ToString() });
        return result.Failed ? Redirect("/Approvals?id=" + id) : Go("/Approvals?id=" + id, "Review started");
    }

    [HttpPost("Approvals/Decision"), Page("approvals", Roles = "Program Leader")]
    public async Task<IActionResult> ApprovalsDecision(int id, string decision, string comment)
    {
        var result = await TryRun("courseRequestReview", new { id = id.ToString(), decision, comment });
        return result.Failed ? Redirect("/Approvals?id=" + id) : Go("/Approvals?id=" + id, "Decision submitted");
    }

    // ---- Course review -----------------------------------------------------------------------

    [HttpGet("CourseReview"), Page("review", Roles = "Program Leader")]
    public IActionResult CourseReview()
    {
        if (Ctx.QueryInt("id") == null) return Redirect("/Approvals");
        ViewData["Title"] = "Review submission";
        var model = new CourseReviewModel(Ctx);
        model.Load();
        return View(model);
    }

    [HttpPost("CourseReview/Start"), Page("review", Roles = "Program Leader")]
    public async Task<IActionResult> CourseReviewStart(int id)
    {
        var pending = Data.Requests.FirstOrDefault(r => r.CourseId == id && r.Kind == "Create" && r.Status == "Pending review");
        var page = "/CourseReview?id=" + id;
        if (pending == null) return Redirect(page);
        var result = await TryRun("courseRequestSeen", new { id = pending.Id.ToString() });
        return result.Failed ? Redirect(page) : Go(page, "Review started");
    }

    [HttpPost("CourseReview/Decision"), Page("review", Roles = "Program Leader")]
    public async Task<IActionResult> CourseReviewDecision(int id, string decision, string comment)
    {
        var result = await TryRun("review", new { courseId = id.ToString(), decision, comment });
        return result.Failed ? Redirect("/CourseReview?id=" + id) : Go("/CourseReview?id=" + id, "Decision submitted");
    }

    // ---- Deletion request review -------------------------------------------------------------

    [HttpGet("RequestReview"), Page("request-review", Roles = "Program Leader")]
    public IActionResult RequestReview()
    {
        if (Ctx.QueryInt("id") == null) return Redirect("/Approvals");
        ViewData["Title"] = "Review deletion request";
        var model = new RequestReviewModel(Ctx);
        model.Load();
        return View(model);
    }

    [HttpPost("RequestReview/Start"), Page("request-review", Roles = "Program Leader")]
    public async Task<IActionResult> RequestReviewStart(int id)
    {
        var result = await TryRun("courseRequestSeen", new { id = id.ToString() });
        return result.Failed ? Redirect("/RequestReview?id=" + id) : Go("/RequestReview?id=" + id, "Review started");
    }

    [HttpPost("RequestReview/Decision"), Page("request-review", Roles = "Program Leader")]
    public async Task<IActionResult> RequestReviewDecision(int id, string decision, string comment)
    {
        var result = await TryRun("courseRequestReview", new { id = id.ToString(), decision, comment });
        return result.Failed ? Redirect("/RequestReview?id=" + id) : Go("/RequestReview?id=" + id, "Decision submitted");
    }
}
