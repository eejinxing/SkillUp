using System;
using System.Web.UI;

namespace SKILL_UP.Frontend
{
    public partial class UserEditorPage : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string route = "users";
            string value0 = Request.QueryString["id"];
            if (string.IsNullOrWhiteSpace(value0))
                route = "users/new";
            else
                if (!string.IsNullOrWhiteSpace(value0)) route += "/" + Uri.EscapeDataString(value0);
            ((SkillUpMaster)Master).SetInitialRoute(route);
            // Backend handoff: add authenticated server-side services here when implementing the backend.
            // Current form actions remain in JavaScript -> SkillAPI.request; no database implementation is claimed.
        }
    }
}
