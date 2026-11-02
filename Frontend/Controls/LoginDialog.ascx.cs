using System;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SKILL_UP.Frontend.Controls
{
    /// <summary>
    /// The Log in form as a pop-up. SkillUp.Master puts it on every visitor page; it opens over the page when the address
    /// ends in #login. A refused sign-in comes back with the pop-up open and its message. Signing in goes to ?return=, back to the page (SkillPage.StayAfterSignIn) or to the dashboard.
    /// </summary>
    public partial class LoginDialog : UserControl
    {
        /// <summary>Shown without #login: the Login page itself, which closes to the landing page.</summary>
        public bool Open { get; set; }

        SkillPage Host => (SkillPage)Page;
        bool reopen;
        /// <summary>Drawn open: on the Login page, or in the reply to a refused sign-in.</summary>
        protected bool Shown => Open || reopen;
        protected string CloseUrl => Open ? "Home" : "#close";
        protected string RegisterUrl => Open ? "Register" : "#register";

        protected void Page_Load(object sender, EventArgs e)
        {
            DemoPanel.Visible = Backend.Settings.DemoLogin;
        }

        /// <summary>Where to go after signing in: the page that asked for it (?return=, on this site), this page, or the dashboard.</summary>
        public string ReturnUrl
        {
            get
            {
                var target = Request.QueryString["return"];
                if (!string.IsNullOrEmpty(target) && target.StartsWith("/") && !target.StartsWith("//") && target.IndexOf('\\') < 0 && target.IndexOfAny(new[] { '\r', '\n' }) < 0) return target;
                return Host.StayAfterSignIn ? Request.RawUrl : "Dashboard";
            }
        }

        protected void LogIn_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) { reopen = true; return; }
            if (Host.TryRun("login", new { email = LoginUser.Text.Trim(), password = LoginPassword.Text }, out _, ShowError)) Host.Go(ReturnUrl);
        }

        protected void Demo_Command(object sender, CommandEventArgs e)
        {
            if (Host.TryRun("demo", new { role = (string)e.CommandArgument }, out _, ShowError)) Host.Go(ReturnUrl);
        }

        void ShowError(string message)
        {
            FormError.Text = Server.HtmlEncode(message);
            reopen = true;
        }
    }
}
