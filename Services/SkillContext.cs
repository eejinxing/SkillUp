using System.Globalization;
using SkillUp.Helpers;
using SkillUp.Models;

namespace SkillUp.Services;

/// <summary>Which page frame the layout draws.</summary>
public enum PageLayout
{
    /// <summary>Visitors get the public frame, signed-in accounts the workspace frame.</summary>
    Auto,
    /// <summary>Signed-in workspace: .shell wrapper, capsule navigation, main.main[data-view].</summary>
    Workspace,
    /// <summary>Capsule navigation, main.public-main and the site footer, even for a signed-in account.</summary>
    Public,
    /// <summary>The landing page: everything inside .site-home.home-refined, with its own footer.</summary>
    Home
}

/// <summary>A page the signed-in account opened, remembered in the session for "back" links.</summary>
public sealed record NavVisit(string Path, string Url, string Title, int UserId, string Group);

/// <summary>One question to the learning companion and its reply.</summary>
public sealed class AiPair
{
    public string Prompt { get; set; }
    public string Response { get; set; }
    public string Date { get; set; }
    public DateTime PromptAt { get; set; }
    public DateTime ResponseAt { get; set; }
}

/// <summary>
/// Everything one request knows: the database snapshot, the signed-in account and the questions every page asks about them
/// (which courses they may open, how far a learner has got). Controllers and views reach it as Ctx.
/// </summary>
public sealed class SkillContext
{
    public const string Key = "SkillUp.Context";

    public SkillContext(Snapshot data, UserRow me, HttpContext http)
    {
        Data = data;
        Me = me;
        Http = http;
    }

    public Snapshot Data { get; set; }
    public UserRow Me { get; set; }
    public HttpContext Http { get; }
    /// <summary>The [Page] settings of the action being run.</summary>
    public PageAttribute Page { get; set; } = new PageAttribute("");

    /// <summary>The frame the layout draws: the page's own choice, otherwise the workspace for a signed-in account and the public frame for a visitor.</summary>
    public PageLayout Frame => Page.Layout != PageLayout.Auto ? Page.Layout : Me == null ? PageLayout.Public : PageLayout.Workspace;
    /// <summary>A message for the toast on this response (a refused save, or a notice kept from the previous page).</summary>
    public string Notice { get; set; }
    /// <summary>Values the person typed in a form the server refused, so the page can show them again.</summary>
    public Dictionary<string, string> Posted { get; set; } = new();

    /// <summary>What the person typed for a field of the last refused form, or the saved value when there is none.</summary>
    public string Typed(string field, string saved = "") => Posted.TryGetValue(field, out var value) ? value : saved ?? "";

    public bool IsStaff => Workspace.IsStaff(Me);

