using System;

namespace SKILL_UP.Frontend
{
    /// <summary>
    /// The enquiry form for signed-in accounts (Controls/EnquiryForm.ascx). Visitors have the same form on the landing
    /// page, so they are sent to it there.
    /// </summary>
    public partial class HelpPage : SkillPage
    {
        public override string Route => "help";
        public override bool Secondary => true;
        public override string BodyClass => "ui-home";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Me == null) Go(HomeContact);
        }
    }
}
