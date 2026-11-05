using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using SkillUp.Services;

namespace SkillUp.Models
{
    /// <summary>
    /// One learning activity (?course=&amp;lesson=): a reading, a lab answer or a timed quiz, with the AI companion and a
    /// practice terminal. Learners must be enrolled and open activities in order; visitors get a read-only preview
    /// of a course's first activities. Grading always happens on the server (Commands.Complete / QuizSubmit).
    /// </summary>
    public sealed class ActivityModel : PageModel
    {
        public ActivityModel(SkillContext ctx) : base(ctx) { }
        public string PageTitle, QuizError, TerminalBody, LabAnswerValue = "", LabPlaceholder = "", ContextQuestion, AskPlaceholder;
        public int ScreenIndex, TaskIndex, TerminalTask;
        public bool ShowLabBefore, ShowLabAfter, ShowComplete, ShowHintPanel, LabLocked, QuizFormVisible = true, QuizResultVisible, ShowRestart, QuizSubmitDisabled;
        public IEnumerable<dynamic> Suggestions, QuizQuestions, ResultFeedback;

        /// <summary>When this learner opened the quiz (kept in the session), for the time limit.</summary>
        public DateTime? QuizStart
        {
            get { var text = Ctx.Http.Session.GetString(QuizKey + ".Start"); return text == null ? null : DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind); }
            set { if (value == null) Ctx.Http.Session.Remove(QuizKey + ".Start"); else Ctx.Http.Session.SetString(QuizKey + ".Start", value.Value.ToString("o", CultureInfo.InvariantCulture)); }
        }


        public CourseRow C;
        public LessonRow L;
        public QuizRow Quiz;
        public bool Guest, QuizReady, IsCompleted, ResultPassed;
        public int Percent, SecondsLeft, HiddenQuestions, TaskNo, ResultScore;
        public string Banner, NavHtml, HintText, UnavailableText;
        public bool HintShown;
        /// <summary>For a lab without a terminal: the code the learner completes (the lab's starter code), shown above the answer box.</summary>
        public string CodeToComplete;
        public List<AiPair> History = new List<AiPair>();
        public List<QuestionRow> Questions = new List<QuestionRow>();

        List<LessonRow> lessons;
        int index;
        EnrollmentRow enrollment;
        List<int> completed = new List<int>();

        public string QuizKey => "SkillUp.Quiz." + Me?.Id + "." + C?.Id + "." + L?.Id;

