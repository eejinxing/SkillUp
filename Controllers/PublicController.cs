using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace SkillUp.Controllers;

/// <summary>Pages anyone can open: the landing page, the course catalogue, static information pages and the enquiry portal.</summary>
public sealed class PublicController : SkillController
{
    // ---- landing page and static pages --------------------------------------------------------

    [HttpGet("/"), HttpGet("Home"), Page("home", Layout = PageLayout.Home, BodyClass = "ui-home smooth-scroll")]
    public IActionResult Home() => View();

    [HttpGet("About"), Page("about", Layout = PageLayout.Public)]
    public IActionResult About() => View();

    [HttpGet("Privacy"), Page("privacy", Layout = PageLayout.Public)]
    public IActionResult Privacy() => View();

    [HttpGet("Terms"), Page("terms", Layout = PageLayout.Public)]
    public IActionResult Terms() => View();

    public sealed record FaqEntry(string Question, string Answer);

    [HttpGet("FAQ"), Page("faq", Layout = PageLayout.Public)]
    public IActionResult Faq() => View("FAQ", new FaqEntry[]
    {
        new("How do I start learning?", "Create a learner account, choose a published course and enrol. Your progress is saved as you complete activities."),
        new("Can I learn at my own pace?", "Yes. Work through readings, practical labs and quizzes in order. Course deadlines help you plan your schedule."),
        new("How do I earn a certificate?", "Complete every activity and pass each quiz with at least 70%. Your certificate then appears in Certificates."),
        new("Who reviews the courses?", "Instructors prepare the content. A Program Leader reviews it before learners can enrol.")
    });

    public sealed record InstructorCard(UserRow User, string Bio, int Published);

    [HttpGet("Instructors"), Page("instructors", Layout = PageLayout.Public)]
    public IActionResult Instructors()
    {
        // Visitors see the instructors behind published courses; signed-in accounts see every active instructor.
        var team = Data.Users
            .Where(u => u.Role == "Instructor" && u.Status == "Active")
            .Select(u => new InstructorCard(u,
                string.IsNullOrWhiteSpace(u.Bio) ? "Practical learning guided by an experienced instructor." : u.Bio,
                Data.Courses.Count(c => c.InstructorId == u.Id && c.Status == "Published")))
            .Where(x => Me != null || x.Published > 0)
            .ToList();
        return View(team);
    }

    [HttpGet("Help"), Page("help", Secondary = true, BodyClass = "ui-home")]
    public IActionResult Help() => Me == null ? Redirect(Routes.HomeContact) : View();

    // ---- catalogue ----------------------------------------------------------------------------

    static readonly string[] LevelNames = ["All levels", "Beginner", "Intermediate", "Advanced"];

    public sealed record LevelChip(string Level, string Label, int Count, bool Pressed);

    public sealed class CatalogModel
    {
        public int Published, EnrolledCount, ActivityCount;
        public string Term;
        public List<CourseRow> Shown;
        public List<LevelChip> Levels;
    }

    /// <summary>Published courses, filtered by level and a search over title, description and instructor (?level=&amp;q=).</summary>
    [HttpGet("Catalog"), Page("catalog", Secondary = true)]
    public IActionResult Catalog(string level, string q)
    {
        level = LevelNames.Contains(level) ? level : "All levels";
        var term = (q ?? "").Trim();
        var published = Data.Courses.Where(c => c.Status == "Published").ToList();
        // Words that start with what was typed; titles that start with it come first.
        var shown = published
            .Where(c => (level == "All levels" || c.Difficulty == level)
                && SearchMatch.Matches(term, c.Title + " " + Data.User(c.InstructorId)?.Name, c.Description, true))
            .OrderBy(c => SearchMatch.Rank(term, c.Title)).ToList();
        return View(new CatalogModel
        {
            Published = published.Count,
            ActivityCount = published.Sum(c => Data.LessonsOf(c.Id).Count),
            EnrolledCount = published.Count(c => Ctx.MyEnrollments.Any(x => x.CourseId == c.Id)),
            Term = term,
            Shown = shown,
            Levels = LevelNames.Select(l => new LevelChip(l, l == "All levels" ? "All" : l,
                l == "All levels" ? published.Count : published.Count(c => c.Difficulty == l), l == level)).ToList()
        });
    }

