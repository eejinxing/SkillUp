using System.Globalization;
using Newtonsoft.Json.Linq;
using SkillUp.Models;

namespace SkillUp.Services;

/// <summary>Role rules shared by controllers and commands, and the small JSON shapes that commands return to the browser.</summary>
public static class Workspace
{
    /// <summary>Sent to the browser as UTC with a Z, so it converts the time to the viewer's own clock.</summary>
    public static string Iso(DateTime time)
    {
        var utc = time.Kind == DateTimeKind.Utc ? time : DateTime.SpecifyKind(time, DateTimeKind.Local).ToUniversalTime();
        return utc.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
    }

    static string Day(DateTime? date) => date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
    static string Id(int id) => id.ToString(CultureInfo.InvariantCulture);
    static string Id(int? id) => id?.ToString(CultureInfo.InvariantCulture);

    public static bool IsStaff(UserRow u) => u != null && (u.Role == "Instructor" || u.Role == "Program Leader" || u.Role == "Admin");

    /// <summary>Courses this user may edit: assigned instructor, leader of the course's program, or any for Admin.</summary>
    public static HashSet<int> ManagedCourses(Snapshot m, UserRow u)
    {
        if (u == null) return new HashSet<int>();
        if (u.Role == "Admin") return new HashSet<int>(m.Courses.Select(c => c.Id));
        if (u.Role == "Instructor") return new HashSet<int>(m.Courses.Where(c => c.InstructorId == u.Id).Select(c => c.Id));
        if (u.Role == "Program Leader")
        {
            var owned = new HashSet<int>(m.Programs.Where(p => p.LeaderId == u.Id).Select(p => p.Id));
            return new HashSet<int>(m.Courses.Where(c => owned.Contains(c.ProgramId)).Select(c => c.Id));
        }
        return new HashSet<int>();
    }

    /// <param name="full">Include lab answers and the quiz answer key (staff who manage the course).</param>
    /// <param name="visitor">Signed-out visitors only see the body of activities marked as a public preview.</param>
    public static JObject CourseDto(Snapshot m, CourseRow c, bool full, bool visitor)
    {
        var quiz = m.QuizFor(c.Id);
        var dto = new JObject
        {
            ["id"] = Id(c.Id), ["title"] = c.Title, ["description"] = c.Description ?? "", ["outcomes"] = c.Outcomes ?? "", ["programId"] = Id(c.ProgramId),
            ["instructorId"] = Id(c.InstructorId), ["difficulty"] = c.Difficulty, ["status"] = c.Status, ["deadline"] = Day(c.Deadline), ["startDate"] = Day(c.StartDate),
            ["duration"] = c.Duration, ["color"] = c.Color ?? "mint", ["icon"] = c.Icon ?? "UP",
            ["lessons"] = new JArray(m.LessonsOf(c.Id).Select(l =>
            {
                var lesson = new JObject
                {
                    ["id"] = Id(l.Id), ["title"] = l.Title, ["type"] = l.Type, ["body"] = visitor && !l.Preview ? "" : (l.Body ?? ""), ["minutes"] = l.Minutes, ["preview"] = l.Preview
                };
                if (full) lesson["answer"] = l.Type == "lab" ? (l.Answer ?? "") : "";
                return lesson;
            })),
            ["quiz"] = new JObject
            {
                ["published"] = quiz?.Status == "Published",
                ["timeLimit"] = quiz?.TimeLimit ?? 10,
                ["questions"] = new JArray((quiz?.Questions ?? new List<QuestionRow>()).Select(q =>
                {
                    var question = new JObject { ["id"] = Id(q.Id), ["text"] = q.Text, ["options"] = new JArray(q.Options.Select(o => o.Text)), ["topic"] = q.Topic ?? "" };
                    if (full) question["answer"] = q.AnswerIndex;
                    return question;
                }))
            }
        };
        if (!string.IsNullOrEmpty(c.Thumbnail)) dto["artwork"] = c.Thumbnail;
        return dto;
    }

    public static JObject EnquiryDto(EnquiryRow q) => new()
    {
        ["id"] = Id(q.Id), ["name"] = q.Name, ["email"] = q.Email, ["subject"] = q.Subject, ["message"] = q.Message, ["status"] = q.Status, ["createdAt"] = Iso(q.CreatedAt)
    };

    public static JObject RequestDto(RequestRow r)
    {
        var first = r.Events.FirstOrDefault(e => e.Action == "Submitted");
        return new JObject
        {
            ["id"] = Id(r.Id), ["submissionId"] = first == null ? null : Id(first.Id), ["kind"] = r.Kind, ["courseId"] = Id(r.CourseId), ["courseTitle"] = r.CourseTitle,
            ["programId"] = Id(r.ProgramId), ["requesterId"] = Id(r.RequesterId), ["status"] = r.Status, ["reason"] = r.Reason ?? "", ["createdAt"] = Iso(r.CreatedAt),
            ["events"] = new JArray(r.Events.Select(e => new JObject
            {
                ["id"] = Id(e.Id), ["actorId"] = Id(e.ActorId), ["action"] = e.Action, ["comment"] = e.Comment ?? "", ["createdAt"] = Iso(e.CreatedAt)
            }))
        };
    }
}
