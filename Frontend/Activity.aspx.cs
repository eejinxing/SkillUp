using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using Newtonsoft.Json.Linq;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend
{
    /// <summary>
    /// One learning activity (?course=&amp;lesson=): a reading, a lab answer or a timed quiz, with the AI companion and a
    /// practice terminal. Learners must be enrolled and open activities in order; visitors get a read-only preview
    /// of a course's first activities. Grading always happens on the server (Commands.Complete / QuizSubmit).
    /// </summary>
    public partial class ActivityPage : SkillPage
    {
        public override string Route => "lesson";
        public override bool StayAfterSignIn => true;

        protected CourseRow C;
        protected LessonRow L;
        protected QuizRow Quiz;
        protected bool Guest, QuizReady, IsCompleted, ResultPassed;
        protected int Percent, SecondsLeft, HiddenQuestions, TaskNo, ResultScore;
        protected string Banner, NavHtml, HintText, UnavailableText;
        protected List<AiPair> History = new List<AiPair>();
        protected List<QuestionRow> Questions = new List<QuestionRow>();

        List<LessonRow> lessons;
        int index;
        EnrollmentRow enrollment;
        List<int> completed = new List<int>();

        string QuizKey => "SkillUp.Quiz." + Me?.Id + "." + C?.Id + "." + L?.Id;

        protected void Page_Load(object sender, EventArgs e)
        {
            Guest = Me == null;
            var cid = QueryInt("course");
            C = cid == null ? null : Data.Course(cid.Value);
            lessons = C == null ? new List<LessonRow>() : Data.LessonsOf(C.Id);
            var lid = QueryInt("lesson");
            L = lessons.FirstOrDefault(l => l.Id == lid);
            index = L == null ? -1 : lessons.IndexOf(L);

            if (!Guest && Me.Role != "Learner")
            {
                Unavailable("Activities open for enrolled learners. Use the course editor to review content.");
                return;
            }
            if (Guest)
            {
                if (C == null || L == null || C.Status != "Published") { Unavailable("This activity is not currently open."); return; }
                if (index >= CoursePage.GuestPreviewCount) { Screens.ActiveViewIndex = 2; return; }
            }
            else
            {
                enrollment = C == null ? null : MyEnrollments.FirstOrDefault(x => x.CourseId == C.Id);
                if (C == null || enrollment == null || L == null || C.Status != "Published") { Unavailable("Enrol in an available course to access this activity."); return; }
                completed = Data.CompletedLessons(Me.Id, C.Id);
                if (index > 0 && !completed.Contains(lessons[index - 1].Id)) { Screens.ActiveViewIndex = 3; return; }
                Percent = Progress(Me.Id, C.Id);
            }

            Title = C.Title + " / " + L.Title;
            IsCompleted = completed.Contains(L.Id);
            Quiz = Data.QuizFor(C.Id);
            QuizReady = L.Type == "quiz" && Quiz != null && Quiz.Status == "Published" && Quiz.Questions.Count > 0;
            TaskNo = L.Type == "lab" ? 2 : 1;
            Banner = SkillBanner(C, index, Percent);
            NavHtml = Navigation();

            // The terminal comes first in a lab, after the task otherwise.
            foreach (var lab in new[] { LabBefore, LabAfter })
            {
                lab.Lesson = L;
                lab.Guest = Guest;
                lab.TaskNumber = L.Type == "lab" ? 1 : 2;
                lab.Username = Me?.Username ?? "learner";
            }
            LabBefore.Visible = L.Type == "lab";
            LabAfter.Visible = L.Type != "lab";

            Task.ActiveViewIndex = L.Type == "quiz" ? 2 : L.Type == "lab" ? 1 : 0;
            CompleteButton.Visible = !Guest && !IsCompleted;
            foreach (var b in new[] { HintArticle, HintLab, HintQuiz, LabSubmit, QuizSubmit, ContextButton, AskButton }) b.Enabled = !Guest;
            LabAnswer.Enabled = !Guest;
            LabAnswer.Attributes["placeholder"] = Guest ? "Log in to answer" : "Type your answer…";
            AskBox.Enabled = !Guest;
            AskBox.Attributes["placeholder"] = Guest ? "Log in to ask the companion" : "Curious? Let’s explore together";

            BindCompanion();
            if (L.Type == "quiz") BindQuiz();
        }

        void Unavailable(string text)
        {
            UnavailableText = text;
            Screens.ActiveViewIndex = 1;
        }

        void BindCompanion()
        {
            if (!Guest)
            {
                // The last three questions and replies.
                var pairs = AiPairs();
                History = pairs.Skip(Math.Max(0, pairs.Count - 3)).ToList();
            }
            Conversation.DataSource = History;
            Conversation.DataBind();
            Suggestions.DataSource = new[]
            {
                new { Title = "Explain this task", Detail = "Break it down, step by step", Question = "Explain what this activity is asking me to do: " + L.Title + "." },
                new { Title = "Give me a hint", Detail = "A nudge in the right direction", Question = "Give me a hint for " + L.Title + " without telling me the answer." },
                new { Title = "Show an example", Detail = "See the idea in practice", Question = "Show me a small example related to " + L.Title + "." }
            };
            Suggestions.DataBind();
        }

        void BindQuiz()
        {
            if (!QuizReady)
            {
                QuizForm.Visible = false;
                return;
            }
            // A finished attempt is shown once, after the redirect that follows submitting.
            var result = Session[QuizKey + ".Result"] as string;
            if (result != null)
            {
                Session.Remove(QuizKey + ".Result");
                ShowResult(JObject.Parse(result));
                return;
            }
            // Visitors preview half of the questions.
            Questions = Guest ? Quiz.Questions.Take((Quiz.Questions.Count + 1) / 2).ToList() : Quiz.Questions;
            HiddenQuestions = Quiz.Questions.Count - Questions.Count;
            if (!Guest)
            {
                var start = Session[QuizKey + ".Start"] as DateTime?;
                if (start == null) Session[QuizKey + ".Start"] = start = DateTime.Now;
                SecondsLeft = Math.Max(0, (Quiz.TimeLimit ?? 10) * 60 - (int)(DateTime.Now - start.Value).TotalSeconds);
                if (SecondsLeft == 0) TimeUp();
            }
            QuizQuestions.DataSource = Questions.Select((q, n) => new
            {
                Number = Quiz.Questions.Count > 1 ? (n + 1) + ". " : "",
                q.Text,
                Options = q.Options.Select((o, j) => new { Name = "q" + n, Index = j, o.Text, Checked = Request.Form["q" + n] == j.ToString(CultureInfo.InvariantCulture) }).ToList()
            });
            QuizQuestions.DataBind();
        }

        void TimeUp()
        {
            QuizError.Text = "Time is up. Restart this quiz to try again.";
            QuizSubmit.Enabled = false;
            RestartButton.Visible = true;
        }

        void ShowResult(JObject result)
        {
            QuizForm.Visible = false;
            QuizResult.Visible = true;
            ResultScore = (int)result["score"];
            ResultPassed = ResultScore >= 70;
            TryAgainButton.Visible = !ResultPassed;
            ResultFeedback.DataSource = ((JArray)result["feedback"]).Select(f => new
            {
                Correct = (bool)f["correct"], Text = (string)f["text"], Selected = (string)f["selectedText"], Answer = (string)f["correctText"]
            });
            ResultFeedback.DataBind();
        }

        string Navigation()
        {
            var link = new Func<string, string, string, string>((url, text, cls) => "<a class=\"" + cls + "\" href=\"" + url + "\">" + text + "</a>");
            var previous = index > 0 ? link(LessonUrl(lessons[index - 1]), "&larr; Previous activity", "answer-previous") : link("Course?id=" + C.Id, "&larr; All activities", "answer-previous");
            var next = index + 1 < lessons.Count ? lessons[index + 1] : null;
            string forward;
            if (Guest) forward = next != null && index + 1 < CoursePage.GuestPreviewCount ? link(LessonUrl(next), "Next preview &rarr;", "answer-next") : link("#login", "Log in to unlock more &rarr;", "answer-next");
            else if (IsCompleted) forward = next != null ? link(LessonUrl(next), "Next activity &rarr;", "answer-next") : link("Course?id=" + C.Id, "View course progress &rarr;", "answer-next");
            else forward = "<span>Complete this activity to continue</span>";
            return previous + forward;
        }

        string LessonUrl(LessonRow lesson) => "Activity?course=" + C.Id + "&lesson=" + lesson.Id;

        // ---- actions ----------------------------------------------------------------------------

        protected void Complete_Click(object sender, EventArgs e)
        {
            if (!TryRun("complete", new { courseId = C.Id.ToString(), lessonId = L.Id.ToString() })) return;
            var next = index + 1 < lessons.Count ? lessons[index + 1] : null;
            Go(next != null ? LessonUrl(next) : "Course?id=" + C.Id, "Activity completed");
        }

        protected void LabSubmit_Click(object sender, EventArgs e)
        {
            var answer = LabAnswer.Text.Trim();
            if (answer.Length == 0)
            {
                LabError.Text = "Type your answer first.";
                return;
            }
            object result;
            if (!TryRun("complete", new { courseId = C.Id.ToString(), lessonId = L.Id.ToString(), answer }, out result)) return;
            if ((bool?)((JObject)result)?["correct"] == true) Go("Course?id=" + C.Id, "Correct — lab completed!");
            LabError.Text = "Not quite. Check the method name and try again. Your attempt has been recorded.";
        }

        protected void QuizSubmit_Click(object sender, EventArgs e)
        {
            var start = Session[QuizKey + ".Start"] as DateTime?;
            if (start != null && (DateTime.Now - start.Value).TotalMinutes > (Quiz.TimeLimit ?? 10))
            {
                TimeUp();
                return;
            }
            var answers = new JArray();
            for (var n = 0; n < Quiz.Questions.Count; n++)
            {
                int choice;
                answers.Add(int.TryParse(Request.Form["q" + n], NumberStyles.Integer, CultureInfo.InvariantCulture, out choice) ? choice : -1);
            }
            if (answers.Any(a => (int)a < 0))
            {
                QuizError.Text = "Answer every question before submitting.";
                return;
            }
            object result;
            if (!TryRun("quizSubmit", new JObject { ["courseId"] = C.Id.ToString(), ["lessonId"] = L.Id.ToString(), ["answers"] = answers, ["startedAt"] = start?.ToString("o", CultureInfo.InvariantCulture) }, out result)) return;
            Session.Remove(QuizKey + ".Start");
            Session[QuizKey + ".Result"] = ((JObject)result).ToString();
            Go(Request.RawUrl);
        }

        protected void Restart_Click(object sender, EventArgs e)
        {
            Session.Remove(QuizKey + ".Start");
            Go(Request.RawUrl);
        }

        protected void Hint_Click(object sender, EventArgs e)
        {
            HintPanel.Visible = true;
            if (L.Type == "lab")
            {
                var answer = L.Answer ?? "";
                HintText = "The method starts with “" + (answer.Length >= 2 ? answer.Substring(0, 2) : answer) + "” and has " + answer.Length + " characters. Check the method name in the lesson example.";
            }
            else if (L.Type == "quiz")
                HintText = "Read each question carefully, then connect it to the course examples. Focus on the key concept: " + (Quiz?.Questions.FirstOrDefault()?.Topic ?? L.Title) + ". You can ask your learning companion to explain the idea.";
            else
                HintText = "Work through the explanation one step at a time. Try describing " + L.Title + " in your own words, then check it against the lesson example.";
        }

        protected void Ask_Click(object sender, EventArgs e) => Ask(AskBox.Text.Trim());

        protected void Ask_Command(object sender, CommandEventArgs e) => Ask((string)e.CommandArgument);

        protected void Context_Click(object sender, EventArgs e) => Ask("Help me understand " + C.Title + " — " + L.Title + ".");

        void Ask(string question)
        {
            if (question.Length == 0)
            {
                AskError.Text = "Type a question first.";
                return;
            }
            object ignored;
            // A refusal (over the allowance, Gemini unavailable) shows under the question box, not in the quiz or lab messages.
            if (TryRun("ai", new { text = question }, out ignored, message => AskError.Text = Server.HtmlEncode(message))) Go(Request.RawUrl);
        }

        public override void ShowError(string message)
        {
            if (L?.Type == "quiz") QuizError.Text = Server.HtmlEncode(message);
            else if (L?.Type == "lab") LabError.Text = Server.HtmlEncode(message);
            else base.ShowError(message);
        }

        // ---- artwork ------------------------------------------------------------------------------

        static readonly Tuple<char, int, string>[] Glyphs =
        {
            Tuple.Create('S', 60, "M12 0H60V20H20V40H48L60 52V88L48 100H0V80H40V60H12L0 48V12Z"),
            Tuple.Create('K', 62, "M0 0H20V40L40 0H62L36 50L62 100H40L20 62V100H0Z"),
            Tuple.Create('I', 20, "M0 0H20V100H0Z"),
            Tuple.Create('L', 55, "M0 0H20V80H55V100H0Z"),
            Tuple.Create('L', 55, "M0 0H20V80H55V100H0Z"),
            Tuple.Create(' ', 22, ""),
            Tuple.Create('U', 60, "M0 0H20V80H40V0H60V88L48 100H12L0 88Z"),
            Tuple.Create('P', 60, "M0 0H48L60 12V58L48 70H20V100H0ZM20 20V50H40V20Z")
        };
        static readonly int[] BarWidths = { 2, 1, 3, 1, 1, 2, 1, 3, 2, 1, 1, 2, 3, 1, 2, 1 };

        /// <summary>The green SKILL UP banner: the word mark drawn from the glyph paths, the activity number and progress.</summary>
        static string SkillBanner(CourseRow course, int index, int percent)
        {
            var letters = new StringBuilder();
            int x = 0, k = 0;
            foreach (var g in Glyphs)
            {
                if (g.Item3.Length > 0) letters.Append("<path class=\"sb-letter\" style=\"--k:" + k + "\" transform=\"translate(" + x + " 0)\" d=\"" + g.Item3 + "\"/>");
                x += g.Item2 + (g.Item1 == ' ' ? 0 : 8);
                k++;
            }
            var bars = string.Concat(BarWidths.Select(w => "<i style=\"--w:" + w + "\"></i>"));
            return "<div class=\"answer-banner skill-banner\" role=\"img\" aria-label=\"Skill UP · " + Ui.H(course.Title) + " · activity " + (index + 1) + "\">"
                + "<svg class=\"sb-word\" viewBox=\"0 0 " + (x - 8) + " 100\" preserveAspectRatio=\"xMinYMid meet\" aria-hidden=\"true\">" + letters + "</svg>"
                + "<span class=\"sb-ring\" aria-hidden=\"true\"><svg viewBox=\"0 0 24 24\"><circle cx=\"12\" cy=\"10\" r=\"7.5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"3.4\"/><path d=\"M12 10v12\" stroke=\"currentColor\" stroke-width=\"3.4\"/></svg></span>"
                + "<span class=\"sb-plus\" aria-hidden=\"true\"></span><span class=\"sb-tm\" aria-hidden=\"true\">TM</span>"
                + "<div class=\"sb-meta\" aria-hidden=\"true\"><span class=\"sb-barcode\">" + bars + "</span><span class=\"sb-x\"></span><span class=\"sb-target\"></span>"
                + "<b>" + (index + 1).ToString("00") + "</b><b>" + percent.ToString("00") + "</b><span class=\"sb-tri\"></span>"
                + "<span class=\"sb-caption\">SKILL UP // " + Ui.H(course.Title.ToUpperInvariant()) + "<br>&ldquo;" + (percent == 100 ? "LEARNING WILL MAKE YOU" : "PRACTICE MAKES PROGRESS") + "&rdquo;</span></div>"
                + "<span class=\"sb-scan\" aria-hidden=\"true\"></span></div>";
        }
    }
}
