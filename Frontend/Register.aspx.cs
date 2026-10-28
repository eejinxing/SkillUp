using System;

namespace SKILL_UP.Frontend
{
    /// <summary>
    /// The address for creating an account. It shows the master page's Sign up pop-up already open; the form lives in
    /// Controls/RegisterDialog.ascx.
    /// </summary>
    public partial class RegisterPage : SkillPage
    {
        public override string Route => "register";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Me != null && !IsPostBack) Go("Dashboard");
            ((SkillUpMaster)Master).SignUpForm.Open = true;
        }
    }
}