        public void Load()
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
                if (index >= CourseModel.GuestPreviewCount) { ScreenIndex = 2; return; }
            }
            else
            {
                enrollment = C == null ? null : MyEnrollments.FirstOrDefault(x => x.CourseId == C.Id);
                if (C == null || enrollment == null || L == null || C.Status != "Published") { Unavailable("Enrol in an available course to access this activity."); return; }
                completed = Data.CompletedLessons(Me.Id, C.Id);
                if (index > 0 && !completed.Contains(lessons[index - 1].Id)) { ScreenIndex = 3; return; }
                Percent = Progress(Me.Id, C.Id);
                // Remember where this learner is, so "Continue learning" on the dashboard returns here (a cookie per account).
                Ctx.Http.Response.Cookies.Append("SkillUp.Resume." + Me.Id, C.Id + "." + L.Id,
                    new CookieOptions { HttpOnly = true, Secure = Ctx.Http.Request.IsHttps, Expires = DateTimeOffset.Now.AddYears(1) });
            }

            PageTitle = C.Title + " / " + L.Title;
            IsCompleted = completed.Contains(L.Id);
            Quiz = Data.QuizFor(C.Id);
            QuizReady = L.Type == "quiz" && Quiz != null && Quiz.Status == "Published" && Quiz.Questions.Count > 0;
            // In a lab with a terminal the terminal is Task 1 and the question Task 2; otherwise the activity is Task 1.
            TaskNo = L.Type == "lab" && L.Workspace == "javascript" ? 2 : 1;
            Banner = SkillBanner(C, index, Percent);
            NavHtml = Navigation();

            // The terminal comes first in a lab, after the task otherwise.
            TerminalTask = L.Type == "lab" ? 1 : 2;
            TerminalBody = Blanks(L.Body);
            // The terminal only runs JavaScript, so it appears only where the instructor chose it. A lab without one
            // shows its starter code as the code to complete.
            var hasTerminal = L.Workspace == "javascript" && L.Type != "quiz";
            ShowLabBefore = hasTerminal && L.Type == "lab";
            ShowLabAfter = hasTerminal && L.Type != "lab";
            CodeToComplete = !hasTerminal && L.Type == "lab" ? Blanks(L.StarterCode) : null;

            TaskIndex = L.Type == "quiz" ? 2 : L.Type == "lab" ? 1 : 0;
            ShowComplete = !Guest && !IsCompleted;
            // A lab already passed shows the correct answer in the answer box too.
            // Once passed, the answer is final: the box is locked and the Submit button is off.
            if (!Guest && IsCompleted && L.Type == "lab")
            {
                LabAnswerValue = L.Answer;
                LabLocked = true;
            }
            LabPlaceholder = Guest ? "Log in to answer" : "Type your answer…";
            ContextQuestion = "Help me understand " + C.Title + " — " + L.Title + ".";
            AskPlaceholder = Guest ? "Log in to ask the companion" : "Curious? Let’s explore together";

            BindCompanion();
            if (L.Type == "quiz") BindQuiz();
            if (!Guest)
            {
                HintText = BuildHint();
                ShowHintPanel = true;
            }
        }

        void Unavailable(string text)
        {
            UnavailableText = text;
            ScreenIndex = 1;
        }

        void BindCompanion()
        {
            if (!Guest)
            {
                // Showing the conversation reads it (the badge in the AI Assistant list goes away).
                // The last three questions and replies.
                var pairs = AiPairs(L.Id);
                History = pairs.Skip(Math.Max(0, pairs.Count - 3)).ToList();
            }
            Suggestions = new[]
            {
                new { Title = "Explain this task", Detail = "Break it down, step by step", Question = "Explain what this activity is asking me to do: " + L.Title + "." },
                new { Title = "Give me a hint", Detail = "A nudge in the right direction", Question = "Give me a hint for " + L.Title + " without telling me the answer." },
                new { Title = "Show an example", Detail = "See the idea in practice", Question = "Show me a small example related to " + L.Title + "." }
            }.ToList();
        }

        void BindQuiz()
        {
            if (!QuizReady)
            {
                QuizFormVisible = false;
                return;
            }
            // A finished attempt is shown once, after the redirect that follows submitting.
            var result = Ctx.Http.Session.GetString(QuizKey + ".Result");
            if (result != null)
            {
                Ctx.Http.Session.Remove(QuizKey + ".Result");
                ShowResult(JObject.Parse(result));
                return;
            }
            // Visitors preview half of the questions.
            Questions = Guest ? Quiz.Questions.Take((Quiz.Questions.Count + 1) / 2).ToList() : Quiz.Questions;
            HiddenQuestions = Quiz.Questions.Count - Questions.Count;
            if (!Guest)
            {
                var start = QuizStart;
                if (start == null) QuizStart = start = DateTime.Now;
                SecondsLeft = Math.Max(0, (Quiz.TimeLimit ?? 10) * 60 - (int)(DateTime.Now - start.Value).TotalSeconds);
                if (SecondsLeft == 0) TimeUp();
            }
            QuizQuestions = Questions.Select((q, n) => new
            {
                Number = Quiz.Questions.Count > 1 ? (n + 1) + ". " : "",
                q.Text,
                Options = q.Options.Select((o, j) => new { Name = "q" + n, Index = j, o.Text, Checked = false }).ToList()
            }).ToList();
        }

        void TimeUp()
        {
            QuizError = "Time is up. Restart this quiz to try again.";
            QuizSubmitDisabled = true;
            ShowRestart = true;
        }

        void ShowResult(JObject result)
        {
            QuizFormVisible = false;
            QuizResultVisible = true;
            ResultScore = (int)result["score"];
            ResultPassed = ResultScore >= 70;
            ResultFeedback = ((JArray)result["feedback"]).Select(f => new
            {
                Correct = (bool)f["correct"], Text = (string)f["text"], Selected = (string)f["selectedText"], Answer = (string)f["correctText"]
            }).ToList();
        }

        string Navigation()
        {
            var link = new Func<string, string, string, string>((url, text, cls) => "<a class=\"" + cls + "\" href=\"" + url + "\">" + text + "</a>");
            var previous = index > 0 ? link(LessonUrl(lessons[index - 1]), "&larr; Previous activity", "answer-previous") : link("/Course?id=" + C.Id, "&larr; All activities", "answer-previous");
            var next = index + 1 < lessons.Count ? lessons[index + 1] : null;
            string forward;
            if (Guest) forward = next != null && index + 1 < CourseModel.GuestPreviewCount ? link(LessonUrl(next), "Next preview &rarr;", "answer-next") : link("#login", "Log in to unlock more &rarr;", "answer-next");
            else if (IsCompleted) forward = next != null ? link(LessonUrl(next), "Next activity &rarr;", "answer-next") : link("/Course?id=" + C.Id, "View course progress &rarr;", "answer-next");
            else forward = "<span>Complete this activity to continue</span>";
            return previous + forward;
        }

        string LessonUrl(LessonRow lesson) => "/Activity?course=" + C.Id + "&lesson=" + lesson.Id;

        // ---- actions ----------------------------------------------------------------------------

        /// <summary>
        /// A blank in a lab's text or code (three or more underscores) becomes one underscore per letter of the expected answer,
        /// for example _ _ _ _ _ _ _ for a seven-letter answer, so the learner can see how long the missing word is. After the
        /// lab is passed it becomes the answer itself.
        /// </summary>
        public string Blanks(string text)
        {
            var answer = (L?.Type == "lab" ? L.Answer : null)?.Trim();
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(answer)) return text;
            // Once the learner has passed the lab, the blanks show the correct answer. Visitors never see it.
            if (!Guest && IsCompleted) return System.Text.RegularExpressions.Regex.Replace(text, "_{3,}", m => answer);
            var blank = string.Join("  ", answer.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(word => string.Join(" ", Enumerable.Repeat("_", word.Length))));
            return System.Text.RegularExpressions.Regex.Replace(text, "_{3,}", blank);
        }

        string BuildHint()
        {
            if (L.Type == "lab")
            {
                var answer = L.Answer ?? "";
                return "The method starts with “" + (answer.Length >= 2 ? answer.Substring(0, 2) : answer) + "” and has " + answer.Length + " characters. Check the method name in the lesson example.";
            }
            if (L.Type == "quiz")
                return "Read each question carefully, then connect it to the course examples. Focus on the key concept: " + (Quiz?.Questions.FirstOrDefault()?.Topic ?? L.Title) + ". You can ask your learning companion to explain the idea.";
            return "Work through the explanation one step at a time. Try describing " + L.Title + " in your own words, then check it against the lesson example.";
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
