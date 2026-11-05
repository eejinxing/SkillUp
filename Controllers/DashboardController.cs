using Microsoft.AspNetCore.Mvc;

namespace SkillUp.Controllers;

/// <summary>The signed-in role's overview page. Each role has its own view, built from its own model.</summary>
public sealed class DashboardController : SkillController
{
    [HttpGet("Dashboard"), Page("dashboard", Roles = "*")]
    public IActionResult Dashboard()
    {
        ViewData["Title"] = "Overview";
        Ctx.Page = new PageAttribute("dashboard") { Roles = "*", BodyClass = Me.Role == "Learner" ? "has-learning-console has-lc-exact has-velo-dashboard" : "" };
        switch (Me.Role)
        {
            case "Admin": { var m = new AdminDashboardModel(Ctx); m.Load(); return View("Admin", m); }
            case "Instructor": { var m = new InstructorDashboardModel(Ctx); m.Load(); return View("Instructor", m); }
            case "Program Leader": { var m = new LeaderDashboardModel(Ctx); m.Load(); return View("Leader", m); }
            default: { var m = new LearnerDashboardModel(Ctx); m.Draw(); return View("Learner", m); }
        }
    }

    /// <summary>Admin: the activity log as currently filtered (?q=&amp;role=&amp;source=&amp;days=), as a CSV download.</summary>
    [HttpGet("Dashboard/Export"), Page("dashboard", Roles = "Admin")]
    public IActionResult Export()
    {
        var model = new AdminDashboardModel(Ctx);
        model.Load();
        return Csv("skillup-user-activity.csv", new[] { new object[] { "User", "Role", "Category", "Action", "Details", "Timestamp (UTC)" } }
            .Concat(model.Filtered.Select(r => new object[] { r.Name, r.Role, r.Source, r.Action, r.Detail, Workspace.Iso(r.At) })));
    }

    /// <summary>Program Leader: mark an alert read, archive it or restore it. The inbox is kept in the leader's preferences.</summary>
    [HttpPost("Dashboard/Note"), Page("dashboard", Roles = "Program Leader")]
    public async Task<IActionResult> Note(string arg, string returnTo)
    {
        var model = new LeaderDashboardModel(Ctx);
        model.Load();
        var parts = (arg ?? "").Split('|', 2);
        if (parts.Length == 2)
        {
            if (parts[0] == "archive") model.archived.Add(parts[1]);
            else if (parts[0] == "restore") model.archived.Remove(parts[1]);
            model.read.Add(parts[1]);
            await TryRun("preferences", new { section = "leaderInbox", read = model.read.ToArray(), archived = model.archived.ToArray() });
        }
        return Redirect(SafeLocal(returnTo, "/Dashboard"));
    }
}
