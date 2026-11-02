using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using SKILL_UP.Backend;

namespace SKILL_UP.Frontend
{
    /// <summary>
    /// The course quiz (?id=course): time limit, publishing, and multiple-choice questions edited in place.
    /// Unsaved edits live in the session, like the old tab draft, until the quiz is saved or the changes are discarded.
    /// </summary>
    public partial class QuizEditorPage : SkillPage
    {
        sealed class QuizDraft
        {
            public string TimeLimit = "10";
            public bool Published;
            public List<QuestionDraft> Questions = new List<QuestionDraft>();
        }

        sealed class QuestionDraft
        {
            public string Id = "", Text = "", Topic = "";
            public List<string> Options = new List<string>();
            public int Answer = -1;
        }

        public override string Route => "quiz-editor";
        /// <summary>The question editor is a step inside the course editor, so going back from the course editor skips it.</summary>
        protected override string NavGroup => "course-edit";
        protected override string[] Roles => new[] { "Instructor", "Program Leader" };

        protected CourseRow C;
        protected bool Locked, Deleting, Changed;
        QuizDraft draft;
        int hint = -1;

        string DraftKey => "SkillUp.QuizDraft." + Me.Id + "." + C.Id;

        protected void Page_Load(object sender, EventArgs e)
        {
            var id = QueryInt("id");
            C = id == null ? null : OwnCourses.FirstOrDefault(c => c.Id == id);
            if (C == null)
            {
                Denied.Visible = true;
                Editor.Visible = false;
                return;
            }
            Deleting = Data.Requests.Any(r => r.CourseId == C.Id && r.Kind == "Delete" && r.Status == "Pending review");
            Locked = C.Status == "Pending review" || Deleting;
            if (Locked) draft = Saved();
            else if (IsPostBack) draft = Posted();
            else
            {
                var kept = Session[DraftKey] as string;
                draft = kept == null ? Saved() : JsonConvert.DeserializeObject<QuizDraft>(kept);
            }
            if (!IsPostBack || Locked)
            {
                TimeLimit.Text = draft.TimeLimit;
                Published.Checked = draft.Published;
            }
        }

        QuizDraft Saved()
        {
            var quiz = Data.QuizFor(C.Id);
            return new QuizDraft
            {
                TimeLimit = (quiz?.TimeLimit ?? 10).ToString(CultureInfo.InvariantCulture),
                Published = quiz?.Status == "Published",
                Questions = (quiz?.Questions ?? new List<QuestionRow>()).Select(q => new QuestionDraft
                {
                    Id = q.Id.ToString(CultureInfo.InvariantCulture), Text = q.Text ?? "", Topic = q.Topic ?? "", Options = q.Options.Select(o => o.Text ?? "").ToList(), Answer = q.AnswerIndex
                }).ToList()
            };
        }

        /// <summary>The quiz as the browser posted it: settings, each question's text, options, topic and chosen answer.</summary>
        QuizDraft Posted()
        {
            var posted = new QuizDraft { TimeLimit = TimeLimit.Text.Trim(), Published = Published.Checked };
            foreach (RepeaterItem item in Questions.Items)
            {
                int answer;
                posted.Questions.Add(new QuestionDraft
                {
                    Id = ((HiddenField)item.FindControl("QuestionId")).Value,
                    Text = ((TextBox)item.FindControl("Prompt")).Text,
                    Topic = ((TextBox)item.FindControl("Topic")).Text,
                    Options = ((Repeater)item.FindControl("Options")).Items.Cast<RepeaterItem>().Select(o => ((TextBox)o.FindControl("OptionText")).Text).ToList(),
                    Answer = int.TryParse(Request.Form["answer" + item.ItemIndex], NumberStyles.Integer, CultureInfo.InvariantCulture, out answer) ? answer : -1
                });
            }
            return posted;
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            if (C == null) return;
            // Keep unsaved edits for this session; forget them once they match the saved quiz again.
            var json = JsonConvert.SerializeObject(draft);
            Changed = !Locked && json != JsonConvert.SerializeObject(Saved());
            if (Changed) Session[DraftKey] = json;
            else Session.Remove(DraftKey);

            Questions.DataSource = draft.Questions.Select((q, i) => new
            {
                Index = i, Number = i + 1, q.Id, q.Text, q.Topic, CanAddOption = q.Options.Count < 6, HintOpen = hint == i,
                Options = q.Options.Select((text, j) => new
                {
                    Question = i, QuestionNumber = i + 1, Index = j, Number = j + 1, Text = text, Correct = q.Answer == j, Removable = q.Options.Count > 2,
                    Outline = i > 0 && j == 0 ? "imgFrame168.svg" : "imgFrame167.svg"
                }).ToList()
            }).ToList();
            Questions.DataBind();
            NoQuestions.Visible = draft.Questions.Count == 0;
            var pairs = AiPairs();
            Conversation.DataSource = pairs.Skip(Math.Max(0, pairs.Count - 3));
            Conversation.DataBind();
        }

