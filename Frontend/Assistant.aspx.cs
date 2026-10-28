using System;

namespace SKILL_UP.Frontend
{
    /// <summary>The learning companion: the account's questions and the sample replies (Commands.Ai).</summary>
    public partial class AssistantPage : SkillPage
    {
        public override string Route => "assistant";
        protected override string[] Roles => new string[0];
        public override bool Secondary => true;

        protected void Page_Load(object sender, EventArgs e)
        {
            var pairs = AiPairs();
            History.DataSource = pairs;
            History.DataBind();
            Welcome.Visible = pairs.Count == 0;
        }

        protected void Ask_Click(object sender, EventArgs e)
        {
            if (IsValid && TryRun("ai", new { text = Question.Text.Trim() })) ReloadPage();
        }
    }
}