    // ---- enquiries ----------------------------------------------------------------------------

    /// <summary>The contact form on the landing page and the Help page.</summary>
    [HttpPost("Enquiry")]
    public async Task<IActionResult> Enquiry(string returnTo, string topic, string name, string email, string subject, string message)
    {
        var page = SafeLocal(returnTo, "/Home");
        // Picking a topic is enough for the subject.
        subject = string.IsNullOrWhiteSpace(subject) ? topic : subject.Trim();
        if (string.IsNullOrEmpty(subject))
        {
            Flash("Add a short subject so we can route your message.");
            return Redirect(page + "#home-enquiry");
        }
        var result = await TryRun("enquirySave", new { name = (name ?? "").Trim(), email = (email ?? "").Trim(), subject, message = (message ?? "").Trim() });
        return Redirect(result.Failed ? page + "#home-enquiry" : page + "?enquirySaved=1#home-enquiry");
    }

    public sealed class EnquiryPortalModel
    {
        public List<EnquiryRow> Mine;
        public EnquiryRow Selected;
        /// <summary>Right after a new enquiry was sent: the saved message replaces the form.</summary>
        public bool Saved, ConfirmDelete;
    }

    /// <summary>Enquiries this browser or account may open, newest first. The rule lives in the enquiryList command.</summary>
    async Task<List<EnquiryRow>> Mine()
    {
        var ids = new HashSet<int>(((IEnumerable<JObject>)await Run("enquiryList")).Select(q => int.Parse((string)q["id"])));
        return Data.Enquiries.Where(q => ids.Contains(q.Id)).OrderByDescending(q => q.CreatedAt).ThenByDescending(q => q.Id).ToList();
    }

    /// <summary>The visitor's enquiries: those linked to this browser (ENQUIRY_ACCESS) or sent from the signed-in account.</summary>
    [HttpGet("EnquiryPortal"), Page("enquiries")]
    public async Task<IActionResult> EnquiryPortal(int? id, bool saved, bool confirmDelete)
    {
        ViewData["Title"] = "Your enquiries.";
        var mine = await Mine();
        var selected = id == null ? null : mine.FirstOrDefault(q => q.Id == id);
        return View(new EnquiryPortalModel { Mine = mine, Selected = selected, Saved = saved && selected != null, ConfirmDelete = confirmDelete && selected != null });
    }

    [HttpPost("EnquiryPortal/Save")]
    public async Task<IActionResult> EnquirySave(int? id, string topic, string name, string email, string subject, string message)
    {
        // Like the landing page form: picking a topic is enough for the subject of a new enquiry.
        subject = (subject ?? "").Trim();
        if (subject.Length == 0 && id == null) subject = topic ?? "";
        var self = id == null ? "/EnquiryPortal" : "/EnquiryPortal?id=" + id;
        if (subject.Length == 0)
        {
            Flash("Add a short subject so we can route your message.");
            return Redirect(self);
        }
        var result = await TryRun("enquirySave", new { id = id?.ToString(), name = (name ?? "").Trim(), email = (email ?? "").Trim(), subject, message = (message ?? "").Trim() });
        if (result.Failed) return Redirect(self);
        var saved = int.Parse((string)((JObject)result.Data)["id"]);
        if (id == null) return Redirect("/EnquiryPortal?id=" + saved + "&saved=1");
        return Go("/EnquiryPortal?id=" + saved, "Changes saved.");
    }

    [HttpPost("EnquiryPortal/Delete")]
    public async Task<IActionResult> EnquiryDelete(int id)
    {
        var result = await TryRun("enquiryDelete", new { id = id.ToString() });
        return result.Failed ? Redirect("/EnquiryPortal?id=" + id) : Go("/EnquiryPortal", "Enquiry deleted.");
    }
}
