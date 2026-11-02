using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using System.Text.RegularExpressions;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend
{
    /// <summary>
    /// The AI Assistant: the learner's general conversation plus one conversation for each activity they asked about
    /// (?lesson=). The side list switches between them; each is saved separately (AI_CHAT_SESSION.lesson_id).
    /// </summary>
    public partial class AssistantPage : SkillPage
    {
        public override string Route => "assistant";
        protected override string[] Roles => new string[0];
        public override bool Secondary => true;

        /// <summary>The open conversation's activity, or null for the general conversation.</summary>
        protected int? SelectedLesson;
        protected string ChatTitle = "General", ChatSub = "Ask anything about learning", ChatLessonUrl, ConversationsHtml, WhereText = "General";
        protected bool HasChat;

        protected void Page_Load(object sender, EventArgs e)
        {
            var lesson = OpenableLesson(QueryInt("lesson"));
            SelectedLesson = lesson?.Id;
            if (lesson != null)
            {
                ChatTitle = lesson.Title;
                ChatSub = Data.Course(lesson.CourseId)?.Title ?? "";
                ChatLessonUrl = "Activity?course=" + lesson.CourseId + "&lesson=" + lesson.Id;
                WhereText = ChatTitle + " · " + ChatSub;
            }
            // Showing the conversation reads it: the badge for its replies goes away and stays away.
            if (!IsPostBack && Data.AiMessages.Any(x => x.UserId == Me.Id && x.LessonId == SelectedLesson && x.Sender == "AI" && !x.Read))
                TryRun("aiRead", new { lessonId = SelectedLesson?.ToString() });
            var pairs = AiPairs(SelectedLesson);
            HasChat = pairs.Count > 0;
            History.DataSource = pairs;
            History.DataBind();
            // The shortcuts and the "+" button ask about the open activity, or about learning in general.
            Suggestions.DataSource = lesson != null
                ? new[]
                {
                    new { Title = "Explain this task", Detail = "Break it down, step by step", Question = "Explain what this activity is asking me to do: " + lesson.Title + "." },
                    new { Title = "Give me a hint", Detail = "A nudge in the right direction", Question = "Give me a hint for " + lesson.Title + " without telling me the answer." },
                    new { Title = "Show an example", Detail = "See the idea in practice", Question = "Show me a small example related to " + lesson.Title + "." }
                }
                : new[]
                {
                    new { Title = "Plan my study", Detail = "A simple plan for this week", Question = "Help me plan my study time this week, based on my courses." },
                    new { Title = "Explain a concept", Detail = "In plain, simple words", Question = "Explain a concept I find hard, in simple words. Ask me which one." },
                    new { Title = "Quiz me", Detail = "One question at a time", Question = "Ask me one question to check what I know, then wait for my answer." }
                };
            Suggestions.DataBind();
            ContextButton.Visible = lesson != null;
            if (lesson != null) ContextButton.Attributes["data-question"] = "Help me understand " + ChatSub + " — " + lesson.Title + ".";
            BuildConversations();
        }

        /// <summary>An activity of a course the learner is enrolled in; anything else opens the general conversation.</summary>
        LessonRow OpenableLesson(int? id)
        {
            var lesson = id == null ? null : Data.Lessons.FirstOrDefault(l => l.Id == id.Value);
            return lesson != null && MyEnrollments.Any(x => x.CourseId == lesson.CourseId) ? lesson : null;
        }

        static readonly string[] CourseColours = { "#a3b0ce", "#b8a1c6", "#9fb6ae", "#baa38e" };

        /// <summary>
        /// The list of conversations, in the style of the Messages sidebar: General first, then each enrolled course as a folding
        /// group (a coloured dot and its name) with its activities under it. A pill shows only AI replies not seen yet, so it goes away
        /// once the conversation is opened. Activities with messages are bright, the others dim; the open one is highlighted, and a
        /// course is open when it has a conversation or holds the open one.
        /// </summary>
        void BuildConversations()
        {
            var mine = Data.AiMessages.Where(x => x.UserId == Me.Id).ToList();
            var questions = mine.Where(x => x.Sender == "User" && x.LessonId != null).Select(x => x.LessonId.Value).Distinct().ToList();
            var unread = mine.Where(x => x.Sender == "AI" && !x.Read && x.LessonId != null).GroupBy(x => x.LessonId.Value).ToDictionary(g => g.Key, g => g.Count());
            var last = mine.Where(x => x.LessonId != null).GroupBy(x => x.LessonId.Value).ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).Last().Content);
            var generalStarted = mine.Any(x => x.Sender == "User" && x.LessonId == null);
            var generalUnread = mine.Count(x => x.Sender == "AI" && !x.Read && x.LessonId == null);

            var html = new StringBuilder();
            html.Append(Row("assistant-convo assistant-channel is-general", "Assistant", "<span class=\"assistant-dot all\"></span>", "General", generalStarted, generalUnread, SelectedLesson == null, "Any topic, not tied to one activity"));
            var index = 0;
            foreach (var course in Data.Courses.Where(c => MyEnrollments.Any(x => x.CourseId == c.Id)))
            {
                var lessons = Data.LessonsOf(course.Id);
                if (lessons.Count == 0) continue;
                var started = lessons.Any(l => questions.Contains(l.Id));
                var courseUnread = lessons.Sum(l => unread.ContainsKey(l.Id) ? unread[l.Id] : 0);
                var holdsOpen = lessons.Any(l => l.Id == SelectedLesson);
                html.Append("<details class=\"assistant-group").Append(holdsOpen ? " has-active" : "").Append("\"").Append(started || holdsOpen ? " open" : "").Append(">")
                    .Append("<summary class=\"assistant-channel\"><span class=\"assistant-dot\" style=\"--channel-color:").Append(CourseColours[index++ % CourseColours.Length]).Append("\"></span>")
                    .Append("<span class=\"assistant-name\">").Append(Ui.H(course.Title)).Append("</span>").Append(courseUnread > 0 ? "<small>" + courseUnread + "</small>" : "").Append("</summary>")
                    .Append("<div class=\"assistant-items\">");
                foreach (var lesson in lessons)
                {
                    int waiting;
                    unread.TryGetValue(lesson.Id, out waiting);
                    string text;
                    html.Append(Row("assistant-convo assistant-item", "Assistant?lesson=" + lesson.Id, "", lesson.Title, questions.Contains(lesson.Id), waiting, lesson.Id == SelectedLesson,
                        last.TryGetValue(lesson.Id, out text) ? Regex.Replace(text ?? "", @"\s+", " ").Trim() : "No messages yet"));
                }
                html.Append("</div></details>");
            }
            ConversationsHtml = html.ToString();
        }

        /// <summary>One row of the list. A row with nothing asked yet is dim; the pill shows only the replies not seen yet.</summary>
        static string Row(string classes, string url, string lead, string title, bool started, int unread, bool active, string tip)
        {
            return "<a class=\"" + classes + (active ? " is-active" : "") + (started ? "" : " is-empty") + "\" href=\"" + url + "\"" + (active ? " aria-current=\"true\"" : "")
                + " title=\"" + Ui.H(tip.Length > 120 ? tip.Substring(0, 120) + "…" : tip) + "\">" + lead + "<span class=\"assistant-name\">" + Ui.H(title) + "</span>"
                + (unread > 0 ? "<small>" + unread + "</small>" : "") + "</a>";
        }

        protected void Ask_Click(object sender, EventArgs e) => Ask(AskBox.Text.Trim());

        protected void Ask_Command(object sender, CommandEventArgs e) => Ask((string)e.CommandArgument);

        protected void Context_Click(object sender, EventArgs e) => Ask("Help me understand " + ChatSub + " — " + ChatTitle + ".");

        void Ask(string question)
        {
            if (question.Length == 0)
            {
                AskError.Text = "Type a question first.";
                return;
            }
            object ignored;
            // A refusal (over the allowance, Gemini unavailable) shows under the question box.
            if (TryRun("ai", new { text = question, lessonId = SelectedLesson?.ToString() }, out ignored, message => AskError.Text = Server.HtmlEncode(message))) Go(Request.RawUrl);
        }
    }
}
