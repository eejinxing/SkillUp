using System;
using Newtonsoft.Json.Linq;

namespace SKILL_UP.Frontend
{
    public partial class ForgotPasswordPage : SkillPage
    {
        public override string Route => "forgot";

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void Send_Click(object sender, EventArgs e)
        {
            object result;
            if (!IsValid || !TryRun("forgot", new { email = AuthEmail.Text.Trim() }, out result)) return;
            var token = (string)((JObject)result)?["token"];
            if (token == null)
            {
                Steps.ActiveViewIndex = 2;
                return;
            }
            // No email service yet: in development (SkillUp:ShowResetLink) the link is shown here instead.
            ResetLink.NavigateUrl = "ResetPassword?token=" + Server.UrlEncode(token);
            Steps.ActiveViewIndex = 1;
        }

        public override void ShowError(string message) => FormError.Text = Server.HtmlEncode(message);
    }
}
