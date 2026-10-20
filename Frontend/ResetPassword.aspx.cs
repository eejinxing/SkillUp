using System;
using System.Web.UI;

namespace SKILL_UP.Frontend
{
    public partial class ResetPasswordPage : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string route = "reset";
            string value0 = Request.QueryString["token"];
            if (string.IsNullOrWhiteSpace(value0))
                route = "forgot";
            else
                if (!string.IsNullOrWhiteSpace(value0)) route += "/" + Uri.EscapeDataString(value0);
            ((SkillUpMaster)Master).SetInitialRoute(route);
            // Backend handoff: add authenticated server-side services here when implementing the backend.
            // Current form actions remain in JavaScript -> SkillAPI.request; no database implementation is claimed.
        }
    }
}
