using System;
using System.Web.UI;
using Newtonsoft.Json.Linq;

namespace SKILL_UP.Frontend.Controls
{
    /// <summary>
    /// The Sign up form as a pop-up. SkillUp.Master puts it on every visitor page; it opens over the page when the address
    /// ends in #register. A refused sign-up comes back with the pop-up open and its message. A new account then opens the Log in pop-up on the same page.
    /// </summary>
    public partial class RegisterDialog : UserControl
    {
        /// <summary>Shown without #register: the Register page itself, which closes to the landing page.</summary>
        public bool Open { get; set; }

        SkillPage Host => (SkillPage)Page;
        bool reopen;
        /// <summary>Drawn open: on the Register page, in the reply to a refused sign-up, or after the footer sign-up box.</summary>
        protected bool Shown => Open || reopen;
        protected string CloseUrl => Open ? "Home" : "#close";
        protected string LoginUrl => Open ? "Login" : "#login";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Register?email= (an old link to the page) fills in the email.
            if (!IsPostBack && Request.QueryString["email"] != null) SignUpEmailBox.Text = Request.QueryString["email"];
        }

        /// <summary>Fills in the email, for the footer's sign-up box.</summary>
        public void Prefill(string email)
        {
            SignUpEmailBox.Text = email;
            reopen = true;
        }

        protected void Register_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) { reopen = true; return; }
            var payload = new
            {
                name = SignUpName.Text.Trim(), username = SignUpUsername.Text.Trim(), email = SignUpEmailBox.Text.Trim(),
                role = RoleInstructor.Checked ? "Instructor" : "Learner", password = SignUpPassword.Text, confirm = SignUpConfirm.Text
            };
            object result;
            if (!Host.TryRun("register", payload, out result, message => { FormError.Text = Server.HtmlEncode(message); reopen = true; })) return;
            var pending = (bool?)((JObject)result)?["pending"] == true;
            Host.Go(Open ? "Login" : Request.RawUrl + "#login", pending ? "Account created. Your instructor application is pending administrator approval."
                                                                 : "Account created. Log in to start learning.");
        }
    }
}