    public int? QueryInt(string key) =>
        int.TryParse(Http.Request.Query[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    public string Query(string key) => Http.Request.Query[key].ToString();

    HashSet<int> managed;
    /// <summary>Courses the signed-in account may edit (assigned instructor, program leader, or any for Admin).</summary>
    public HashSet<int> Managed => managed ??= Workspace.ManagedCourses(Data, Me);

    /// <summary>Courses this staff account looks after (Admin: all), in course order.</summary>
    public List<CourseRow> OwnCourses => Data.Courses.Where(c => Managed.Contains(c.Id)).ToList();

    /// <summary>Courses whose materials this account may open: enrolled ones for a learner, managed ones for staff.</summary>
    public List<CourseRow> AccessibleCourses => Me?.Role == "Learner"
        ? Data.Courses.Where(c => MyEnrollments.Any(e => e.CourseId == c.Id)).ToList()
        : OwnCourses;

    /// <summary>Id/Title pairs for a course drop-down list.</summary>
    public static IEnumerable<object> CourseOptions(IEnumerable<CourseRow> courses) => courses.Select(c => new { c.Id, c.Title }).ToList();

    /// <summary>The signed-in learner's enrolments.</summary>
    public List<EnrollmentRow> MyEnrollments => Me == null ? new List<EnrollmentRow>() : Data.Enrollments.Where(e => e.UserId == Me.Id).ToList();

    /// <summary>Courses this viewer may see: published ones, plus those they manage or are enrolled in.</summary>
    public IEnumerable<CourseRow> VisibleCourses
    {
        get
        {
            var enrolled = new HashSet<int>(MyEnrollments.Select(e => e.CourseId));
            return Data.Courses.Where(c => c.Status == "Published" || Managed.Contains(c.Id) || enrolled.Contains(c.Id));
        }
    }

    List<int> visibleOrder;

    /// <summary>The course's uploaded artwork, or one of the four stock covers chosen by its place in the course list.</summary>
    public string CourseArt(CourseRow course)
    {
        if (!string.IsNullOrEmpty(course.Thumbnail)) return course.Thumbnail.StartsWith("uploads/") ? "/" + course.Thumbnail : course.Thumbnail;
        visibleOrder ??= VisibleCourses.Select(c => c.Id).ToList();
        return "/assets/my-courses/computer-essentials-wide-0" + (Math.Max(0, visibleOrder.IndexOf(course.Id)) % 4 + 1) + ".webp";
    }

    /// <summary>Percentage of a course's activities this learner has completed.</summary>
    public int Progress(int userId, int courseId)
    {
        var total = Data.LessonsOf(courseId).Count;
        return total == 0 ? 0 : (int)Math.Round(100.0 * Data.CompletedLessons(userId, courseId).Count / total);
    }

    /// <summary>
    /// The account's conversation with the learning companion, oldest first: the one for an activity (lessonId), or the general
    /// one on the AI Assistant page. AI_CHAT_MESSAGE rows come in pairs: the prompt, then the reply.
    /// </summary>
    public List<AiPair> AiPairs(int? lessonId = null)
    {
        var pairs = new List<AiPair>();
        AiMessageRow prompt = null;
        foreach (var row in Data.AiMessages.Where(x => x.UserId == Me.Id && x.LessonId == lessonId))
        {
            if (row.Sender == "User") { prompt = row; continue; }
            if (prompt == null) continue;
            pairs.Add(new AiPair { Prompt = prompt.Content, Response = row.Content, Date = Ui.Date(prompt.CreatedAt), PromptAt = prompt.CreatedAt, ResponseAt = row.CreatedAt });
            prompt = null;
        }
        return pairs;
    }

    /// <summary>Rows of a learners table (course editor, student performance): person, course, completion and last activity.</summary>
    public List<object> LearnerLines(IEnumerable<EnrollmentRow> enrollments) => enrollments.Select(e =>
    {
        var u = Data.User(e.UserId);
        var p = Progress(e.UserId, e.CourseId);
        return (object)new
        {
            e.UserId, e.CourseId, Avatar = Ui.Avatar(u), Name = u?.Name ?? "Unknown learner", Email = u?.Email ?? "", Course = Data.Course(e.CourseId)?.Title ?? "",
            Progress = p, StatusKey = p == 100 ? "done" : p > 0 ? "progress" : "idle", Status = p == 100 ? "Completed" : p > 0 ? "In progress" : "Not started",
            LastActive = Ui.Date(e.LastActive ?? e.EnrolledAt)
        };
    }).ToList();

    public const string NavCurrent = "SkillUp.Nav.Current", NavPrevious = "SkillUp.Nav.Previous";

    public NavVisit ReadVisit(string key)
    {
        var json = Http.Session.GetString(key);
        return json == null ? null : System.Text.Json.JsonSerializer.Deserialize<NavVisit>(json);
    }

    /// <summary>
    /// The address and name of the page this one was opened from, for a "back" link. Pages whose path is in skip are passed over
    /// (a page that is a step inside this one), and when nothing suitable was remembered the fallback is used.
    /// </summary>
    public (string Url, string Label) BackTo(string fallbackUrl, string fallbackLabel, params string[] skip)
    {
        var previous = ReadVisit(NavPrevious);
        if (previous == null || Me == null || previous.UserId != Me.Id) return (fallbackUrl, fallbackLabel);
        var file = previous.Path.Trim('/');
        if (string.Equals(file, Http.Request.Path.Value?.Trim('/'), StringComparison.OrdinalIgnoreCase)) return (fallbackUrl, fallbackLabel);
        if (skip.Any(x => string.Equals(x, file, StringComparison.OrdinalIgnoreCase))) return (fallbackUrl, fallbackLabel);
        if (new[] { "Login", "Register", "ForgotPassword", "ResetPassword", "Home", "" }.Contains(file, StringComparer.OrdinalIgnoreCase)) return (fallbackUrl, fallbackLabel);
        var name = previous.Title.Replace("Skill UP · ", "").Trim();
        return (previous.Url, name.Length == 0 ? fallbackLabel : name);
    }
}
