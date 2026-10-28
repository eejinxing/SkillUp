using System;

namespace SKILL_UP.Frontend
{
    /// <summary>
    /// The address for signing in (pages that need an account send visitors to Login?return=). It shows the master page's
    /// Log in pop-up already open; the form and its buttons live in Controls/LoginDialog.ascx.
    /// </summary>
    public partial class LoginPage : SkillPage
    {
        public override string Route => "login";

        protected void Page_Load(object sender, EventArgs e)
        {
            var form = ((SkillUpMaster)Master).LoginForm;
            if (Me != null && !IsPostBack) Go(form.ReturnUrl);
            form.Open = true;
        }
    }
}