        int Argument(CommandEventArgs e, int part = 0) =>
            int.Parse(e.CommandArgument.ToString().Split(':')[part], CultureInfo.InvariantCulture);

        protected void AddQuestion_Click(object sender, EventArgs e) =>
            draft.Questions.Add(new QuestionDraft { Options = new List<string> { "", "", "" } });

        protected void SampleDraft_Click(object sender, EventArgs e)
        {
            draft.Questions.Add(new QuestionDraft
            {
                Text = "Which approach is best when learning " + C.Title + "?", Topic = "Practical learning", Answer = 0,
                Options = new List<string> { "Practise with a small example", "Skip validation", "Memorise without practising" }
            });
            ShowError("Sample draft added. Review it before publishing.");
        }

        protected void RemoveQuestion_Command(object sender, CommandEventArgs e)
        {
            var i = Argument(e);
            if (i < draft.Questions.Count) draft.Questions.RemoveAt(i);
        }

        protected void AddOption_Command(object sender, CommandEventArgs e)
        {
            var q = draft.Questions[Argument(e)];
            if (q.Options.Count < 6) q.Options.Add("");
        }

        protected void RemoveOption_Command(object sender, CommandEventArgs e)
        {
            var q = draft.Questions[Argument(e)];
            var j = Argument(e, 1);
            if (q.Options.Count <= 2 || j >= q.Options.Count) return;
            q.Options.RemoveAt(j);
            if (q.Answer == j) q.Answer = -1;
            else if (q.Answer > j) q.Answer--;
        }

        protected void Hint_Command(object sender, CommandEventArgs e) => hint = Argument(e);

        protected void Discard_Click(object sender, EventArgs e)
        {
            Session.Remove(DraftKey);
            Go("QuizEditor?id=" + C.Id);
        }

        protected void DeleteQuiz_Click(object sender, EventArgs e)
        {
            if (!TryRun("quizDelete", new { courseId = C.Id.ToString(CultureInfo.InvariantCulture) })) return;
            Session.Remove(DraftKey);
            ReloadPage("Quiz deleted");
        }

        protected void Save_Click(object sender, EventArgs e)
        {
            var problem = Problem();
            if (problem != null)
            {
                ShowError(problem);
                return;
            }
            if (!TryRun("quizSave", new
            {
                courseId = C.Id.ToString(CultureInfo.InvariantCulture), timeLimit = draft.TimeLimit, published = draft.Published,
                questions = draft.Questions.Select(q => new { id = q.Id, text = q.Text, options = q.Options, answer = q.Answer, topic = q.Topic })
            })) return;
            Session.Remove(DraftKey);
            ReloadPage("Quiz saved");
        }

        /// <summary>Names the first incomplete question, so the author knows where to look before the server refuses the save.</summary>
        string Problem()
        {
            for (int i = 0; i < draft.Questions.Count; i++)
            {
                var q = draft.Questions[i];
                if (string.IsNullOrWhiteSpace(q.Text)) return "Write the text of question " + (i + 1) + ".";
                if (q.Options.Any(string.IsNullOrWhiteSpace)) return "Fill in or remove the empty options of question " + (i + 1) + ".";
                if (q.Answer < 0) return "Choose the correct answer for question " + (i + 1) + ".";
            }
            return null;
        }

        protected void UseContext_Click(object sender, EventArgs e)
        {
            var question = draft.Questions.FirstOrDefault(q => !string.IsNullOrWhiteSpace(q.Text));
            var text = "Help me write clear answer options for " + (question?.Text.Trim() ?? C.Title) + ".";
            AskBox.Text = text.Length > 1000 ? text.Substring(0, 1000) : text;
        }

        protected void Ask_Click(object sender, EventArgs e)
        {
            var question = AskBox.Text.Trim();
            if (question.Length == 0)
            {
                AskError.Text = "Type a question first.";
                return;
            }
            // The quiz draft is kept in the session, so the reload keeps unsaved questions.
            Session[DraftKey] = JsonConvert.SerializeObject(draft);
            if (TryRun("ai", new { text = question })) ReloadPage();
        }
    }
}
