using System;
using System.Web.UI;

namespace SKILL_UP.Frontend
{
    public partial class ActivityPage : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string route = "lesson";
            string value0 = Request.QueryString["course"];
            string value1 = Request.QueryString["lesson"];
            if (string.IsNullOrWhiteSpace(value0) || string.IsNullOrWhiteSpace(value1))
                route = "learning";
            else
                route += "/" + Uri.EscapeDataString(value0) + "/" + Uri.EscapeDataString(value1);
            ((SkillUpMaster)Master).SetInitialRoute(route);
            // Backend handoff: add authenticated server-side services here when implementing the backend.
            // Current form actions remain in JavaScript -> SkillAPI.request; no database implementation is claimed.
        }
    }
}
