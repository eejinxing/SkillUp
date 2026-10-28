using System;
using Newtonsoft.Json.Linq;

namespace SKILL_UP.Frontend
{
    /// <summary>Account settings (?tab=security|address|billing): password, saved address in USER.preferences, and learning access.</summary>
    public partial class SettingsPage : SkillPage
    {
        public override string Route => "settings";
        protected override string[] Roles => new string[0];
        public override bool Secondary => true;

        protected string Tab;
        protected bool PasswordOpen;

        protected void Page_Load(object sender, EventArgs e)
        {
            Tab = Request.QueryString["tab"] == "address" || Request.QueryString["tab"] == "billing" ? Request.QueryString["tab"] : "security";
            Tabs.ActiveViewIndex = Tab == "address" ? 1 : Tab == "billing" ? 2 : 0;
            if (IsPostBack || Tab != "address") return;
            JObject p = null;
            try { p = string.IsNullOrEmpty(Me.Preferences) ? null : JObject.Parse(Me.Preferences); } catch (Exception) { }
            Address.Text = (string)p?["address"] ?? "";
            City.Text = (string)p?["city"] ?? "";
            Country.Text = (string)p?["country"] ?? "";
            Postcode.Text = (string)p?["postcode"] ?? "";
        }

        protected void Password_Click(object sender, EventArgs e)
        {
            PasswordOpen = true;
            if (IsValid && TryRun("password", new { current = CurrentPassword.Text, password = NewPassword.Text, confirm = ConfirmPassword.Text })) ReloadPage("Password updated");
        }

        protected void Address_Click(object sender, EventArgs e)
        {
            var payload = new { section = "address", address = Address.Text, city = City.Text, country = Country.Text, postcode = Postcode.Text };
            if (TryRun("preferences", payload)) ReloadPage("Address saved");
        }

        protected void SignOut_Click(object sender, EventArgs e)
        {
            if (TryRun("logout")) Go("Home", "You have signed out.");
        }

        public override void ShowError(string message)
        {
            if (Tab == "security") PasswordError.Text = Server.HtmlEncode(message);
            else base.ShowError(message);
        }
    }
}
