using System;
using System.Web.UI;

namespace SKILL_UP.Frontend
{
    public partial class CatalogPage : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string route = "catalog";
            ((SkillUpMaster)Master).SetInitialRoute(route);
            // Backend handoff: add authenticated server-side services here when implementing the backend.
            // Current form actions remain in JavaScript -> SkillAPI.request; no database implementation is claimed.
        }
    }
}
