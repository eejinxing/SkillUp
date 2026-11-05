using Microsoft.AspNetCore.Mvc;

namespace SkillUp.Controllers;

/// <summary>Uploading and deleting the files attached to a course (shown by Views/Shared/_CourseFiles.cshtml).</summary>
public sealed class FilesController : SkillController
{
    static readonly Dictionary<string, string> Types = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf", [".txt"] = "text/plain", [".png"] = "image/png", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".webp"] = "image/webp"
    };

    [HttpPost("Files/Upload"), Page("files", Roles = "Instructor,Program Leader,Admin"), RequestSizeLimit(8 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, string courseId, string returnTo)
    {
        var back = SafeLocal(returnTo, "/Resources");
        if (file == null || file.Length == 0) return Go(back, "Choose a file to upload.");
        if (!Types.TryGetValue(Path.GetExtension(file.FileName), out var type)) return Go(back, "Supported files: PDF, TXT, PNG, JPG and WebP.");
        if (file.Length > 2 * 1024 * 1024) return Go(back, "Files must be smaller than 2 MB.");
        using var memory = new MemoryStream();
        await file.CopyToAsync(memory);
        var payload = new
        {
            courseId,
            name = Path.GetFileName(file.FileName),
            size = memory.Length,
            dataUrl = "data:" + type + ";base64," + Convert.ToBase64String(memory.ToArray())
        };
        var result = await TryRun("resourceSave", payload);
        return result.Failed ? Redirect(back) : Go(back, "File uploaded. The course is back in Draft for review.");
    }

    [HttpPost("Files/Delete"), Page("files", Roles = "Instructor,Program Leader,Admin")]
    public async Task<IActionResult> Delete(int id, string returnTo)
    {
        var result = await TryRun("resourceDelete", new { id = id.ToString() });
        var back = SafeLocal(returnTo, "/Resources");
        return result.Failed ? Redirect(back) : Go(back, "File deleted.");
    }
}
