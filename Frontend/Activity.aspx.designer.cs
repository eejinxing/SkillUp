// This file describes the controls declared in Activity.aspx.
namespace SKILL_UP.Frontend
{
    public partial class ActivityPage
    {
        protected global::System.Web.UI.WebControls.MultiView Screens;
        protected global::System.Web.UI.WebControls.Repeater Conversation;
        protected global::System.Web.UI.WebControls.Repeater Suggestions;
        protected global::SKILL_UP.Frontend.Controls.SubmitButton ContextButton;
        protected global::System.Web.UI.WebControls.TextBox AskBox;
        protected global::SKILL_UP.Frontend.Controls.SubmitButton AskButton;
        protected global::System.Web.UI.WebControls.Literal AskError;
        protected global::SKILL_UP.Frontend.Controls.TerminalLab LabBefore;
        protected global::System.Web.UI.WebControls.PlaceHolder TaskArea;
        protected global::System.Web.UI.WebControls.MultiView Task;
        protected global::SKILL_UP.Frontend.Controls.SubmitButton CompleteButton;
        protected global::SKILL_UP.Frontend.Controls.SubmitButton HintArticle;
        protected global::System.Web.UI.WebControls.TextBox LabAnswer;
        protected global::SKILL_UP.Frontend.Controls.SubmitButton LabSubmit;
        protected global::SKILL_UP.Frontend.Controls.SubmitButton HintLab;
        protected global::System.Web.UI.WebControls.Literal LabError;
        protected global::System.Web.UI.WebControls.PlaceHolder QuizForm;
        protected global::System.Web.UI.WebControls.Repeater QuizQuestions;
        protected global::System.Web.UI.WebControls.Literal QuizError;
        protected global::SKILL_UP.Frontend.Controls.SubmitButton QuizSubmit;
        protected global::SKILL_UP.Frontend.Controls.SubmitButton HintQuiz;
        protected global::SKILL_UP.Frontend.Controls.SubmitButton RestartButton;
        protected global::System.Web.UI.WebControls.PlaceHolder QuizResult;
        protected global::System.Web.UI.WebControls.Repeater ResultFeedback;
        protected global::SKILL_UP.Frontend.Controls.SubmitButton TryAgainButton;
        protected global::System.Web.UI.WebControls.PlaceHolder HintPanel;
        protected global::SKILL_UP.Frontend.Controls.TerminalLab LabAfter;
    }
}
