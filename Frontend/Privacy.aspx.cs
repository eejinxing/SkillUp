using System;

namespace SKILL_UP.Frontend
{
    public partial class PrivacyPage : SkillPage
    {
        public override string Route => "privacy";
        public override PageLayout Layout => PageLayout.Public;

        protected void Page_Load(object sender, EventArgs e)
        {
        }
    }
}
