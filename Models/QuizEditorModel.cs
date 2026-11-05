using System.Globalization;
using Newtonsoft.Json;

namespace SkillUp.Models;

/// <summary>
/// The question editor of a course (?id=). The quiz being edited is a draft kept in the session: every button of the editor posts the
/// whole form, QuizEditor/Edit applies the button to the draft, and this model shows the draft. Saving sends it to the quizSave command.
/// </summary>
public sealed class QuizEditorModel : PageModel
{
    public QuizEditorModel(SkillContext ctx) : base(ctx) { }

    public sealed class QuizDraft
    {
        public string TimeLimit = "10";
        public bool Published;
        public List<QuestionDraft> Questions = new();
    }

    public sealed class QuestionDraft
    {
        public string Id = "", Text = "", Topic = "";
        public List<string> Options = new();
        public int Answer = -1;
    }

    public CourseRow C;
    public bool IsDenied, Locked, Deleting, Changed, NoQuestions;
    public QuizDraft Draft;
    /// <summary>The question whose authoring hint is open (?hint=), or -1.</summary>
    public int Hint = -1;
    public IEnumerable<dynamic> QuestionList = new List<object>();
    public List<AiPair> Conversation = new();
    /// <summary>What the "Use question context" button put in the question box.</summary>
    public string AskText = "";

    public const string AskKey = "SkillUp.QuizEditor.Ask";

    public static string DraftKey(int userId, int courseId) => "SkillUp.QuizDraft." + userId + "." + courseId;

    /// <summary>The draft kept in the session, or the saved quiz when there is none.</summary>
    public static QuizDraft KeptDraft(SkillContext ctx, int courseId)
    {
        var kept = ctx.Http.Session.GetString(DraftKey(ctx.Me.Id, courseId));
        return kept == null ? SavedDraft(ctx.Data, courseId) : JsonConvert.DeserializeObject<QuizDraft>(kept);
    }

    public static void KeepDraft(SkillContext ctx, int courseId, QuizDraft draft) =>
        ctx.Http.Session.SetString(DraftKey(ctx.Me.Id, courseId), JsonConvert.SerializeObject(draft));

    public static void ForgetDraft(SkillContext ctx, int courseId) => ctx.Http.Session.Remove(DraftKey(ctx.Me.Id, courseId));

    public static QuizDraft SavedDraft(Snapshot data, int courseId)
    {
        var quiz = data.QuizFor(courseId);
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

    /// <summary>Names the first incomplete question, so the author knows where to look before the server refuses the save.</summary>
    public static string Problem(QuizDraft draft)
    {
        for (var i = 0; i < draft.Questions.Count; i++)
        {
            var q = draft.Questions[i];
            if (string.IsNullOrWhiteSpace(q.Text)) return "Write the text of question " + (i + 1) + ".";
            if (q.Options.Any(string.IsNullOrWhiteSpace)) return "Fill in or remove the empty options of question " + (i + 1) + ".";
            if (q.Answer < 0) return "Choose the correct answer for question " + (i + 1) + ".";
        }
        return null;
    }

    public void Load()
    {
        var id = QueryInt("id");
        C = id == null ? null : OwnCourses.FirstOrDefault(c => c.Id == id);
        if (C == null)
        {
            IsDenied = true;
            return;
        }
        Deleting = Data.Requests.Any(r => r.CourseId == C.Id && r.Kind == "Delete" && r.Status == "Pending review");
        Locked = C.Status == "Pending review" || Deleting;
        Draft = Locked ? SavedDraft(Data, C.Id) : KeptDraft(Ctx, C.Id);
        Hint = QueryInt("hint") ?? -1;
        AskText = Ctx.Http.Session.GetString(AskKey) ?? "";
        Ctx.Http.Session.Remove(AskKey);
    }

    public void Draw()
    {
        if (C == null) return;
        // Unsaved edits are kept for this session; they are forgotten once they match the saved quiz again.
        var json = JsonConvert.SerializeObject(Draft);
        Changed = !Locked && json != JsonConvert.SerializeObject(SavedDraft(Data, C.Id));
        if (Changed) Ctx.Http.Session.SetString(DraftKey(Me.Id, C.Id), json);
        else ForgetDraft(Ctx, C.Id);

        QuestionList = Draft.Questions.Select((q, i) => new
        {
            Index = i, Number = i + 1, q.Id, q.Text, q.Topic, CanAddOption = q.Options.Count < 6, HintOpen = Hint == i,
            Options = q.Options.Select((text, j) => new
            {
                Question = i, QuestionNumber = i + 1, Index = j, Number = j + 1, Text = text, Correct = q.Answer == j, Removable = q.Options.Count > 2,
                Outline = i > 0 && j == 0 ? "imgFrame168.svg" : "imgFrame167.svg"
            }).ToList()
        }).ToList();
        NoQuestions = Draft.Questions.Count == 0;
        var pairs = AiPairs();
        Conversation = pairs.Skip(Math.Max(0, pairs.Count - 3)).ToList();
    }
}
