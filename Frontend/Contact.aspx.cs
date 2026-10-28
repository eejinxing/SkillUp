using System;

namespace SKILL_UP.Frontend
{
    /// <summary>The enquiry form on its own page (Controls/EnquiryForm.ascx).</summary>
    public partial class ContactPage : SkillPage
    {
        public override string Route => "contact";
        public override PageLayout Layout => PageLayout.Public;
        public override string BodyClass => "ui-home";

        protected void Page_Load(object sender, EventArgs e)
        {
        }
    }
}
