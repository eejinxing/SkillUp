using System;

namespace SKILL_UP.Frontend
{
    public partial class AboutPage : SkillPage
    {
        public override string Route => "about";
        public override PageLayout Layout => PageLayout.Public;

        protected void Page_Load(object sender, EventArgs e)
        {
        }
    }
}
