using System;

namespace SKILL_UP.Frontend
{
    /// <summary>The enquiry form on its own page (Controls/EnquiryForm.ascx).</summary>
    public partial class HelpPage : SkillPage
    {
        public override string Route => "help";
        public override bool Secondary => true;
        public override string BodyClass => "ui-home";

        protected void Page_Load(object sender, EventArgs e)
        {
        }
    }
}
