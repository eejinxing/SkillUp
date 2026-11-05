using System.Collections.Specialized;
using SkillUp.Services;

namespace SkillUp.Models;

/// <summary>
/// Base class of the per-page view models. It offers what the old Web Forms SkillPage gave a page's code (Data, Me, Managed,
/// Progress, Request.QueryString ...) so the page logic could move here unchanged. A model computes everything its view draws,
/// from the request's SkillContext, and never writes: saving is done by a controller action through SkillController.TryRun.
/// </summary>
public abstract class PageModel
{
    protected PageModel(SkillContext ctx) => Ctx = ctx;

    public SkillContext Ctx { get; }
    public Snapshot Data => Ctx.Data;
    public UserRow Me => Ctx.Me;

    public bool IsStaff => Ctx.IsStaff;
    public HashSet<int> Managed => Ctx.Managed;
    public List<CourseRow> OwnCourses => Ctx.OwnCourses;
    public List<CourseRow> AccessibleCourses => Ctx.AccessibleCourses;
    public List<EnrollmentRow> MyEnrollments => Ctx.MyEnrollments;
    public IEnumerable<CourseRow> VisibleCourses => Ctx.VisibleCourses;
    public string CourseArt(CourseRow course) => Ctx.CourseArt(course);
    public int Progress(int userId, int courseId) => Ctx.Progress(userId, courseId);
    public List<AiPair> AiPairs(int? lessonId = null) => Ctx.AiPairs(lessonId);
    public List<object> LearnerLines(IEnumerable<EnrollmentRow> enrollments) => Ctx.LearnerLines(enrollments);
    public int? QueryInt(string key) => Ctx.QueryInt(key);
    public static IEnumerable<object> CourseOptions(IEnumerable<CourseRow> courses) => SkillContext.CourseOptions(courses);

    /// <summary>The page this one was opened from (see SkillContext.BackTo).</summary>
    public void BackTo(string fallbackUrl, string fallbackLabel, out string url, out string label, params string[] skip)
    {
        (url, label) = Ctx.BackTo(fallbackUrl, fallbackLabel, skip);
    }

    /// <summary>A model is only built to draw a page, never to handle a post.</summary>
    public bool IsPostBack => false;

    /// <summary>Request.QueryString["x"] and Request.RawUrl, as the page code used them.</summary>
    public RequestInfo Request => new(Ctx);

    public readonly struct RequestInfo(SkillContext ctx)
    {
        public QueryInfo QueryString => new(ctx);
        public string RawUrl => ctx.Http.Request.Path + ctx.Http.Request.QueryString;
        public CookieInfo Cookies => new(ctx);
    }

    public sealed class CookieValue(string value)
    {
        public string Value { get; } = value;
    }

    public readonly struct CookieInfo(SkillContext ctx)
    {
        /// <summary>The cookie, or null when the browser sent none (like HttpCookieCollection).</summary>
        public CookieValue this[string name] => ctx.Http.Request.Cookies.TryGetValue(name, out var value) ? new CookieValue(value) : null;
    }

    public readonly struct QueryInfo(SkillContext ctx)
    {
        /// <summary>The query value, or null when the address has none (like NameValueCollection).</summary>
        public string this[string key] => ctx.Http.Request.Query.TryGetValue(key, out var value) ? value.ToString() : null;
    }
}
