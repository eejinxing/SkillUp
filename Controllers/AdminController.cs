using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace SkillUp.Controllers;

/// <summary>Administrator pages: the user account directory and the account editor.</summary>
public sealed class AdminController : SkillController
{
    // ---- User accounts -----------------------------------------------------------------------

    [HttpGet("Users"), Page("users", Roles = "Admin")]
    public IActionResult Users()
    {
        ViewData["Title"] = "User accounts";
        var model = new UsersModel(Ctx);
        model.Load();
        return View(model);
    }

    [HttpPost("Users/Approve"), Page("users", Roles = "Admin")]
    public async Task<IActionResult> UserApprove(int id)
    {
        var target = Data.User(id);
        if (target == null) return Redirect("/Users");
        var result = await TryRun("userSave", new { id = id.ToString(), name = target.Name, email = target.Email, username = target.Username, bio = target.Bio ?? "", role = target.Role, status = "Active" });
        return result.Failed ? Redirect("/Users") : Go("/Users", "User account approved");
    }

    [HttpPost("Users/Delete"), Page("users", Roles = "Admin")]
    public async Task<IActionResult> UserDelete(int id)
    {
        var result = await TryRun("userDelete", new { id = id.ToString() });
        return result.Failed ? Redirect("/Users") : Go("/Users", "User account deleted");
    }

    // ---- Account editor ----------------------------------------------------------------------

    [HttpGet("UserEditor"), Page("users", Roles = "Admin")]
    public IActionResult UserEditor()
    {
        ViewData["Title"] = "User account";
        var model = new UserEditorModel(Ctx);
        model.Load();
        return View(model);
    }

    [HttpPost("UserEditor/Create"), Page("users", Roles = "Admin")]
    public async Task<IActionResult> UserCreate(string name, string email, string username, string bio, string role, string status, string password)
    {
        var result = await TryRun("userSave", new { id = "", name = (name ?? "").Trim(), email = (email ?? "").Trim(), username = (username ?? "").Trim(), bio, role, status, password });
        return result.Failed ? Redirect("/UserEditor?id=new") : Go("/UserEditor?id=" + ((JObject)result.Data)["id"], "User account created");
    }

    [HttpPost("UserEditor/Save"), Page("users", Roles = "Admin")]
    public async Task<IActionResult> UserSave(int id, string name, string email, string username, string bio, string role, string status)
    {
        var result = await TryRun("userSave", new { id = id.ToString(), name = (name ?? "").Trim(), email = (email ?? "").Trim(), username = (username ?? "").Trim(), bio, role, status });
        return result.Failed ? Redirect("/UserEditor?id=" + id) : Go("/UserEditor?id=" + id, "User account saved");
    }

    [HttpPost("UserEditor/Status"), Page("users", Roles = "Admin")]
    public async Task<IActionResult> UserStatus(int id)
    {
        var u = Data.User(id);
        if (u == null) return Redirect("/Users");
        var activate = u.Status != "Active";
        var result = await TryRun("userSave", new { id = id.ToString(), name = u.Name, email = u.Email, username = u.Username, bio = u.Bio ?? "", role = u.Role, status = activate ? "Active" : "Inactive" });
        return result.Failed ? Redirect("/UserEditor?id=" + id) : Go("/UserEditor?id=" + id, activate ? "Account activated" : "Account deactivated");
    }

    [HttpPost("UserEditor/Transfer"), Page("users", Roles = "Admin")]
    public async Task<IActionResult> UserTransfer(int id, string what, string toId)
    {
        var result = await TryRun("staffTransfer", new { fromId = id.ToString(), toId, what });
        if (result.Failed) return Redirect("/UserEditor?id=" + id);
        var r = (JObject)result.Data;
        return Go("/UserEditor?id=" + id, Ui.Plural((int)r["moved"], what == "courses" ? "course" : "program") + " transferred to " + (string)r["to"]);
    }

    [HttpPost("UserEditor/Delete"), Page("users", Roles = "Admin")]
    public async Task<IActionResult> UserRemove(int id)
    {
        var result = await TryRun("userDelete", new { id = id.ToString() });
        return result.Failed ? Redirect("/UserEditor?id=" + id) : Go("/Users", "User account deleted");
    }
}
