using System;

namespace SKILL_UP.Frontend
{
    public partial class ResetPasswordPage : SkillPage
    {
        public override string Route => "reset";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Request.QueryString["token"])) Go("ForgotPassword");
        }

        /// <summary>The same rules as the browser check (Backend Passwords.Problem), for a browser without scripts.</summary>
        protected void Password_ServerValidate(object source, System.Web.UI.WebControls.ServerValidateEventArgs args)
        {
            var problem = Backend.Passwords.Problem(args.Value);
            args.IsValid = problem == null;
            if (problem != null) ((System.Web.UI.WebControls.CustomValidator)source).ErrorMessage = problem;
        }

        protected void Save_Click(object sender, EventArgs e)
        {
            var payload = new { token = Request.QueryString["token"], password = AuthPassword.Text, confirm = AuthConfirm.Text };
            if (IsValid && TryRun("resetPassword", payload)) Go("Login", "Password updated. Log in with your new password.");
        }

        public override void ShowError(string message) => FormError.Text = Server.HtmlEncode(message);
    }
}
