using System;

namespace SKILL_UP.Frontend
{
    /// <summary>The signed-in role's overview page.</summary>
    public partial class DashboardPage : SkillPage
    {
        public override string Route => "dashboard";
        protected override string[] Roles => new string[0];

        public override string BodyClass => Me?.Role == "Learner" ? "has-learning-console has-lc-exact has-velo-dashboard" : "";

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            if (Me == null) return;
            // Loaded on every request (also on postback) so the dashboard's own buttons keep working.
            var control = Me.Role == "Program Leader" ? "LeaderDashboard" : Me.Role.Replace(" ", "") + "Dashboard";
            RoleDashboard.Controls.Add(LoadControl("~/Frontend/Controls/" + control + ".ascx"));
            Title = "Overview";
        }

        protected void Page_Load(object sender, EventArgs e)
        {
        }
    }
}
