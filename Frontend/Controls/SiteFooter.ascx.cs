using System;
using System.Web.UI;

namespace SKILL_UP.Frontend.Controls
{
    /// <summary>
    /// The footer every visitor page ends with (the landing page's "Your next skill starts here" sign-up, contact links,
    /// logo and legal links). The master page draws it for visitors; signed-in accounts have no footer.
    /// </summary>
    public partial class SiteFooter : UserControl
    {
        protected void SignUp_Click(object sender, EventArgs e)
        {
            // The page comes back with the Sign up pop-up open and this email filled in.
            ((SkillUpMaster)Page.Master).SignUpForm.Prefill(SignUpEmail.Text.Trim());
        }
    }
}
