using Newtonsoft.Json;

namespace SkillUp.Helpers;

/// <summary>Addresses that several pages link to.</summary>
public static class Routes
{
    /// <summary>The enquiry form near the end of the landing page.</summary>
    public const string HomeContact = "/Home#home-enquiry";

    /// <summary>The "What You'll Learn" section of the landing page, where visitors browse courses instead of the catalogue.</summary>
    public const string HomeCourses = "/Home#discover";

    /// <summary>Where Help links go: visitors to the landing page's enquiry form, signed-in accounts to the Help page.</summary>
    public static string Help(UserRow me) => me == null ? HomeContact : "/Help";
}

/// <summary>The navigation bar and the data the call preview script needs.</summary>
public static class Nav
{
    public sealed record Link(string Route, string Label, string Url);

    static Link L(string route, string label, string url) => new(route, label, url);

    /// <summary>Navigation per role. The bar shows the main destinations only.</summary>
    static readonly Dictionary<string, Link[]> Menus = new()
    {
        ["Learner"] = [L("dashboard", "Overview", "/Dashboard"), L("learning", "My courses", "/MyCourses"), L("catalog", "Explore courses", "/Catalog"), L("certificates", "Certificates", "/Certificates"), L("leaderboard", "Leaderboard", "/Leaderboard"), L("assistant", "AI Assistant", "/Assistant"), L("messages", "Messages", "/Messages")],
        ["Instructor"] = [L("dashboard", "Overview", "/Dashboard"), L("courses", "My courses", "/Courses"), L("requests", "My requests", "/CourseRequests"), L("learners", "Learners", "/Learners"), L("resources", "Learning resources", "/Resources"), L("analytics", "Course analytics", "/Analytics"), L("messages", "Messages", "/Messages")],
        ["Program Leader"] = [L("dashboard", "Overview", "/Dashboard"), L("programs", "Programs", "/Programs"), L("assignments", "Instructors", "/Assignments"), L("approvals", "Course approvals", "/Approvals"), L("messages", "Messages", "/Messages")],
        ["Admin"] = [L("dashboard", "Overview", "/Dashboard"), L("users", "User accounts", "/Users")],
        // Visitors: the tabs scroll to sections of the landing page (#top is the top of the page), except My Enquiries.
        ["Visitor"] = [L("home", "About", "/Home#top"), L("catalog", "Our Courses", Routes.HomeCourses), L("instructors", "Our Instructors", "/Home#visitor-instructors"), L("feedback", "Feedback", "/Home#visitor-stories"), L("faq", "FAQ", "/Home#visitor-faq"), L("help", "Help", Routes.HomeContact), L("enquiries", "My Enquiries", "/EnquiryPortal")]
    };

    public static Link[] For(UserRow me) => Menus[me == null ? "Visitor" : me.Role];

    /// <summary>
    /// What js/call-preview.js shows in the device-only call preview: the signed-in account, a demo caller for the
    /// "Test video call" buttons, and a course for the outline. JSON, escaped so it is safe inside a script element.
    /// </summary>
    public static string CallContext(SkillContext ctx)
    {
        var me = ctx.Me;
        var data = ctx.Data;
        var peer = data.Users.FirstOrDefault(u => u.Id != me.Id && u.Role == "Instructor" && u.Status == "Active") ?? data.Users.FirstOrDefault(u => u.Id != me.Id && u.Status == "Active");
        var enrolled = ctx.MyEnrollments.Select(x => data.Course(x.CourseId)).FirstOrDefault(c => c != null);
        var course = enrolled ?? data.Courses.FirstOrDefault(c => c.InstructorId == me.Id);
        return JsonConvert.SerializeObject(new
        {
            api = "/api/call/",
            me = new { id = me.Id, name = me.Name, avatar = me.Avatar },
            peer = peer == null ? null : new { name = peer.Name, role = peer.Role },
            course = course == null ? null : new
            {
                title = course.Title, description = course.Description ?? "", outcomes = course.Outcomes ?? "",
                lessons = data.LessonsOf(course.Id).Select(l => l.Title), progress = enrolled == null ? 0 : ctx.Progress(me.Id, course.Id)
            }
        }, new JsonSerializerSettings { StringEscapeHandling = StringEscapeHandling.EscapeHtml });
    }
}
