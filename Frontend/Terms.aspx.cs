using System;

namespace SKILL_UP.Frontend
{
    public partial class TermsPage : SkillPage
    {
        public override string Route => "terms";
        public override PageLayout Layout => PageLayout.Public;

        protected void Page_Load(object sender, EventArgs e)
        {
        }
    }
}
