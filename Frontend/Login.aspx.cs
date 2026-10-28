using System;
using System.Web.UI.WebControls;

namespace SKILL_UP.Frontend
{
    public partial class LoginPage : SkillPage
    {
        public override string Route => "login";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Me != null && !IsPostBack) Go(ReturnUrl);
            DemoPanel.Visible = Backend.Settings.DemoLogin;
        }

        /// <summary>Where to go after signing in: the page that asked for it, when it is on this site.</summary>
        string ReturnUrl
        {
            get
            {
                var target = Request.QueryString["return"];
                return !string.IsNullOrEmpty(target) && target.StartsWith("/") && !target.StartsWith("//") ? target : "Dashboard";
            }
        }

        protected void LogIn_Click(object sender, EventArgs e)
        {
            if (IsValid && TryRun("login", new { email = AuthEmail.Text.Trim(), password = AuthPassword.Text })) Go(ReturnUrl);
        }

        protected void Demo_Command(object sender, CommandEventArgs e)
        {
            if (TryRun("demo", new { role = (string)e.CommandArgument })) Go(ReturnUrl);
        }

        public override void ShowError(string message) => FormError.Text = Server.HtmlEncode(message);
    }
}
