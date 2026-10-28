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

        protected void Save_Click(object sender, EventArgs e)
        {
            var payload = new { token = Request.QueryString["token"], password = AuthPassword.Text, confirm = AuthConfirm.Text };
            if (IsValid && TryRun("resetPassword", payload)) Go("Login", "Password updated. Log in with your new password.");
        }

        public override void ShowError(string message) => FormError.Text = Server.HtmlEncode(message);
    }
}
