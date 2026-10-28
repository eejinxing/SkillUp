using System;
using Newtonsoft.Json.Linq;

namespace SKILL_UP.Frontend
{
    public partial class RegisterPage : SkillPage
    {
        public override string Route => "register";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Me != null && !IsPostBack) Go("Dashboard");
            // The landing page footer passes the email typed into its sign-up box.
            if (!IsPostBack && Request.QueryString["email"] != null) AuthEmail.Text = Request.QueryString["email"];
        }

        protected void Register_Click(object sender, EventArgs e)
        {
            if (!IsValid) return;
            var payload = new
            {
                name = AuthName.Text.Trim(), username = AuthUsername.Text.Trim(), email = AuthEmail.Text.Trim(),
                role = RoleInstructor.Checked ? "Instructor" : "Learner", password = AuthPassword.Text, confirm = AuthConfirm.Text
            };
            object result;
            if (!TryRun("register", payload, out result)) return;
            var pending = (bool?)((JObject)result)?["pending"] == true;
            Go("Login", pending ? "Account created. Your instructor application is pending administrator approval."
                               : "Account created. Log in to start learning.");
        }

        public override void ShowError(string message) => FormError.Text = Server.HtmlEncode(message);
    }
}
